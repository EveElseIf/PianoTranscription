using System;

namespace PianoTranscription.Core;

internal static class Extension
{
    public static long GetLength(this Range r) => r.End.Value - r.Start.Value;

    public static float[,] Read(this float[,] input, Range i, Range j)
    {
        var output = new float[i.GetLength(), j.GetLength()];
        for (var x = 0; x < i.GetLength(); x++)
        {
            for (var y = 0; y < j.GetLength(); y++)
            {
                output[x, y] = input[x + i.Start.Value, y + j.Start.Value];
            }
        }

        return output;
    }

    public static void Write(this float[,] dst, Range i, Range j, float[,] src)
    {
        for (var x = 0; x < i.GetLength(); x++)
        {
            for (var y = 0; y < j.GetLength(); y++)
            {
                dst[x + i.Start.Value, y + j.Start.Value] = src[x, y];
            }
        }
    }
}