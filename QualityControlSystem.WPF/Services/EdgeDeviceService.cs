using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services
{
    public class EdgeDeviceService : IEdgeDeviceService
    {
        private readonly INotificationService _notificationService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EdgeDeviceOptions _options;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public EdgeDeviceService(
            INotificationService notificationService,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _notificationService = notificationService;
            _scopeFactory = scopeFactory;
            _options = configuration.GetSection("EdgeDevice").Get<EdgeDeviceOptions>() ?? new EdgeDeviceOptions();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(configuration.GetValue("EdgeDevice:TimeoutSeconds", 30))
            };
        }

        public async Task<AnalysisResultDto> AnalyzeFrameAsync()
        {
            var results = await GetInspectionResultsAsync();
            var last = results.OrderByDescending(x => x.RecordedAt).FirstOrDefault();
            if (last == null)
                return new AnalysisResultDto { Status = "Нет данных", Message = "Скрипт operating пока не вернул результаты" };

            return new AnalysisResultDto
            {
                Status = last.Status ?? string.Empty,
                MatchValue = last.Similarity ?? 0,
                Message = last.Reason ?? string.Empty,
                Timestamp = last.RecordedAt
            };
        }

        public async Task CaptureTemplateAsync(int templateNumber)
        {
            await CaptureTemplateRemoteAsync();
            _notificationService.ShowSuccess("Команда создания шаблона отправлена на Raspberry Pi");
        }

        public async Task<DeviceStatusDto> GetStatusAsync()
        {
            try
            {
                var status = await GetStatusPayloadAsync(BuildUrl(_options.PhotomakerPort, "status"))
                    ?? await GetStatusPayloadAsync(BuildUrl(_options.OperatingPort, "status"));

                return new DeviceStatusDto
                {
                    IsConnected = status != null,
                    Status = status?.GetPropertyOrDefault("state") ?? "Недоступно",
                    TemplatesCount = 0
                };
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Ошибка получения статуса: {ex.Message}");
                throw;
            }
        }

        public string GetPhotomakerFrameUrl() => BuildUrl(_options.PhotomakerPort, "frame.jpg");

        public string GetOperatingFrameUrl() => BuildUrl(_options.OperatingPort, "frame.jpg");

        public async Task<IReadOnlyList<LookupItemDto>> GetFrameOptionsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await dbContext.Frames
                .AsNoTracking()
                .OrderBy(frame => frame.Name)
                .Select(frame => new LookupItemDto
                {
                    Id = frame.FrameId,
                    Name = frame.Name,
                    Weight = frame.Weight.HasValue ? (double?)Convert.ToDouble(frame.Weight.Value) : null
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<string> CheckConnectionAsync(CancellationToken cancellationToken = default)
        {
            var result = await RunSshAsync("printf 'connected: '; hostname", cancellationToken);
            return result.Trim();
        }

        public async Task<string> DeployScriptsAsync(CancellationToken cancellationToken = default)
        {
            var photomakerServer = FindProjectScript("photomaker_module.py");
            var operatingServer = FindProjectScript("operating_module.py");
            var remote = $"{RemoteIdentity()}:{_options.RemoteDirectory.TrimEnd('/')}/";

            var copy = await RunProcessAsync("scp", BuildScpArgs(new[] { photomakerServer, operatingServer }, remote), cancellationToken);
            var chmod = await RunSshAsync($"chmod +x {QuoteRemote(_options.RemoteDirectory.TrimEnd('/') + "/photomaker_module.py")} {QuoteRemote(_options.RemoteDirectory.TrimEnd('/') + "/operating_module.py")}", cancellationToken);
            return $"{copy.Trim()}\n{chmod.Trim()}".Trim();
        }

        public async Task<string> StartPhotomakerAsync(CancellationToken cancellationToken = default)
        {
            await StopOperatingAsync(cancellationToken);
            await StopPhotomakerAsync(cancellationToken);
            return await RunSshAsync(BuildStartCommand("photomaker_module.py", _options.PhotomakerPort, "photomaker_server.log", "photomaker_server.pid"), cancellationToken);
        }

        public async Task StopPhotomakerAsync(CancellationToken cancellationToken = default)
        {
            await TryPostStopAsync(_options.PhotomakerPort, cancellationToken);
            await RunSshAsync(BuildStopCommand("photomaker_module.py", "photomaker_server.pid"), cancellationToken);
        }

        public async Task CaptureTemplateRemoteAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(BuildUrl(_options.PhotomakerPort, "capture"), cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<int> CaptureTemplateForFrameAsync(int frameId, string side, CancellationToken cancellationToken = default)
        {
            if (frameId <= 0)
                throw new InvalidOperationException("Выберите модель каркаса для шаблона.");

            if (string.IsNullOrWhiteSpace(side))
                throw new InvalidOperationException("Выберите сторону каркаса.");

            await CaptureTemplateRemoteAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);

            var localTemplateFolder = await SyncCapturedTemplateToLocalAsync(frameId, side.Trim(), cancellationToken);
            await ClearRemoteTemplateDirectoryAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var frameName = await dbContext.Frames
                .AsNoTracking()
                .Where(frame => frame.FrameId == frameId)
                .Select(frame => frame.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return await InsertTemplateRecordAsync(
                dbContext,
                $"{frameName ?? $"frame_{frameId}"} {side}",
                localTemplateFolder,
                side,
                cancellationToken);
        }

        public async Task<string> SyncTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var localTemplateDirectory = ResolveLocalTemplateDirectory();
            Directory.CreateDirectory(localTemplateDirectory);

            var remoteTemplate = $"{RemoteIdentity()}:{_options.RemoteTemplateDirectory.TrimEnd('/')}/*";
            return await RunProcessAsync("scp", BuildScpArgs(new[] { remoteTemplate }, localTemplateDirectory), cancellationToken);
        }

        public async Task<string> StartOperatingAsync(CancellationToken cancellationToken = default)
        {
            await StopPhotomakerAsync(cancellationToken);
            await StopOperatingAsync(cancellationToken);
            return await RunSshAsync(BuildStartCommand("operating_module.py", _options.OperatingPort, "operating_server.log", "operating_server.pid"), cancellationToken);
        }

        public async Task<string> StartOperatingForFrameAsync(int frameId, CancellationToken cancellationToken = default)
        {
            return await StartOperatingForFramesAsync(new[] { frameId }, cancellationToken);
        }

        public async Task<string> StartOperatingForFramesAsync(IEnumerable<int> frameIds, CancellationToken cancellationToken = default)
        {
            await PrepareRemoteTemplatesForFramesAsync(frameIds, cancellationToken);
            return await StartOperatingAsync(cancellationToken);
        }

        public async Task StopOperatingAsync(CancellationToken cancellationToken = default)
        {
            await TryPostStopAsync(_options.OperatingPort, cancellationToken);
            await RunSshAsync(BuildStopCommand("operating_module.py", "operating_server.pid"), cancellationToken);
        }

        public async Task<IReadOnlyList<EdgeInspectionResultDto>> GetInspectionResultsAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(BuildUrl(_options.OperatingPort, "results"), cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var results = await JsonSerializer.DeserializeAsync<List<EdgeInspectionResultDto>>(stream, _jsonOptions, cancellationToken);
            return results ?? new List<EdgeInspectionResultDto>();
        }

        public async Task<string> CreateQualityReportAsync(
            int frameId,
            IEnumerable<EdgeInspectionResultDto> results,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            if (frameId <= 0)
                throw new InvalidOperationException("Выберите модель каркаса для отчета.");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("Укажите путь для сохранения отчета.");

            var rows = results
                .Where(result => result.RecordedAt != default)
                .GroupBy(result => new { result.RecordedAt, result.Id })
                .Select(group => group.First())
                .OrderBy(result => result.ControlNumber == 0 ? int.MaxValue : result.ControlNumber)
                .ThenBy(result => result.RecordedAt)
                .ThenBy(result => result.Id)
                .ToList();

            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index].ControlNumber == 0)
                    rows[index].ControlNumber = index + 1;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await EnsureFrameTestFormHasNoDeviceColumnsAsync(dbContext, cancellationToken);

            var frame = await dbContext.Frames
                .AsNoTracking()
                .Include(item => item.MaterialType)
                .Include(item => item.Workshop)
                .FirstOrDefaultAsync(item => item.FrameId == frameId, cancellationToken);

            if (frame == null)
                throw new InvalidOperationException("Выбранная модель каркаса не найдена в БД.");

            var expectedWeight = frame.Weight.HasValue ? Convert.ToDouble(frame.Weight.Value) : (double?)null;
            ApplyWeightCheck(rows, expectedWeight);
            var tests = await LoadReportTestsAsync(dbContext, frameId, cancellationToken);
            var devices = await LoadReportDevicesAsync(dbContext, cancellationToken);
            var reportText = BuildQualityReportV3(frame, devices, tests, rows);
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(outputPath, reportText, Encoding.UTF8, cancellationToken);
            return outputPath;
        }

        public async Task<string> CreateQualityReportAsync(
            IEnumerable<int> frameIds,
            IEnumerable<EdgeInspectionResultDto> results,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            var selectedFrameIds = NormalizeFrameIds(frameIds);
            if (selectedFrameIds.Count == 0)
                throw new InvalidOperationException("Выберите модели каркаса для отчета.");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("Укажите путь для сохранения отчета.");

            var rows = results
                .Where(result => result.RecordedAt != default)
                .GroupBy(result => new { result.RecordedAt, result.Id })
                .Select(group => group.First())
                .OrderBy(result => result.ControlNumber == 0 ? int.MaxValue : result.ControlNumber)
                .ThenBy(result => result.RecordedAt)
                .ThenBy(result => result.Id)
                .ToList();

            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index].ControlNumber == 0)
                    rows[index].ControlNumber = index + 1;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await EnsureFrameTestFormHasNoDeviceColumnsAsync(dbContext, cancellationToken);

            var frames = await dbContext.Frames
                .AsNoTracking()
                .Include(item => item.MaterialType)
                .Include(item => item.Workshop)
                .Where(item => selectedFrameIds.Contains(item.FrameId))
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken);

            if (frames.Count == 0)
                throw new InvalidOperationException("Выбранные модели каркаса не найдены в БД.");

            var templateFrameMap = await LoadTemplateFrameMapAsync(dbContext, selectedFrameIds, cancellationToken);
            ApplyFrameChecks(rows, templateFrameMap, frames);
            var tests = await LoadReportTestsAsync(dbContext, selectedFrameIds, cancellationToken);
            var devices = await LoadReportDevicesAsync(dbContext, cancellationToken);
            var reportText = BuildQualityReportForFrames(frames, devices, tests, rows);
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(outputPath, reportText, Encoding.UTF8, cancellationToken);
            return outputPath;
        }

        private async Task<JsonElement?> GetStatusPayloadAsync(string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var text = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(text);
            }
            catch
            {
                return null;
            }
        }

        private async Task TryPostStopAsync(int port, CancellationToken cancellationToken)
        {
            try
            {
                await _httpClient.PostAsync(BuildUrl(port, "stop"), null, cancellationToken);
            }
            catch
            {
                // Process can already be stopped; pkill is the fallback.
            }
        }

        private string BuildUrl(int port, string path)
        {
            return $"http://{_options.Host}:{port}/{path.TrimStart('/')}";
        }

        private async Task<string> SyncCapturedTemplateToLocalAsync(int frameId, string side, CancellationToken cancellationToken)
        {
            var tempRoot = Path.Combine(ResolveLocalTemplateDirectory(), "_remote_sync");
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);

            Directory.CreateDirectory(tempRoot);
            var remoteTemplate = $"{RemoteIdentity()}:{_options.RemoteTemplateDirectory.TrimEnd('/')}/*";
            await RunProcessAsync("scp", BuildScpArgs(new[] { remoteTemplate }, tempRoot), cancellationToken);

            var captured = Directory.GetDirectories(tempRoot)
                .Where(path => int.TryParse(Path.GetFileName(path), out _))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (captured == null)
                throw new InvalidOperationException("Photomaker не создал папку шаблона на Raspberry Pi.");

            var destinationParent = Path.Combine(
                ResolveLocalTemplateDirectory(),
                $"frame_{frameId}",
                side,
                DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(destinationParent);
            var destination = Path.Combine(destinationParent, Path.GetFileName(captured));
            CopyDirectory(captured, destination);
            return destination;
        }

        private static async Task<int> InsertTemplateRecordAsync(
            AppDbContext dbContext,
            string name,
            string imagePath,
            string side,
            CancellationToken cancellationToken)
        {
            var normalizedSide = side.Trim().ToLowerInvariant();
            var allowedSides = new[] { "front", "left", "right", "top", "back" };
            if (!allowedSides.Contains(normalizedSide))
                throw new InvalidOperationException($"Недопустимая сторона шаблона: {side}.");

            if (imagePath.Length > 500)
                throw new InvalidOperationException($"Путь к шаблону длиннее 500 символов: {imagePath}");

            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO template(name, image_path, side)
                VALUES (@name, @image_path, CAST(@side AS template_side))
                RETURNING template_id;
                """;

            AddParameter(command, "name", name);
            AddParameter(command, "image_path", imagePath);
            AddParameter(command, "side", normalizedSide);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        private static async Task EnsureFrameTestFormHasNoDeviceColumnsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                ALTER TABLE frame_test_form
                    DROP COLUMN IF EXISTS camera_id;

                ALTER TABLE frame_test_form
                    DROP COLUMN IF EXISTS sensor_id;
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<IReadOnlyList<ReportTestInfo>> LoadReportTestsAsync(
            AppDbContext dbContext,
            int frameId,
            CancellationToken cancellationToken)
        {
            var tests = new Dictionary<int, ReportTestInfo>();
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT
                        ftf.frame_test_form_id,
                        ftf.name,
                        ftf.description
                    FROM frame_test_form ftf
                    JOIN frame_test_form_frame ftff ON ftff.frame_test_form_id = ftf.frame_test_form_id
                    WHERE ftff.frame_id = @frame_id
                    ORDER BY ftf.frame_test_form_id;
                    """;
                AddParameter(command, "frame_id", frameId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var test = new ReportTestInfo
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                    tests[test.Id] = test;
                }
            }

            if (tests.Count == 0)
                return Array.Empty<ReportTestInfo>();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT
                        ftft.frame_test_form_id,
                        t.template_id,
                        t.name,
                        t.side,
                        t.image_path
                    FROM frame_test_form_template ftft
                    JOIN template t ON t.template_id = ftft.template_id
                    WHERE ftft.frame_test_form_id = ANY(@test_ids)
                    ORDER BY ftft.frame_test_form_id, t.template_id;
                    """;
                AddParameter(command, "test_ids", tests.Keys.ToArray());

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var testId = reader.GetInt32(0);
                    if (!tests.TryGetValue(testId, out var test))
                        continue;

                    var template = $"#{reader.GetInt32(1)} {reader.GetString(2)} ({reader.GetString(3)}), путь: {reader.GetString(4)}";
                    test.Templates.Add(template);
                }
            }

            return tests.Values.OrderBy(test => test.Id).ToList();
        }

        private static async Task<IReadOnlyList<ReportTestInfo>> LoadReportTestsAsync(
            AppDbContext dbContext,
            IReadOnlyCollection<int> frameIds,
            CancellationToken cancellationToken)
        {
            var result = new List<ReportTestInfo>();
            foreach (var frameId in frameIds)
            {
                var tests = await LoadReportTestsAsync(dbContext, frameId, cancellationToken);
                result.AddRange(tests);
            }

            return result
                .GroupBy(test => test.Id)
                .Select(group => group.First())
                .OrderBy(test => test.Id)
                .ToList();
        }

        private static async Task<Dictionary<int, FrameInspectionInfo>> LoadTemplateFrameMapAsync(
            AppDbContext dbContext,
            IReadOnlyCollection<int> frameIds,
            CancellationToken cancellationToken)
        {
            var rows = await (
                from frame in dbContext.Frames.AsNoTracking()
                join frameLink in dbContext.FrameTestFormFrames.AsNoTracking() on frame.FrameId equals frameLink.FrameId
                join templateLink in dbContext.FrameTestFormTemplates.AsNoTracking() on frameLink.FrameTestFormId equals templateLink.FrameTestFormId
                join template in dbContext.Templates.AsNoTracking() on templateLink.TemplateId equals template.TemplateId
                where frameIds.Contains(frame.FrameId)
                select new
                {
                    TemplateId = template.TemplateId,
                    FrameId = frame.FrameId,
                    FrameName = frame.Name,
                    frame.Weight
                })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(row => row.TemplateId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var row = group.First();
                        return new FrameInspectionInfo
                        {
                            FrameId = row.FrameId,
                            FrameName = row.FrameName,
                            ExpectedWeight = row.Weight.HasValue ? Convert.ToDouble(row.Weight.Value) : null
                        };
                    });
        }

        private static async Task<ReportDeviceInfo> LoadReportDevicesAsync(
            AppDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var cameraName = await dbContext.Cameras
                .AsNoTracking()
                .OrderBy(camera => camera.Name)
                .Select(camera => camera.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var sensorName = await dbContext.Sensors
                .AsNoTracking()
                .Where(sensor => sensor.SensorType.Code == "01")
                .OrderBy(sensor => sensor.Name)
                .Select(sensor => sensor.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return new ReportDeviceInfo
            {
                CameraName = cameraName,
                SensorName = sensorName
            };
        }

        private static string BuildQualityReport(
            Frame frame,
            ReportDeviceInfo devices,
            IReadOnlyList<ReportTestInfo> tests,
            IReadOnlyList<EdgeInspectionResultDto> results)
        {
            var passedCount = results.Count(result => IsPassedStatus(result.Status));
            var builder = new StringBuilder();

            builder.AppendLine("ОТЧЕТ О КОНТРОЛЕ КАЧЕСТВА");
            builder.AppendLine($"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            builder.AppendLine();

            builder.AppendLine("Модель каркаса");
            builder.AppendLine($"ID: {frame.FrameId}");
            builder.AppendLine($"Название: {frame.Name}");
            builder.AppendLine($"Материал: {frame.MaterialType?.Name ?? "не указан"}");
            builder.AppendLine($"Цех: {FormatWorkshop(frame.Workshop)}");
            builder.AppendLine($"Вес: {FormatDecimal(frame.Weight)}");
            builder.AppendLine($"Длина: {FormatDecimal(frame.Length)}");
            builder.AppendLine($"Ширина: {FormatDecimal(frame.Width)}");
            builder.AppendLine($"Высота: {FormatDecimal(frame.Height)}");
            builder.AppendLine($"Путь к фотографии: {EmptyIfNull(frame.ImagePath)}");
            builder.AppendLine();

            builder.AppendLine("Итоги контроля");
            builder.AppendLine($"Проверено каркасов: {results.Count}");
            builder.AppendLine($"Прошло контроль: {passedCount}");
            builder.AppendLine($"Требует внимания: {results.Count - passedCount}");
            builder.AppendLine();

            builder.AppendLine("Проведенные тесты");
            if (tests.Count == 0)
            {
                builder.AppendLine("Для выбранной модели каркаса тесты не найдены.");
            }
            else
            {
                for (var index = 0; index < tests.Count; index++)
                {
                    var test = tests[index];
                    builder.AppendLine($"{index + 1}. {test.Name}");
                    builder.AppendLine($"   Описание: {EmptyIfNull(test.Description)}");

                    builder.AppendLine(test.Templates.Count == 0
                        ? "   Шаблоны: не привязаны"
                        : $"   Шаблоны: {string.Join("; ", test.Templates)}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("Результаты по каркасам");
            if (results.Count == 0)
            {
                builder.AppendLine("Результаты контроля отсутствуют.");
            }
            else
            {
                builder.AppendLine("№\tВремя\tСтатус\tРезультат\tШаблон\tСходство\tТензодатчик (code 01)");
                foreach (var result in results)
                {
                    builder.AppendLine(
                        $"{result.ControlNumber}\t" +
                        $"{result.RecordedAt:dd.MM.yyyy HH:mm:ss}\t" +
                        $"{EmptyIfNull(result.Status)}\t" +
                        $"{EmptyIfNull(result.ResultText)}\t" +
                        $"{result.TemplateId}\t" +
                        $"{FormatDouble(result.Similarity, "F1", "%")}\t" +
                        $"{FormatDouble(result.Weight, "F0", string.Empty)}");
                }
            }

            return builder.ToString();
        }

        private static string BuildQualityReportV3(
            Frame frame,
            ReportDeviceInfo devices,
            IReadOnlyList<ReportTestInfo> tests,
            IReadOnlyList<EdgeInspectionResultDto> results)
        {
            var passedCount = results.Count(IsPassedResult);
            var builder = new StringBuilder();

            builder.AppendLine("ОТЧЕТ О КОНТРОЛЕ КАЧЕСТВА");
            builder.AppendLine($"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            builder.AppendLine();

            builder.AppendLine("Модель каркаса");
            builder.AppendLine($"ID: {frame.FrameId}");
            builder.AppendLine($"Название: {frame.Name}");
            builder.AppendLine($"Материал: {frame.MaterialType?.Name ?? "не указан"}");
            builder.AppendLine($"Цех: {ReportWorkshop(frame.Workshop)}");
            builder.AppendLine($"Вес по карточке: {ReportDecimal(frame.Weight)}");
            builder.AppendLine($"Длина: {ReportDecimal(frame.Length)}");
            builder.AppendLine($"Ширина: {ReportDecimal(frame.Width)}");
            builder.AppendLine($"Высота: {ReportDecimal(frame.Height)}");
            builder.AppendLine($"Путь к фотографии: {ReportText(frame.ImagePath)}");
            builder.AppendLine();

            builder.AppendLine("Средства контроля");
            builder.AppendLine($"Камера: {ReportText(devices.CameraName)}");
            builder.AppendLine($"Датчик веса: {ReportText(devices.SensorName)}");
            builder.AppendLine();

            builder.AppendLine("Итоги контроля");
            builder.AppendLine($"Проверено каркасов: {results.Count}");
            builder.AppendLine($"Прошло контроль: {passedCount}");
            builder.AppendLine($"Требует внимания: {results.Count - passedCount}");
            builder.AppendLine();

            builder.AppendLine("Проведенные тесты");
            if (tests.Count == 0)
            {
                builder.AppendLine("Для выбранной модели каркаса тесты не найдены.");
            }
            else
            {
                for (var index = 0; index < tests.Count; index++)
                {
                    var test = tests[index];
                    builder.AppendLine($"{index + 1}. {test.Name}");
                    builder.AppendLine($"   Описание: {ReportText(test.Description)}");
                    builder.AppendLine(test.Templates.Count == 0
                        ? "   Шаблоны: не привязаны"
                        : $"   Шаблоны: {string.Join("; ", test.Templates)}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("Результаты по каркасам");
            if (results.Count == 0)
            {
                builder.AppendLine("Результаты контроля отсутствуют.");
            }
            else
            {
                builder.AppendLine("№\tВремя\tСтатус\tШаблон\tСходство\tВес\tРезультат шаблона\tРезультат взвешивания");
                foreach (var result in results)
                {
                    builder.AppendLine(
                        $"{result.ControlNumber}\t" +
                        $"{result.RecordedAt:dd.MM.yyyy HH:mm:ss}\t" +
                        $"{ReportText(result.Status)}\t" +
                        $"{result.TemplateId}\t" +
                        $"{ReportDouble(result.Similarity, "F1", "%")}\t" +
                        $"{ReportDouble(result.Weight, "F0", string.Empty)}\t" +
                        $"{ReportText(result.TemplateResultText)}\t" +
                        $"{ReportText(result.WeightResultText)}");
                }
            }

            return builder.ToString();
        }

        private static string BuildQualityReportForFrames(
            IReadOnlyList<Frame> frames,
            ReportDeviceInfo devices,
            IReadOnlyList<ReportTestInfo> tests,
            IReadOnlyList<EdgeInspectionResultDto> results)
        {
            var passedCount = results.Count(IsPassedResult);
            var builder = new StringBuilder();

            builder.AppendLine("ОТЧЕТ О КОНТРОЛЕ КАЧЕСТВА");
            builder.AppendLine($"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            builder.AppendLine();

            builder.AppendLine(frames.Count == 1 ? "Модель каркаса" : "Модели каркаса");
            foreach (var frame in frames)
            {
                builder.AppendLine($"ID: {frame.FrameId}");
                builder.AppendLine($"Название: {frame.Name}");
                builder.AppendLine($"Материал: {frame.MaterialType?.Name ?? "не указан"}");
                builder.AppendLine($"Цех: {ReportWorkshop(frame.Workshop)}");
                builder.AppendLine($"Вес по карточке: {ReportDecimal(frame.Weight)}");
                builder.AppendLine($"Длина: {ReportDecimal(frame.Length)}");
                builder.AppendLine($"Ширина: {ReportDecimal(frame.Width)}");
                builder.AppendLine($"Высота: {ReportDecimal(frame.Height)}");
                builder.AppendLine($"Путь к фотографии: {ReportText(frame.ImagePath)}");
                builder.AppendLine();
            }

            builder.AppendLine("Средства контроля");
            builder.AppendLine($"Камера: {ReportText(devices.CameraName)}");
            builder.AppendLine($"Датчик веса: {ReportText(devices.SensorName)}");
            builder.AppendLine();

            builder.AppendLine("Итоги контроля");
            builder.AppendLine($"Проверено каркасов: {results.Count}");
            builder.AppendLine($"Прошло контроль: {passedCount}");
            builder.AppendLine($"Требует внимания: {results.Count - passedCount}");
            builder.AppendLine();

            builder.AppendLine("Проведенные тесты");
            if (tests.Count == 0)
            {
                builder.AppendLine("Для выбранных моделей каркаса тесты не найдены.");
            }
            else
            {
                for (var index = 0; index < tests.Count; index++)
                {
                    var test = tests[index];
                    builder.AppendLine($"{index + 1}. {test.Name}");
                    builder.AppendLine($"   Описание: {ReportText(test.Description)}");
                    builder.AppendLine(test.Templates.Count == 0
                        ? "   Шаблоны: не привязаны"
                        : $"   Шаблоны: {string.Join("; ", test.Templates)}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("Результаты по каркасам");
            if (results.Count == 0)
            {
                builder.AppendLine("Результаты контроля отсутствуют.");
            }
            else
            {
                builder.AppendLine("№\tВремя\tКаркас\tСтатус\tШаблон\tСходство\tВес\tРезультат шаблона\tРезультат взвешивания");
                foreach (var result in results)
                {
                    builder.AppendLine(
                        $"{result.ControlNumber}\t" +
                        $"{result.RecordedAt:dd.MM.yyyy HH:mm:ss}\t" +
                        $"{ReportText(result.FrameName)}\t" +
                        $"{ReportText(result.Status)}\t" +
                        $"{result.TemplateId}\t" +
                        $"{ReportDouble(result.Similarity, "F1", "%")}\t" +
                        $"{ReportDouble(result.Weight, "F0", string.Empty)}\t" +
                        $"{ReportText(result.TemplateResultText)}\t" +
                        $"{ReportText(result.WeightResultText)}");
                }
            }

            return builder.ToString();
        }

        private static string ReportWorkshop(Workshop? workshop)
        {
            if (workshop == null)
                return "не указан";

            return string.IsNullOrWhiteSpace(workshop.Purpose)
                ? workshop.Number
                : $"{workshop.Number} ({workshop.Purpose})";
        }

        private static string ReportDecimal(decimal? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : "не указано";
        }

        private static string ReportDouble(double? value, string format, string suffix)
        {
            return value.HasValue
                ? value.Value.ToString(format, CultureInfo.InvariantCulture) + suffix
                : "-";
        }

        private static string ReportText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "не указано" : value;
        }

        private static string BuildQualityReportV2(
            Frame frame,
            ReportDeviceInfo devices,
            IReadOnlyList<ReportTestInfo> tests,
            IReadOnlyList<EdgeInspectionResultDto> results)
        {
            var passedCount = results.Count(IsPassedResult);
            var builder = new StringBuilder();

            builder.AppendLine("ОТЧЕТ О КОНТРОЛЕ КАЧЕСТВА");
            builder.AppendLine($"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            builder.AppendLine();

            builder.AppendLine("Модель каркаса");
            builder.AppendLine($"ID: {frame.FrameId}");
            builder.AppendLine($"Название: {frame.Name}");
            builder.AppendLine($"Материал: {frame.MaterialType?.Name ?? "не указан"}");
            builder.AppendLine($"Цех: {FormatWorkshop(frame.Workshop)}");
            builder.AppendLine($"Вес по карточке: {FormatDecimal(frame.Weight)}");
            builder.AppendLine($"Длина: {FormatDecimal(frame.Length)}");
            builder.AppendLine($"Ширина: {FormatDecimal(frame.Width)}");
            builder.AppendLine($"Высота: {FormatDecimal(frame.Height)}");
            builder.AppendLine($"Путь к фотографии: {EmptyIfNull(frame.ImagePath)}");
            builder.AppendLine();

            builder.AppendLine("Средства контроля");
            builder.AppendLine($"Камера: {EmptyIfNull(devices.CameraName)}");
            builder.AppendLine($"Датчик веса: {EmptyIfNull(devices.SensorName)}");
            builder.AppendLine();

            builder.AppendLine("Итоги контроля");
            builder.AppendLine($"Проверено каркасов: {results.Count}");
            builder.AppendLine($"Прошло контроль: {passedCount}");
            builder.AppendLine($"Требует внимания: {results.Count - passedCount}");
            builder.AppendLine();

            builder.AppendLine("Проведенные тесты");
            if (tests.Count == 0)
            {
                builder.AppendLine("Для выбранной модели каркаса тесты не найдены.");
            }
            else
            {
                for (var index = 0; index < tests.Count; index++)
                {
                    var test = tests[index];
                    builder.AppendLine($"{index + 1}. {test.Name}");
                    builder.AppendLine($"   Описание: {EmptyIfNull(test.Description)}");
                    builder.AppendLine(test.Templates.Count == 0
                        ? "   Шаблоны: не привязаны"
                        : $"   Шаблоны: {string.Join("; ", test.Templates)}");
                }
            }
            builder.AppendLine();

            builder.AppendLine("Результаты по каркасам");
            if (results.Count == 0)
            {
                builder.AppendLine("Результаты контроля отсутствуют.");
            }
            else
            {
                builder.AppendLine("№\tВремя\tСтатус\tШаблон\tСходство\tВес\tРезультат шаблона\tРезультат взвешивания");
                foreach (var result in results)
                {
                    builder.AppendLine(
                        $"{result.ControlNumber}\t" +
                        $"{result.RecordedAt:dd.MM.yyyy HH:mm:ss}\t" +
                        $"{EmptyIfNull(result.Status)}\t" +
                        $"{result.TemplateId}\t" +
                        $"{FormatDouble(result.Similarity, "F1", "%")}\t" +
                        $"{FormatDouble(result.Weight, "F0", string.Empty)}\t" +
                        $"{EmptyIfNull(result.TemplateResultText)}\t" +
                        $"{EmptyIfNull(result.WeightResultText)}");
                }
            }

            return builder.ToString();
        }

        private static void ApplyWeightCheck(IEnumerable<EdgeInspectionResultDto> results, double? expectedWeight)
        {
            foreach (var result in results)
            {
                result.ExpectedWeight = expectedWeight;
                result.WeightTolerance = expectedWeight.HasValue
                    ? EdgeInspectionResultDto.GetDefaultWeightTolerance(expectedWeight.Value)
                    : null;
            }
        }

        private static void ApplyFrameChecks(
            IEnumerable<EdgeInspectionResultDto> results,
            IReadOnlyDictionary<int, FrameInspectionInfo> templateFrameMap,
            IReadOnlyList<Frame> selectedFrames)
        {
            var fallbackFrame = selectedFrames.Count == 1 ? selectedFrames[0] : null;
            foreach (var result in results)
            {
                if (templateFrameMap.TryGetValue(result.TemplateId, out var frameInfo))
                {
                    result.FrameId = frameInfo.FrameId;
                    result.FrameName = frameInfo.FrameName;
                    result.ExpectedWeight = frameInfo.ExpectedWeight;
                }
                else if (fallbackFrame != null)
                {
                    result.FrameId = fallbackFrame.FrameId;
                    result.FrameName = fallbackFrame.Name;
                    result.ExpectedWeight = fallbackFrame.Weight.HasValue ? Convert.ToDouble(fallbackFrame.Weight.Value) : null;
                }

                result.WeightTolerance = result.ExpectedWeight.HasValue
                    ? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value)
                    : null;
            }
        }

        private static List<int> NormalizeFrameIds(IEnumerable<int> frameIds)
        {
            return frameIds
                .Where(frameId => frameId > 0)
                .Distinct()
                .ToList();
        }

        private static bool IsPassedResult(EdgeInspectionResultDto result)
        {
            if (!IsPassedStatusForQualityResult(result.Status))
                return false;

            if (!result.ExpectedWeight.HasValue || !result.Weight.HasValue)
                return false;

            var tolerance = result.WeightTolerance ?? EdgeInspectionResultDto.GetDefaultWeightTolerance(result.ExpectedWeight.Value);
            return Math.Abs(result.Weight.Value - result.ExpectedWeight.Value) <= tolerance;
        }

        private static bool IsPassedStatusForQualityResult(string? status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Годен", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Passed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPassedStatus(string? status)
        {
            return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Годен", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Passed", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatWorkshop(Workshop? workshop)
        {
            if (workshop == null)
                return "не указан";

            return string.IsNullOrWhiteSpace(workshop.Purpose)
                ? workshop.Number
                : $"{workshop.Number} ({workshop.Purpose})";
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : "не указано";
        }

        private static string FormatDouble(double? value, string format, string suffix)
        {
            return value.HasValue
                ? value.Value.ToString(format, CultureInfo.InvariantCulture) + suffix
                : "-";
        }

        private static string EmptyIfNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "не указано" : value;
        }

        private async Task PrepareRemoteTemplatesForFrameAsync(int frameId, CancellationToken cancellationToken)
        {
            if (frameId <= 0)
                throw new InvalidOperationException("Выберите модель каркаса для запуска operating.");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var templatePaths = await (
                from test in dbContext.FrameTestForms.AsNoTracking()
                join frameLink in dbContext.FrameTestFormFrames.AsNoTracking() on test.FrameTestFormId equals frameLink.FrameTestFormId
                join templateLink in dbContext.FrameTestFormTemplates.AsNoTracking() on test.FrameTestFormId equals templateLink.FrameTestFormId
                join template in dbContext.Templates.AsNoTracking() on templateLink.TemplateId equals template.TemplateId
                where frameLink.FrameId == frameId
                select template.ImagePath)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (templatePaths.Count == 0)
                throw new InvalidOperationException("Для выбранной модели каркаса нет тестов с привязанными шаблонами.");

            var existingTemplatePaths = templatePaths
                .Where(path => Directory.Exists(path))
                .ToList();

            if (existingTemplatePaths.Count == 0)
                throw new InvalidOperationException("Локальные папки шаблонов из таблицы template не найдены на ПК.");

            await ClearRemoteTemplateDirectoryAsync(cancellationToken);
            await RunSshAsync($"mkdir -p {QuoteRemote(_options.RemoteTemplateDirectory.TrimEnd('/'))}", cancellationToken);
            var remoteTarget = $"{RemoteIdentity()}:{_options.RemoteTemplateDirectory.TrimEnd('/')}/";
            await RunProcessAsync("scp", BuildScpArgs(existingTemplatePaths, remoteTarget), cancellationToken);
        }

        private async Task PrepareRemoteTemplatesForFramesAsync(IEnumerable<int> frameIds, CancellationToken cancellationToken)
        {
            var selectedFrameIds = NormalizeFrameIds(frameIds);
            if (selectedFrameIds.Count == 0)
                throw new InvalidOperationException("Выберите модели каркаса для запуска operating.");

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var templateItems = await (
                from test in dbContext.FrameTestForms.AsNoTracking()
                join frameLink in dbContext.FrameTestFormFrames.AsNoTracking() on test.FrameTestFormId equals frameLink.FrameTestFormId
                join templateLink in dbContext.FrameTestFormTemplates.AsNoTracking() on test.FrameTestFormId equals templateLink.FrameTestFormId
                join template in dbContext.Templates.AsNoTracking() on templateLink.TemplateId equals template.TemplateId
                where selectedFrameIds.Contains(frameLink.FrameId)
                select new TemplateDeployItem
                {
                    TemplateId = template.TemplateId,
                    ImagePath = template.ImagePath
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (templateItems.Count == 0)
                throw new InvalidOperationException("Для выбранных моделей каркаса нет тестов с привязанными шаблонами.");

            var stagingRoot = Path.Combine(Path.GetTempPath(), "QualityControlSystem", "remote_templates", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(stagingRoot);
                foreach (var item in templateItems)
                {
                    if (!Directory.Exists(item.ImagePath))
                        continue;

                    StageTemplateDirectory(item.ImagePath, stagingRoot, item.TemplateId);
                }

                var stagedTemplatePaths = Directory.GetDirectories(stagingRoot).ToList();
                if (stagedTemplatePaths.Count == 0)
                    throw new InvalidOperationException("Локальные папки шаблонов из таблицы template не найдены на ПК.");

                await ClearRemoteTemplateDirectoryAsync(cancellationToken);
                await RunSshAsync($"mkdir -p {QuoteRemote(_options.RemoteTemplateDirectory.TrimEnd('/'))}", cancellationToken);
                var remoteTarget = $"{RemoteIdentity()}:{_options.RemoteTemplateDirectory.TrimEnd('/')}/";
                await RunProcessAsync("scp", BuildScpArgs(stagedTemplatePaths, remoteTarget), cancellationToken);
            }
            finally
            {
                TryDeleteDirectory(stagingRoot);
            }
        }

        private async Task ClearRemoteTemplateDirectoryAsync(CancellationToken cancellationToken)
        {
            var remoteDir = _options.RemoteTemplateDirectory.TrimEnd('/');
            await RunSshAsync($"mkdir -p {QuoteRemote(remoteDir)} && find {QuoteRemote(remoteDir)} -mindepth 1 -maxdepth 1 -exec rm -rf {{}} +", cancellationToken);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

            foreach (var directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }

        private static void StageTemplateDirectory(string source, string stagingRoot, int templateId)
        {
            var destination = Path.Combine(stagingRoot, templateId.ToString(CultureInfo.InvariantCulture));
            if (Directory.Exists(destination))
                Directory.Delete(destination, recursive: true);

            Directory.CreateDirectory(destination);
            var sourceTemplateId = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            CopyTemplateDirectory(source, destination, sourceTemplateId, templateId.ToString(CultureInfo.InvariantCulture));
        }

        private static void CopyTemplateDirectory(string source, string destination, string sourceTemplateId, string targetTemplateId)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                var fileName = RenameTemplateFile(Path.GetFileName(file), sourceTemplateId, targetTemplateId);
                File.Copy(file, Path.Combine(destination, fileName), overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyTemplateDirectory(
                    directory,
                    Path.Combine(destination, Path.GetFileName(directory)),
                    sourceTemplateId,
                    targetTemplateId);
            }
        }

        private static string RenameTemplateFile(string fileName, string sourceTemplateId, string targetTemplateId)
        {
            if (string.IsNullOrWhiteSpace(sourceTemplateId)
                || string.Equals(sourceTemplateId, targetTemplateId, StringComparison.OrdinalIgnoreCase))
                return fileName;

            var extension = Path.GetExtension(fileName);
            var name = Path.GetFileNameWithoutExtension(fileName);
            var suffix = "_" + sourceTemplateId;
            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? name[..^suffix.Length] + "_" + targetTemplateId + extension
                : fileName;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Temporary staging can be cleaned by the OS later.
            }
        }

        private string BuildStartCommand(string scriptName, int port, string logName, string pidName)
        {
            var remoteDir = _options.RemoteDirectory.TrimEnd('/');
            var python = $"{_options.VirtualEnvironmentPath.TrimEnd('/')}/bin/python";
            var scriptPath = $"{remoteDir}/{scriptName}";
            var logPath = $"{remoteDir}/{logName}";
            var pidPath = $"{remoteDir}/{pidName}";
            var safePattern = RegexEscapeForPkill(scriptName);
            var command =
                $"cd {QuoteRemote(remoteDir)} && " +
                $"rm -f {QuoteRemote(pidPath)} && " +
                $"setsid -f {QuoteRemote(python)} {QuoteRemote(scriptPath)} --host 0.0.0.0 --port {port.ToString(CultureInfo.InvariantCulture)} > {QuoteRemote(logPath)} 2>&1 < /dev/null; " +
                $"sleep 0.3; " +
                $"pgrep -f {QuoteRemote(safePattern)} | head -n 1 > {QuoteRemote(pidPath)} || true; " +
                $"echo started {scriptName} pid=$(cat {QuoteRemote(pidPath)} 2>/dev/null)";
            return $"bash -lc {QuoteRemote(command)}";
        }

        private string BuildStopCommand(string scriptName, string pidName)
        {
            var remoteDir = _options.RemoteDirectory.TrimEnd('/');
            var pidPath = $"{remoteDir}/{pidName}";
            var safePattern = RegexEscapeForPkill(scriptName);
            var command =
                $"if [ -f {QuoteRemote(pidPath)} ]; then " +
                $"pid=$(cat {QuoteRemote(pidPath)}); " +
                $"if [ -n \"$pid\" ]; then kill \"$pid\" 2>/dev/null || true; fi; " +
                $"rm -f {QuoteRemote(pidPath)}; " +
                $"fi; " +
                $"pkill -f {QuoteRemote(safePattern)} 2>/dev/null || true; " +
                $"echo stopped";
            return $"bash -lc {QuoteRemote(command)}";
        }

        private static string RegexEscapeForPkill(string scriptName)
        {
            if (string.IsNullOrWhiteSpace(scriptName))
                return scriptName;

            return $"[{scriptName[0]}]{scriptName[1..]}";
        }

        private async Task<string> RunSshAsync(string remoteCommand, CancellationToken cancellationToken)
        {
            return await RunProcessAsync("ssh", BuildSshArgs(remoteCommand), cancellationToken);
        }

        private List<string> BuildSshArgs(string remoteCommand)
        {
            var args = new List<string>
            {
                "-p",
                _options.SshPort.ToString(CultureInfo.InvariantCulture),
                "-o",
                "ConnectTimeout=10",
                "-o",
                "StrictHostKeyChecking=accept-new",
                "-o",
                "BatchMode=yes",
                "-o",
                "NumberOfPasswordPrompts=0"
            };

            if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPath))
            {
                args.Add("-i");
                args.Add(_options.PrivateKeyPath);
            }

            args.Add(RemoteIdentity());
            args.Add(remoteCommand);
            return args;
        }

        private List<string> BuildScpArgs(IEnumerable<string> sources, string target)
        {
            var args = new List<string>
            {
                "-r",
                "-P",
                _options.SshPort.ToString(CultureInfo.InvariantCulture),
                "-o",
                "ConnectTimeout=10",
                "-o",
                "StrictHostKeyChecking=accept-new",
                "-o",
                "BatchMode=yes",
                "-o",
                "NumberOfPasswordPrompts=0"
            };

            if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPath))
            {
                args.Add("-i");
                args.Add(_options.PrivateKeyPath);
            }

            args.AddRange(sources);
            args.Add(target);
            return args;
        }

        private async Task<string> RunProcessAsync(string fileName, IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo(fileName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            var output = new StringBuilder();
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => AppendLine(output, e.Data);
            process.ErrorDataReceived += (_, e) => AppendLine(output, e.Data);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось запустить {fileName}. Проверьте, что OpenSSH установлен в Windows. {ex.Message}", ex);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _options.SshCommandTimeoutSeconds)));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw new TimeoutException(
                    $"{fileName} не завершился за {_options.SshCommandTimeoutSeconds} сек. " +
                    $"Скорее всего SSH ждет пароль в скрытом процессе. " +
                    $"Настройте вход по ключу для {RemoteIdentity()} или укажите PrivateKeyPath в appsettings.json.");
            }

            var text = output.ToString().Trim();
            if (process.ExitCode != 0)
                throw new InvalidOperationException(BuildProcessError(fileName, process.ExitCode, text));

            return text;
        }

        private string BuildProcessError(string fileName, int exitCode, string output)
        {
            var message = string.IsNullOrWhiteSpace(output)
                ? $"{fileName} завершился с кодом {exitCode}"
                : output;

            if (message.Contains("Permission denied", StringComparison.OrdinalIgnoreCase)
                || message.Contains("publickey", StringComparison.OrdinalIgnoreCase)
                || message.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                return message + Environment.NewLine + Environment.NewLine +
                    $"Приложение запускает SSH без интерактивного ввода пароля. " +
                    $"Проверьте, что команда `ssh {RemoteIdentity()}` входит без запроса пароля, " +
                    $"или укажите путь к приватному ключу в EdgeDevice:PrivateKeyPath.";
            }

            return message;
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // The caller already gets the timeout reason.
            }
        }

        private static void AddParameter(IDbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static void AppendLine(StringBuilder builder, string? line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                builder.AppendLine(line);
        }

        private string RemoteIdentity() => $"{_options.UserName}@{_options.Host}";

        private static string QuoteRemote(string value) => "'" + value.Replace("'", "'\"'\"'") + "'";

        private string ResolveLocalTemplateDirectory()
        {
            if (Path.IsPathRooted(_options.LocalTemplateDirectory))
                return _options.LocalTemplateDirectory;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.LocalTemplateDirectory);
        }

        private static string FindProjectScript(string fileName)
        {
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "EdgeScripts", fileName);
                if (File.Exists(candidate))
                    return candidate;

                current = current.Parent;
            }

            throw new FileNotFoundException($"Не найден wrapper-скрипт {fileName}");
        }

        private sealed class ReportTestInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<string> Templates { get; } = new();
        }

        private sealed class ReportDeviceInfo
        {
            public string? CameraName { get; set; }
            public string? SensorName { get; set; }
        }

        private sealed class TemplateDeployItem
        {
            public int TemplateId { get; set; }
            public string ImagePath { get; set; } = string.Empty;
        }

        private sealed class FrameInspectionInfo
        {
            public int FrameId { get; set; }
            public string FrameName { get; set; } = string.Empty;
            public double? ExpectedWeight { get; set; }
        }
    }

    internal static class JsonElementExtensions
    {
        public static string? GetPropertyOrDefault(this JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) ? value.ToString() : null;
        }
    }
}
