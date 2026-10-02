using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using CGSSVmdConverter.Models;

namespace CGSSVmdConverter.Core;

public static class AnimParser
{
    private static readonly Regex PairRegex =
        new(
            @"([xyzw]):\s*([-+0-9.eE]+)",
            RegexOptions.Compiled);

    public static UnityAnimation Load(
        string path)
    {
        string[] lines =
            File.ReadAllLines(path);

        var result =
            new UnityAnimation
            {
                Name =
                    Path.GetFileNameWithoutExtension(path)
            };

        Section section =
            Section.None;

        int i = 0;

        while (i < lines.Length)
        {
            string line =
                lines[i].Trim();

            if (line == "m_RotationCurves:")
            {
                section =
                    Section.Rotation;

                i++;
                continue;
            }

            if (line == "m_PositionCurves:")
            {
                section =
                    Section.Position;

                i++;
                continue;
            }

            if (line.StartsWith(
                    "m_ScaleCurves:",
                    StringComparison.Ordinal) ||
                line.StartsWith(
                    "m_FloatCurves:",
                    StringComparison.Ordinal) ||
                line.StartsWith(
                    "m_CompressedRotationCurves:",
                    StringComparison.Ordinal) ||
                line.StartsWith(
                    "m_EulerCurves:",
                    StringComparison.Ordinal))
            {
                section =
                    Section.None;

                i++;
                continue;
            }

            if (line.StartsWith(
                    "m_SampleRate:",
                    StringComparison.Ordinal))
            {
                string value =
                    line[
                        (line.IndexOf(':') + 1)..]
                    .Trim();

                if (float.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float rate))
                {
                    result.SampleRate =
                        rate;
                }

                i++;
                continue;
            }

            if (line.StartsWith(
                    "m_Name:",
                    StringComparison.Ordinal))
            {
                string name =
                    line[
                        (line.IndexOf(':') + 1)..]
                    .Trim();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    result.Name =
                        name;
                }

                i++;
                continue;
            }

            if (line == "- curve:" &&
                section != Section.None)
            {
                if (section ==
                    Section.Rotation)
                {
                    QuaternionCurve curve;

                    (curve, i) =
                        ParseQuaternionCurve(
                            lines,
                            i);

                    if (!string.IsNullOrWhiteSpace(
                            curve.Path))
                    {
                        result.RotationCurves[
                            curve.Path] =
                            curve;
                    }
                }
                else
                {
                    Vector3Curve curve;

                    (curve, i) =
                        ParseVectorCurve(
                            lines,
                            i);

                    if (!string.IsNullOrWhiteSpace(
                            curve.Path))
                    {
                        result.PositionCurves[
                            curve.Path] =
                            curve;
                    }
                }

                continue;
            }

            i++;
        }

        if (result.RotationCurves.Count == 0)
        {
            throw new InvalidDataException(
                "No rotation curves were found. " +
                "Expected a Unity Legacy AnimationClip YAML .anim file.");
        }

        float duration = 0.0f;

        foreach (QuaternionCurve curve
                 in result.RotationCurves.Values)
        {
            if (curve.Keys.Count == 0)
                continue;

            duration =
                MathF.Max(
                    duration,
                    curve.Keys[^1].Time);
        }

        foreach (Vector3Curve curve
                 in result.PositionCurves.Values)
        {
            if (curve.Keys.Count == 0)
                continue;

            duration =
                MathF.Max(
                    duration,
                    curve.Keys[^1].Time);
        }

        result.Length =
            duration;

        return result;
    }

    private static (
        QuaternionCurve Curve,
        int NextIndex)
        ParseQuaternionCurve(
            string[] lines,
            int start)
    {
        var keys =
            new List<AnimationKey<Quaternion>>();

        string path = "";

        int i =
            start + 1;

        while (i < lines.Length)
        {
            string line =
                lines[i].Trim();

            if (line.StartsWith(
                    "path:",
                    StringComparison.Ordinal))
            {
                path =
                    line[
                        (line.IndexOf(':') + 1)..]
                    .Trim();

                return (
                    new QuaternionCurve
                    {
                        Path = path,
                        Keys = keys
                    },
                    i + 1);
            }

            if (line.StartsWith(
                    "time:",
                    StringComparison.Ordinal))
            {
                float time =
                    ParseFloatAfterColon(line);

                Quaternion value =
                    Quaternion.Identity;

                Quaternion inSlope =
                    new(0, 0, 0, 0);

                Quaternion outSlope =
                    new(0, 0, 0, 0);

                bool hasValue =
                    false;

                int j =
                    i + 1;

                while (j < lines.Length)
                {
                    string child =
                        lines[j].Trim();

                    if (child.StartsWith(
                            "time:",
                            StringComparison.Ordinal) ||
                        child.StartsWith(
                            "m_PreInfinity:",
                            StringComparison.Ordinal) ||
                        child.StartsWith(
                            "path:",
                            StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (child.StartsWith(
                            "value:",
                            StringComparison.Ordinal))
                    {
                        value =
                            ParseQuaternion(child);

                        hasValue =
                            true;
                    }
                    else if (child.StartsWith(
                                 "inSlope:",
                                 StringComparison.Ordinal))
                    {
                        inSlope =
                            ParseQuaternion(child);
                    }
                    else if (child.StartsWith(
                                 "outSlope:",
                                 StringComparison.Ordinal))
                    {
                        outSlope =
                            ParseQuaternion(child);
                    }

                    j++;
                }

                if (hasValue)
                {
                    keys.Add(
                        new AnimationKey<Quaternion>
                        {
                            Time = time,
                            Value = value,
                            InSlope = inSlope,
                            OutSlope = outSlope
                        });
                }

                i = j;
                continue;
            }

            i++;
        }

        throw new InvalidDataException(
            "Rotation curve has no path.");
    }

    private static (
        Vector3Curve Curve,
        int NextIndex)
        ParseVectorCurve(
            string[] lines,
            int start)
    {
        var keys =
            new List<AnimationKey<Vector3>>();

        string path = "";

        int i =
            start + 1;

        while (i < lines.Length)
        {
            string line =
                lines[i].Trim();

            if (line.StartsWith(
                    "path:",
                    StringComparison.Ordinal))
            {
                path =
                    line[
                        (line.IndexOf(':') + 1)..]
                    .Trim();

                return (
                    new Vector3Curve
                    {
                        Path = path,
                        Keys = keys
                    },
                    i + 1);
            }

            if (line.StartsWith(
                    "time:",
                    StringComparison.Ordinal))
            {
                float time =
                    ParseFloatAfterColon(line);

                Vector3 value =
                    Vector3.Zero;

                Vector3 inSlope =
                    Vector3.Zero;

                Vector3 outSlope =
                    Vector3.Zero;

                bool hasValue =
                    false;

                int j =
                    i + 1;

                while (j < lines.Length)
                {
                    string child =
                        lines[j].Trim();

                    if (child.StartsWith(
                            "time:",
                            StringComparison.Ordinal) ||
                        child.StartsWith(
                            "m_PreInfinity:",
                            StringComparison.Ordinal) ||
                        child.StartsWith(
                            "path:",
                            StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (child.StartsWith(
                            "value:",
                            StringComparison.Ordinal))
                    {
                        value =
                            ParseVector3(child);

                        hasValue =
                            true;
                    }
                    else if (child.StartsWith(
                                 "inSlope:",
                                 StringComparison.Ordinal))
                    {
                        inSlope =
                            ParseVector3(child);
                    }
                    else if (child.StartsWith(
                                 "outSlope:",
                                 StringComparison.Ordinal))
                    {
                        outSlope =
                            ParseVector3(child);
                    }

                    j++;
                }

                if (hasValue)
                {
                    keys.Add(
                        new AnimationKey<Vector3>
                        {
                            Time = time,
                            Value = value,
                            InSlope = inSlope,
                            OutSlope = outSlope
                        });
                }

                i = j;
                continue;
            }

            i++;
        }

        throw new InvalidDataException(
            "Position curve has no path.");
    }

    private static float ParseFloatAfterColon(
        string line)
    {
        string text =
            line[
                (line.IndexOf(':') + 1)..]
            .Trim();

        return float.Parse(
            text,
            CultureInfo.InvariantCulture);
    }

    private static Quaternion ParseQuaternion(
        string line)
    {
        Dictionary<char, float> values =
            ParseComponents(line);

        return new Quaternion(
            Get(values, 'x'),
            Get(values, 'y'),
            Get(values, 'z'),
            Get(values, 'w'));
    }

    private static Vector3 ParseVector3(
        string line)
    {
        Dictionary<char, float> values =
            ParseComponents(line);

        return new Vector3(
            Get(values, 'x'),
            Get(values, 'y'),
            Get(values, 'z'));
    }

    private static Dictionary<char, float>
        ParseComponents(
            string line)
    {
        var result =
            new Dictionary<char, float>();

        foreach (Match match
                 in PairRegex.Matches(line))
        {
            char component =
                match.Groups[1]
                    .Value[0];

            float value =
                float.Parse(
                    match.Groups[2].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);

            result[component] =
                value;
        }

        return result;
    }

    private static float Get(
        Dictionary<char, float> values,
        char component)
    {
        return values.TryGetValue(
            component,
            out float value)
            ? value
            : 0.0f;
    }

    private enum Section
    {
        None,
        Rotation,
        Position
    }
}