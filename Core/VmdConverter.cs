using System;
using System.Collections.Generic;
using System.Numerics;
using CGSSVmdConverter.Models;

namespace CGSSVmdConverter.Core;

public static class VmdConverter
{
    public const float OutputFps = 30.0f;
    public const float PositionScale = 12.5f;

    private const string RootPath = "Position/Root";

    /*
     * Derived from the matching known-good pair:
     *
     * an_chr_son3087_01_legacy.anim
     * Motion.vmd
     *
     * Base Unity -> MMD coordinate conversion:
     *
     * rotation: (-X, +Y, -Z, +W)
     * position: (-X, +Y, -Z)
     *
     * Then each MMD bone receives a fixed rest/bind-space
     * correction:
     *
     *     qMmd = A * qConverted * B
     *
     * Waist and Hip are special because the reference VMD
     * folds the CGSS Root rotation into them.
     */

    private sealed record BoneRule(
        string MmdName,
        Quaternion A,
        Quaternion B);

    private static Quaternion Q(
        float x,
        float y,
        float z,
        float w)
    {
        return Quaternion.Normalize(
            new Quaternion(x, y, z, w));
    }

    private static readonly Dictionary<string, BoneRule> Rules =
        new(StringComparer.Ordinal)
        {
            ["Waist"] = new(
                "上半身",
                Q(0.000000647f, -0.000009019f, 0.000004320f, 1.000000000f),
                Q(-0.000000086f, 0.000009369f, -0.000003766f, 1.000000000f)),

            ["Chest"] = new(
                "上半身2",
                Q(-0.000012559f, -0.000009641f, 0.000003326f, 1.000000000f),
                Q(0.000011354f, 0.000009264f, -0.000004340f, 1.000000000f)),

            ["Shoulder_L"] = new(
                "左肩",
                Q(0.000014926f, 0.000014685f, 0.000011841f, 1.000000000f),
                Q(-0.000016803f, 0.016567164f, -0.000013100f, 0.999862755f)),

            ["Arm_L"] = new(
                "左腕",
                Q(-0.000000317f, -0.016578231f, 0.000001730f, 0.999862572f),
                Q(-0.018173173f, -0.021451947f, 0.325058465f, 0.945275909f)),

            ["Elbow_L"] = new(
                "左ひじ",
                Q(0.018173877f, 0.021446207f, -0.325063525f, 0.945274286f),
                Q(-0.003693939f, 0.020629557f, 0.325544052f, 0.945294582f)),

            ["Wrist_L"] = new(
                "左手首",
                Q(0.003695368f, -0.020623552f, -0.325543666f, 0.945294840f),
                Q(-0.009660673f, 0.003413455f, 0.233288991f, 0.972353468f)),

            ["Shoulder_R"] = new(
                "右肩",
                Q(-0.000002511f, 0.000001028f, -0.000047156f, 0.999999999f),
                Q(0.000004262f, -0.016575367f, 0.000046955f, 0.999862618f)),

            ["Arm_R"] = new(
                "右腕",
                Q(-0.000003178f, 0.016583791f, 0.000000218f, 0.999862479f),
                Q(-0.018173919f, 0.021447171f, -0.325057866f, 0.945276209f)),

            ["Elbow_R"] = new(
                "右ひじ",
                Q(0.018172415f, -0.021452483f, 0.325060458f, 0.945275226f),
                Q(-0.003695358f, -0.020624126f, -0.325547175f, 0.945293619f)),

            ["Wrist_R"] = new(
                "右手首",
                Q(0.003694815f, 0.020623030f, 0.325542730f, 0.945295176f),
                Q(-0.009662512f, -0.003413951f, -0.233293953f, 0.972352257f)),

            ["Neck"] = new(
                "首",
                Q(0.000020403f, 0.000009205f, 0.000009628f, 1.000000000f),
                Q(-0.000020707f, -0.000008932f, -0.000007797f, 1.000000000f)),

            ["Head"] = new(
                "頭",
                Q(-0.000000277f, 0.000003645f, -0.000005287f, 1.000000000f),
                Q(0.000000452f, -0.000004775f, 0.000003522f, 1.000000000f)),

            ["Hip"] = new(
                "下半身",
                Q(0.000005010f, -0.000000912f, -0.000001791f, 1.000000000f),
                Q(-0.000004696f, 0.000002429f, -0.000000640f, 1.000000000f)),

            ["Thigh_L"] = new(
                "左足",
                Q(-0.000002937f, -0.000002357f, 0.000002360f, 1.000000000f),
                Q(0.000002538f, 0.000003409f, -0.000000970f, 1.000000000f)),

            ["Knee_L"] = new(
                "左ひざ",
                Q(0.000003355f, -0.000003493f, 0.000002728f, 1.000000000f),
                Q(-0.000003106f, 0.000004139f, -0.000000738f, 1.000000000f)),

            ["Ankle_L"] = new(
                "左足首",
                Q(0.000007858f, 0.000013631f, 0.000005663f, 1.000000000f),
                Q(-0.000007809f, -0.000013357f, -0.000006014f, 1.000000000f)),

            ["Thigh_R"] = new(
                "右足",
                Q(-0.000004859f, 0.000004327f, 0.000005279f, 1.000000000f),
                Q(0.000001971f, -0.000007002f, -0.000005671f, 1.000000000f)),

            ["Knee_R"] = new(
                "右ひざ",
                Q(-0.000004154f, -0.000000069f, 0.000003072f, 1.000000000f),
                Q(0.000004619f, 0.000001250f, -0.000002202f, 1.000000000f)),

            ["Ankle_R"] = new(
                "右足首",
                Q(-0.000015803f, -0.000001775f, -0.000001984f, 1.000000000f),
                Q(0.000015400f, 0.000000287f, 0.000004261f, 1.000000000f)),

            // LEFT FINGERS

            ["Iindex_01_L"] = new(
                "左人指１",
                Q(0.009670903f, -0.003413263f, -0.233307980f, 0.972348811f),
                Q(0.003029443f, 0.054270482f, 0.225010791f, 0.972838980f)),

            ["Index_02_L"] = new(
                "左人指２",
                Q(-0.003034724f, -0.054273789f, -0.224982354f, 0.972845356f),
                Q(-0.000813324f, 0.041528843f, 0.246183783f, 0.968332711f)),

            ["Index_03_L"] = new(
                "左人指３",
                Q(0.004490646f, -0.043099125f, -0.184568765f, 0.981863876f),
                Q(-0.005707445f, 0.043326215f, 0.214466677f, 0.975753201f)),

            ["Middle_01_L"] = new(
                "左中指１",
                Q(0.009668640f, -0.003432910f, -0.233335816f, 0.972342085f),
                Q(-0.016101212f, -0.016568915f, 0.156080848f, 0.987474046f)),

            ["Middle_02_L"] = new(
                "左中指２",
                Q(0.024770033f, 0.018348541f, -0.441724848f, 0.896620843f),
                Q(-0.016004137f, -0.043264469f, 0.529012904f, 0.847359075f)),

            ["Middle_03_L"] = new(
                "左中指３",
                Q(0.015928784f, 0.042897737f, -0.557823210f, 0.828697366f),
                Q(-0.014363319f, -0.042413276f, 0.582880946f, 0.811322755f)),

            ["Ring_01_L"] = new(
                "左薬指１",
                Q(0.009667978f, -0.003410853f, -0.233329679f, 0.972343641f),
                Q(-0.046533863f, -0.092960651f, 0.102803215f, 0.989254475f)),

            ["Ring_02_L"] = new(
                "左薬指２",
                Q(0.105869457f, 0.091731238f, -0.577977181f, 0.803939933f),
                Q(-0.099308201f, -0.085162508f, 0.622533759f, 0.771580811f)),

            ["Ring_03_L"] = new(
                "左薬指３",
                Q(0.118795636f, 0.071644223f, -0.795484790f, 0.589880200f),
                Q(-0.108354454f, -0.055715862f, 0.842671201f, 0.524461917f)),

            ["Pinky_01_L"] = new(
                "左小指１",
                Q(0.009658246f, -0.003416864f, -0.233294647f, 0.972352123f),
                Q(-0.075223578f, -0.146059875f, 0.054103334f, 0.984926777f)),

            ["Pinky_02_L"] = new(
                "左小指２",
                Q(0.129513180f, 0.153992592f, -0.296788780f, 0.933503636f),
                Q(-0.121363538f, -0.169435933f, 0.321100438f, 0.923827292f)),

            ["Pinky_03_L"] = new(
                "左小指３",
                Q(0.020339789f, 0.151179520f, 0.157521352f, 0.975662887f),
                Q(-0.013125646f, -0.161069941f, -0.106890386f, 0.981049763f)),

            ["Thumb_01_L"] = new(
                "左親指０",
                Q(0.009603972f, -0.003415619f, -0.233302439f, 0.972350795f),
                Q(0.017848145f, -0.083316483f, 0.049265133f, 0.995144590f)),

            ["Thumb_02_L"] = new(
                "左親指１",
                Q(-0.017796445f, 0.083333111f, -0.049278214f, 0.995143475f),
                Q(0.070143224f, 0.227482963f, 0.138233166f, 0.961365186f)),

            ["Thumb_03_L"] = new(
                "左親指２",
                Q(-0.047912541f, -0.198852438f, -0.149344810f, 0.967397656f),
                Q(0.047913724f, 0.198852453f, 0.149344094f, 0.967397704f)),

            // RIGHT FINGERS

            ["Iindex_01_R"] = new(
                "右人指１",
                Q(0.009657253f, 0.003412797f, 0.233324649f, 0.972344948f),
                Q(0.003039366f, -0.054272203f, -0.225024746f, 0.972835625f)),

            ["Index_02_R"] = new(
                "右人指２",
                Q(-0.003034310f, 0.054273282f, 0.225001585f, 0.972840938f),
                Q(-0.000812948f, -0.041527619f, -0.246199937f, 0.968328657f)),

            ["Index_03_R"] = new(
                "右人指３",
                Q(0.003361889f, 0.042634760f, 0.203616205f, 0.978116259f),
                Q(-0.004593953f, -0.042777294f, -0.233397919f, 0.971429056f)),

            ["Middle_01_R"] = new(
                "右中指１",
                Q(0.009657414f, 0.003437668f, 0.233316641f, 0.972346781f),
                Q(-0.016104184f, 0.016561981f, -0.156060348f, 0.987477354f)),

            ["Middle_02_R"] = new(
                "右中指２",
                Q(0.005987973f, -0.013332921f, -0.143436973f, 0.989551521f),
                Q(-0.014222755f, 0.041233589f, 0.044309310f, 0.998065223f)),

            ["Middle_03_R"] = new(
                "右中指３",
                Q(0.015730344f, -0.044118588f, 0.225423523f, 0.973134288f),
                Q(-0.014397824f, 0.044662170f, -0.255043430f, 0.965790268f)),

            ["Ring_01_R"] = new(
                "右薬指１",
                Q(0.009651002f, 0.003411361f, 0.233250457f, 0.972362815f),
                Q(-0.046506372f, 0.092973323f, -0.102720643f, 0.989263154f)),

            ["Ring_02_R"] = new(
                "右薬指２",
                Q(0.049012062f, -0.093366962f, 0.121364547f, 0.986990919f),
                Q(-0.043674406f, 0.095487550f, -0.176291601f, 0.978721587f)),

            ["Ring_03_R"] = new(
                "右薬指３",
                Q(0.068473626f, -0.094105007f, 0.369789987f, 0.921797687f),
                Q(-0.059147933f, 0.091379880f, -0.445191097f, 0.888794761f)),

            ["Pinky_01_R"] = new(
                "右小指１",
                Q(0.009666226f, 0.003418627f, 0.233309483f, 0.972348478f),
                Q(-0.075232006f, 0.146055390f, -0.054118081f, 0.984925988f)),

            ["Pinky_02_R"] = new(
                "右小指２",
                Q(0.040243893f, -0.136769971f, -0.093512091f, 0.985357749f),
                Q(-0.039856267f, 0.157480437f, 0.069859951f, 0.984241321f)),

            ["Pinky_03_R"] = new(
                "右小指３",
                Q(0.077888090f, -0.166213931f, 0.107121286f, 0.977154750f),
                Q(-0.068995459f, 0.168953392f, -0.158194381f, 0.970396267f)),

            ["Thumb_01_R"] = new(
                "右親指０",
                Q(0.009661861f, 0.003418861f, 0.233284580f, 0.972354495f),
                Q(0.017795369f, 0.083326750f, -0.049271652f, 0.995144352f)),

            ["Thumb_02_R"] = new(
                "右親指１",
                Q(-0.017797473f, -0.083330875f, 0.049276761f, 0.995143716f),
                Q(0.070143525f, -0.227484307f, -0.138232868f, 0.961364889f)),

            ["Thumb_03_R"] = new(
                "右親指２",
                Q(-0.078877637f, 0.238682518f, 0.133792018f, 0.958597241f),
                Q(0.078878411f, -0.238682717f, -0.133791487f, 0.958597202f))
        };

    public static List<VmdBoneFrame> Convert(
        UnityAnimation animation,
        float fps = OutputFps)
    {
        if (animation is null)
            throw new ArgumentNullException(nameof(animation));

        if (fps <= 0)
            throw new ArgumentOutOfRangeException(nameof(fps));

        int frameCount =
            (int)MathF.Round(animation.Length * fps) + 1;

        if (!animation.RotationCurves.TryGetValue(
                RootPath,
                out QuaternionCurve? rootRotation))
        {
            throw new InvalidOperationException(
                "CGSS animation has no Position/Root rotation curve.");
        }

        animation.PositionCurves.TryGetValue(
            RootPath,
            out Vector3Curve? rootPosition);

        var result = new List<VmdBoneFrame>();

        // -----------------------------------------------------
        // CENTER
        //
        // The known-good VMD has NO center rotation.
        // Root translation goes here.
        // -----------------------------------------------------

        Quaternion previousCenter =
            Quaternion.Identity;

        for (int frame = 0; frame < frameCount; frame++)
        {
            float time =
                MathF.Min(frame / fps, animation.Length);

            Vector3 position =
                Vector3.Zero;

            if (rootPosition is not null)
            {
                Vector3 p =
                    CurveSampler.Sample(rootPosition, time);

                position =
                    ConvertRootPosition(p);
            }

            result.Add(
                new VmdBoneFrame
                {
                    BoneName = "センター",
                    Frame = (uint)frame,
                    Position = position,
                    Rotation = previousCenter
                });
        }

        // -----------------------------------------------------
        // ROTATION BONES
        // -----------------------------------------------------

        foreach (QuaternionCurve curve
                 in animation.RotationCurves.Values)
        {
            string leaf =
                GetLeaf(curve.Path);

            if (leaf == "Root")
                continue;

            // CGSS toes are not directly animated in the
            // known-good MMD motion. Foot IK handles this side.
            if (leaf == "Toe_L" ||
                leaf == "Toe_R")
            {
                continue;
            }

            if (!Rules.TryGetValue(
                    leaf,
                    out BoneRule? rule))
            {
                continue;
            }

            Quaternion? previous =
                null;

            for (int frame = 0;
                 frame < frameCount;
                 frame++)
            {
                float time =
                    MathF.Min(
                        frame / fps,
                        animation.Length);

                Quaternion source;

                /*
                 * CGSS hierarchy:
                 *
                 *       Root
                 *       /  \
                 *   Waist  Hip
                 *
                 * MMD reference folds Root rotation into
                 * 上半身 and 下半身 instead of rotating センター.
                 */

                if (leaf == "Waist" ||
                    leaf == "Hip")
                {
                    Quaternion root =
                        CurveSampler.Sample(
                            rootRotation,
                            time);

                    Quaternion local =
                        CurveSampler.Sample(
                            curve,
                            time);

                    source =
                        Multiply(root, local);
                }
                else
                {
                    source =
                        CurveSampler.Sample(
                            curve,
                            time);
                }

                source =
                    NormalizeSafe(source);

                // Correct Unity -> MMD handedness.
                Quaternion converted =
                    MirrorRotation(source);

                // Rest/bind-space conversion.
                Quaternion rotation =
                    Multiply(
                        Multiply(
                            rule.A,
                            converted),
                        rule.B);

                rotation =
                    NormalizeSafe(rotation);

                // q and -q are identical orientations.
                // Keep signs continuous for VMD interpolation.
                if (previous.HasValue &&
                    Quaternion.Dot(
                        previous.Value,
                        rotation) < 0.0f)
                {
                    rotation =
                        Negate(rotation);
                }

                previous =
                    rotation;

                result.Add(
                    new VmdBoneFrame
                    {
                        BoneName =
                            rule.MmdName,

                        Frame =
                            (uint)frame,

                        Position =
                            Vector3.Zero,

                        Rotation =
                            rotation
                    });
            }
        }

        return result;
    }

    private static Quaternion MirrorRotation(
        Quaternion q)
    {
        q =
            NormalizeSafe(q);

        /*
         * Confirmed directly against Motion.vmd:
         *
         * CGSS:
         * ( X, Y, Z, W )
         *
         * MMD coordinate basis:
         * (-X, Y,-Z, W )
         */

        return new Quaternion(
            -q.X,
             q.Y,
            -q.Z,
             q.W);
    }

    private static Vector3 ConvertRootPosition(
        Vector3 p)
    {
        /*
         * This reproduces the known-good Motion.vmd.
         *
         * The X/Y/Z scale and signs were recovered directly
         * from the matching source animation.
         *
         * The fixed Y/Z correction is the CGSS-root ->
         * MMD-center rest-position difference.
         */

        return new Vector3(
            -p.X * PositionScale,

             p.Y * PositionScale
                - 12.7333939f,

            -p.Z * PositionScale
                - 0.0150908f);
    }

    private static string GetLeaf(
        string path)
    {
        int slash =
            path.LastIndexOf('/');

        return slash < 0
            ? path
            : path[(slash + 1)..];
    }

    /*
     * Quaternion multiplication.
     *
     * Deliberately implemented here instead of relying on
     * Quaternion.Concatenate(), whose ordering semantics are
     * easy to misread.
     */
    private static Quaternion Multiply(
        Quaternion a,
        Quaternion b)
    {
        return new Quaternion(
            a.W * b.X +
            a.X * b.W +
            a.Y * b.Z -
            a.Z * b.Y,

            a.W * b.Y -
            a.X * b.Z +
            a.Y * b.W +
            a.Z * b.X,

            a.W * b.Z +
            a.X * b.Y -
            a.Y * b.X +
            a.Z * b.W,

            a.W * b.W -
            a.X * b.X -
            a.Y * b.Y -
            a.Z * b.Z);
    }

    private static Quaternion NormalizeSafe(
        Quaternion q)
    {
        if (q.LengthSquared() <
            0.00000001f)
        {
            return Quaternion.Identity;
        }

        return Quaternion.Normalize(q);
    }

    private static Quaternion Negate(
        Quaternion q)
    {
        return new Quaternion(
            -q.X,
            -q.Y,
            -q.Z,
            -q.W);
    }
}