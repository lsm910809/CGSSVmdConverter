using System.Numerics;

namespace CGSSVmdConverter.Models;

public sealed class VmdBoneFrame
{
    public string BoneName { get; init; } = "";

    public uint Frame { get; init; }

    public Vector3 Position { get; init; }

    public Quaternion Rotation { get; init; }
}