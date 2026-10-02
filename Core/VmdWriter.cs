using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CGSSVmdConverter.Models;

namespace CGSSVmdConverter.Core;

public static class VmdWriter
{
    private static readonly Encoding ShiftJis;

    private static readonly byte[]
        LinearInterpolation =
            CreateLinearInterpolation();

    static VmdWriter()
    {
        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        ShiftJis =
            Encoding.GetEncoding(932);
    }

    public static void Write(
        string path,
        string modelName,
        IReadOnlyList<VmdBoneFrame> frames)
    {
        using FileStream stream =
            File.Create(path);

        using BinaryWriter writer =
            new(stream);

        // ====================================================
        // VMD HEADER
        // ====================================================

        WriteFixedAscii(
            writer,
            "Vocaloid Motion Data 0002",
            30);

        WriteFixedShiftJis(
            writer,
            modelName,
            20);

        // ====================================================
        // BONE FRAMES
        // ====================================================

        writer.Write(
            checked((uint)frames.Count));

        foreach (VmdBoneFrame frame
                 in frames)
        {
            // Bone name
            WriteFixedShiftJis(
                writer,
                frame.BoneName,
                15);

            // Frame number
            writer.Write(
                frame.Frame);

            // Position
            writer.Write(
                frame.Position.X);

            writer.Write(
                frame.Position.Y);

            writer.Write(
                frame.Position.Z);

            // Rotation quaternion
            writer.Write(
                frame.Rotation.X);

            writer.Write(
                frame.Rotation.Y);

            writer.Write(
                frame.Rotation.Z);

            writer.Write(
                frame.Rotation.W);

            // 64-byte interpolation block
            writer.Write(
                LinearInterpolation);
        }

        // ====================================================
        // MORPH FRAMES
        // ====================================================

        writer.Write((uint)0);

        // ====================================================
        // CAMERA FRAMES
        // ====================================================

        writer.Write((uint)0);

        // ====================================================
        // LIGHT FRAMES
        // ====================================================

        writer.Write((uint)0);

        // ====================================================
        // SELF-SHADOW FRAMES
        // ====================================================

        writer.Write((uint)0);

        // ====================================================
        // MODEL DISPLAY / IK FRAMES
        // ====================================================
        //
        // CGSS motion is FK-driven.
        //
        // The motion directly rotates:
        //
        //   左足
        //   左ひざ
        //   左足首
        //
        //   右足
        //   右ひざ
        //   右足首
        //
        // Therefore MMD's leg/toe IK must NOT fight those
        // rotations.
        //
        // Put one model/IK frame at frame 0 and explicitly
        // disable:
        //
        //   左足ＩＫ
        //   右足ＩＫ
        //   左つま先ＩＫ
        //   右つま先ＩＫ
        //
        // The state persists until another IK frame changes it.
        // ====================================================

        // Number of model display / IK frames
        writer.Write((uint)1);

        // ----------------------------------------------------
        // IK FRAME 0
        // ----------------------------------------------------

        // Frame number
        writer.Write((uint)0);

        // Model display flag:
        //
        // 1 = visible
        // 0 = hidden
        writer.Write((byte)1);

        // Number of IK entries in this frame
        writer.Write((uint)4);

        // ----------------------------------------------------
        // LEFT LEG IK -> OFF
        // ----------------------------------------------------

        WriteFixedShiftJis(
            writer,
            "左足ＩＫ",
            20);

        // 0 = disabled
        writer.Write((byte)0);

        // ----------------------------------------------------
        // RIGHT LEG IK -> OFF
        // ----------------------------------------------------

        WriteFixedShiftJis(
            writer,
            "右足ＩＫ",
            20);

        // 0 = disabled
        writer.Write((byte)0);

        // ----------------------------------------------------
        // LEFT TOE IK -> OFF
        // ----------------------------------------------------

        WriteFixedShiftJis(
            writer,
            "左つま先ＩＫ",
            20);

        // 0 = disabled
        writer.Write((byte)0);

        // ----------------------------------------------------
        // RIGHT TOE IK -> OFF
        // ----------------------------------------------------

        WriteFixedShiftJis(
            writer,
            "右つま先ＩＫ",
            20);

        // 0 = disabled
        writer.Write((byte)0);
    }

    // ========================================================
    // FIXED-LENGTH ASCII STRING
    // ========================================================

    private static void WriteFixedAscii(
        BinaryWriter writer,
        string text,
        int length)
    {
        byte[] source =
            Encoding.ASCII.GetBytes(text);

        byte[] target =
            new byte[length];

        Array.Copy(
            source,
            target,
            Math.Min(
                source.Length,
                target.Length));

        writer.Write(target);
    }

    // ========================================================
    // FIXED-LENGTH SHIFT-JIS STRING
    // ========================================================

    private static void WriteFixedShiftJis(
        BinaryWriter writer,
        string text,
        int length)
    {
        byte[] source =
            ShiftJis.GetBytes(text);

        byte[] target =
            new byte[length];

        int sourceIndex = 0;
        int targetIndex = 0;

        /*
         * VMD uses fixed-size Shift-JIS fields.
         *
         * Don't cut a two-byte Japanese character in half
         * if it happens to land on the end of the field.
         */

        while (sourceIndex <
               source.Length)
        {
            byte first =
                source[sourceIndex];

            bool doubleByte =
                (first >= 0x81 &&
                 first <= 0x9F) ||
                (first >= 0xE0 &&
                 first <= 0xFC);

            int count =
                doubleByte
                    ? 2
                    : 1;

            if (sourceIndex + count >
                source.Length)
            {
                break;
            }

            if (targetIndex + count >
                length)
            {
                break;
            }

            for (int j = 0;
                 j < count;
                 j++)
            {
                target[targetIndex++] =
                    source[sourceIndex++];
            }
        }

        writer.Write(target);
    }

    // ========================================================
    // DEFAULT LINEAR INTERPOLATION
    // ========================================================

    private static byte[]
        CreateLinearInterpolation()
    {
        byte[] result =
            new byte[64];

        /*
         * Standard linear VMD Bezier:
         *
         *      (20, 20)
         *          ↓
         *      (107, 107)
         *
         * Applied to:
         *
         *   X position
         *   Y position
         *   Z position
         *   Rotation
         */

        for (int c = 0;
             c < 4;
             c++)
        {
            result[c] = 20;
            result[4 + c] = 20;
            result[8 + c] = 107;
            result[12 + c] = 107;

            result[16 + c] = 20;
            result[20 + c] = 20;
            result[24 + c] = 107;
            result[28 + c] = 107;

            result[32 + c] = 20;
            result[36 + c] = 20;
            result[40 + c] = 107;
            result[44 + c] = 107;

            result[48 + c] = 20;
            result[52 + c] = 20;
            result[56 + c] = 107;
            result[60 + c] = 107;
        }

        return result;
    }
}