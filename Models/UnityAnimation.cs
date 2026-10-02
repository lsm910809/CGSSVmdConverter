using System.Collections.Generic;

namespace CGSSVmdConverter.Models;

public sealed class UnityAnimation
{
    public string Name { get; set; } = "CGSS";

    public float Length { get; set; }

    public float SampleRate { get; set; } = 60.0f;

    public Dictionary<string, QuaternionCurve>
        RotationCurves { get; } =
        new();

    public Dictionary<string, Vector3Curve>
        PositionCurves { get; } =
        new();
}