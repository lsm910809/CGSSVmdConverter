using System.Collections.Generic;
using System.Numerics;

namespace CGSSVmdConverter.Models;

public sealed class Vector3Curve
{
    public string Path { get; init; } = "";

    public List<AnimationKey<Vector3>> Keys
    {
        get;
        init;
    } = new();
}