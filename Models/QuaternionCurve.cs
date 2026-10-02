using System.Collections.Generic;
using System.Numerics;

namespace CGSSVmdConverter.Models;

public sealed class QuaternionCurve
{
    public string Path { get; init; } = "";

    public List<AnimationKey<Quaternion>> Keys
    {
        get;
        init;
    } = new();
}