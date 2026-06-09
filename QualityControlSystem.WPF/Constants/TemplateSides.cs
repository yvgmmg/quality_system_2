using System.Collections.Generic;

namespace QualityControlSystem.WPF.Constants;

public static class TemplateSides
{
    public const string Front = "front";
    public const string Left = "left";
    public const string Right = "right";
    public const string Top = "top";
    public const string Back = "back";

    public static readonly IReadOnlyList<string> All =
    [
        Front,
        Left,
        Right,
        Top,
        Back
    ];
}
