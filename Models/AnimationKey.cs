namespace CGSSVmdConverter.Models;

public sealed class AnimationKey<T>
{
    public float Time { get; init; }

    public T Value { get; init; } = default!;

    public T InSlope { get; init; } = default!;

    public T OutSlope { get; init; } = default!;
}