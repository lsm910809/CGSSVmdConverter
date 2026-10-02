using System;
using System.Collections.Generic;
using System.Numerics;
using CGSSVmdConverter.Models;

namespace CGSSVmdConverter.Core;

public static class CurveSampler
{
    public static Quaternion Sample(
        QuaternionCurve curve,
        float time)
    {
        if (curve.Keys.Count == 0)
            return Quaternion.Identity;

        AnimationKey<Quaternion> a;
        AnimationKey<Quaternion> b;

        if (!FindPair(
                curve.Keys,
                time,
                out a,
                out b))
        {
            return NormalizeSafe(
                curve.Keys[0].Value);
        }

        if (ReferenceEquals(a, b))
        {
            return NormalizeSafe(a.Value);
        }

        float dt =
            b.Time - a.Time;

        if (dt <= 0.0f)
            return NormalizeSafe(a.Value);

        float u =
            (time - a.Time) / dt;

        u =
            Math.Clamp(u, 0.0f, 1.0f);

        float u2 =
            u * u;

        float u3 =
            u2 * u;

        float h00 =
            2 * u3 - 3 * u2 + 1;

        float h10 =
            u3 - 2 * u2 + u;

        float h01 =
            -2 * u3 + 3 * u2;

        float h11 =
            u3 - u2;

        Quaternion q =
            new(
                Hermite(
                    a.Value.X,
                    a.OutSlope.X,
                    b.Value.X,
                    b.InSlope.X,
                    dt,
                    h00,
                    h10,
                    h01,
                    h11),

                Hermite(
                    a.Value.Y,
                    a.OutSlope.Y,
                    b.Value.Y,
                    b.InSlope.Y,
                    dt,
                    h00,
                    h10,
                    h01,
                    h11),

                Hermite(
                    a.Value.Z,
                    a.OutSlope.Z,
                    b.Value.Z,
                    b.InSlope.Z,
                    dt,
                    h00,
                    h10,
                    h01,
                    h11),

                Hermite(
                    a.Value.W,
                    a.OutSlope.W,
                    b.Value.W,
                    b.InSlope.W,
                    dt,
                    h00,
                    h10,
                    h01,
                    h11));

        return NormalizeSafe(q);
    }

    public static Vector3 Sample(
        Vector3Curve curve,
        float time)
    {
        if (curve.Keys.Count == 0)
            return Vector3.Zero;

        AnimationKey<Vector3> a;
        AnimationKey<Vector3> b;

        if (!FindPair(
                curve.Keys,
                time,
                out a,
                out b))
        {
            return curve.Keys[0].Value;
        }

        if (ReferenceEquals(a, b))
            return a.Value;

        float dt =
            b.Time - a.Time;

        if (dt <= 0.0f)
            return a.Value;

        float u =
            (time - a.Time) / dt;

        u =
            Math.Clamp(u, 0.0f, 1.0f);

        float u2 =
            u * u;

        float u3 =
            u2 * u;

        float h00 =
            2 * u3 - 3 * u2 + 1;

        float h10 =
            u3 - 2 * u2 + u;

        float h01 =
            -2 * u3 + 3 * u2;

        float h11 =
            u3 - u2;

        return new Vector3(
            Hermite(
                a.Value.X,
                a.OutSlope.X,
                b.Value.X,
                b.InSlope.X,
                dt,
                h00,
                h10,
                h01,
                h11),

            Hermite(
                a.Value.Y,
                a.OutSlope.Y,
                b.Value.Y,
                b.InSlope.Y,
                dt,
                h00,
                h10,
                h01,
                h11),

            Hermite(
                a.Value.Z,
                a.OutSlope.Z,
                b.Value.Z,
                b.InSlope.Z,
                dt,
                h00,
                h10,
                h01,
                h11));
    }

    private static float Hermite(
        float a,
        float aOut,
        float b,
        float bIn,
        float dt,
        float h00,
        float h10,
        float h01,
        float h11)
    {
        return
            h00 * a +
            h10 * dt * aOut +
            h01 * b +
            h11 * dt * bIn;
    }

    private static bool FindPair<T>(
        List<AnimationKey<T>> keys,
        float time,
        out AnimationKey<T> a,
        out AnimationKey<T> b)
    {
        if (time <= keys[0].Time)
        {
            a = keys[0];
            b = keys[0];
            return true;
        }

        if (time >= keys[^1].Time)
        {
            a = keys[^1];
            b = keys[^1];
            return true;
        }

        int low = 0;
        int high = keys.Count - 1;

        while (low <= high)
        {
            int mid =
                low + ((high - low) / 2);

            if (keys[mid].Time <= time)
            {
                low =
                    mid + 1;
            }
            else
            {
                high =
                    mid - 1;
            }
        }

        int right =
            Math.Clamp(
                low,
                1,
                keys.Count - 1);

        a = keys[right - 1];
        b = keys[right];

        return true;
    }

    private static Quaternion NormalizeSafe(
        Quaternion q)
    {
        float length =
            q.LengthSquared();

        if (length <
            0.00000001f)
        {
            return Quaternion.Identity;
        }

        return Quaternion.Normalize(q);
    }
}