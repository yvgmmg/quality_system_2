using System;
using QualityControlSystem.Infrastructure.Enums;

namespace QualityControlSystem.WPF.Models;

public class RegulatoryInformationDto
{
    public int RegulatoryInformationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "equipment";
    public string? Description { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public MeasurementUnit Measurement { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }

    public string TypeDisplay => Type switch
    {
        "equipment" => "Оборудование",
        "frame" => "Каркас",
        _ => Type
    };

    public string MeasurementDisplay => Measurement.ToString() switch
    {
        "РјРј" => "мм",
        "СЃРј" => "см",
        "Рј" => "м",
        "Рі" => "г",
        "РєРі" => "кг",
        "С‚" => "т",
        "GradC" => "°C",
        "Grad" => "°",
        "Empty" => "%",
        "С€С‚" => "шт",
        "Рќ" => "Н",
        "РњРџР°" => "МПа",
        "Р’" => "В",
        "Рђ" => "А",
        "m_s" => "м/с",
        "m_kv" => "м²",
        "m_kub" => "м³",
        "bezrazm" => "безразм.",
        var value => value
    };
}
