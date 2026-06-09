using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services;

public sealed class OperatorControlDataService : IOperatorControlDataService
{
    private readonly AppDbContext _dbContext;

    public OperatorControlDataService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<int, OperatorFrameInspectionInfoDto>> GetTemplateFrameMapAsync(
        IReadOnlyCollection<int> frameIds,
        CancellationToken cancellationToken = default)
    {
        if (frameIds.Count == 0)
            return new Dictionary<int, OperatorFrameInspectionInfoDto>();

        var rows = await (
            from frame in _dbContext.Frames.AsNoTracking()
            join frameLink in _dbContext.FrameTestFormFrames.AsNoTracking() on frame.FrameId equals frameLink.FrameId
            join templateLink in _dbContext.FrameTestFormTemplates.AsNoTracking() on frameLink.FrameTestFormId equals templateLink.FrameTestFormId
            join template in _dbContext.Templates.AsNoTracking() on templateLink.TemplateId equals template.TemplateId
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
                    return new OperatorFrameInspectionInfoDto
                    {
                        FrameId = row.FrameId,
                        FrameName = row.FrameName,
                        ExpectedWeight = row.Weight.HasValue ? Convert.ToDouble(row.Weight.Value) : null
                    };
                });
    }
}
