using System;

namespace QualityControlSystem.WPF.Models
{
    public class EdgeInspectionResultDto
    {
        public int ControlNumber { get; set; }
        public int Id { get; set; }
        public DateTime RecordedAt { get; set; }
        public string? Status { get; set; }
        public int TemplateId { get; set; }
        public double? Similarity { get; set; }
        public double? ContourScore { get; set; }
        public double? FilledScore { get; set; }
        public double? ShapeScore { get; set; }
        public string? Reason { get; set; }
        public double? Weight { get; set; }
        public double? ExpectedWeight { get; set; }
        public double? WeightTolerance { get; set; }
        public double? LightPercent { get; set; }
        public double? LightAdc { get; set; }

        public string TemplateResultText => string.IsNullOrWhiteSpace(Reason) ? Status ?? string.Empty : Reason;

        public string ResultText => TemplateResultText;

        public string WeightResultText
        {
            get
            {
                if (!ExpectedWeight.HasValue)
                    return "Вес модели не указан";

                if (!Weight.HasValue)
                    return "Нет данных с тензодатчика";

                var tolerance = WeightTolerance ?? GetDefaultWeightTolerance(ExpectedWeight.Value);
                var difference = Math.Abs(Weight.Value - ExpectedWeight.Value);
                return difference <= tolerance
                    ? $"OK, отклонение {difference:F0}"
                    : $"Отклонение веса {difference:F0}";
            }
        }

        public static double GetDefaultWeightTolerance(double expectedWeight)
        {
            return Math.Max(1.0, Math.Abs(expectedWeight) * 0.05);
        }
    }
}
