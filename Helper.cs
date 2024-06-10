using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;

namespace ToImageEncoder;

public static class Helper
{
    public static bool IsEven(byte value) => value % 2 == 0;

    public static void SetPixelColorValue(ref Rgb24 pixel, int index, byte value)
    {
        if (index == 0) pixel.R = value;
        else if (index == 1) pixel.G = value;
        else if (index == 2) pixel.B = value;
        else throw new Exception("м?");
    }

    public static byte GetPixelColorValue(Rgb24 pixel, int index)
    {
        if (index == 0) return pixel.R;
        else if (index == 1) return pixel.G;
        else if (index == 2) return pixel.B;
        else throw new Exception("м?");
    }
}