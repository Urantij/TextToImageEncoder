using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ToImageEncoder;

/// <summary>
/// В данном случае мы не создаём новые картинки, а модифицируем имеющиеся.
/// И без мета инфы по понятным причинам.
/// </summary>
public static class ImageSteganography
{
    /// <summary>
    /// Один пиксель = 1 бит
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static int CountBytes(Image<Rgb24> image)
    {
        return image.Width * image.Height * 3 / 8;
    }

    /// <summary>
    /// Чётное - true, нечётное - false
    /// Если места не хватит, то ничего не поделаешь
    /// </summary>
    /// <param name="image">Изображение будет изменяться</param>
    /// <param name="message">Длина больше нуля</param>
    public static void EncryptMessageWithEvenColors(Image<Rgb24> image, string message)
    {
        if (message.Length == 0)
        {
            throw new ArgumentException($"{nameof(message)}.Length == 0");
        }

        int bitsIndex = 0;
        var bits = new BitArray(Encoding.UTF8.GetBytes(message));

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgb24 pixel = image[x, y];

                for (int colorIndex = 0; colorIndex < 3; colorIndex++)
                {
                    byte value = Helper.GetPixelColorValue(pixel, colorIndex);

                    bool even = Helper.IsEven(value);

                    if (even != bits[bitsIndex])
                    {
                        if (value > byte.MaxValue / 2)
                            value--;
                        else
                            value++;

                        Helper.SetPixelColorValue(ref pixel, colorIndex, value);
                        image[x, y] = pixel;
                    }

                    bitsIndex++;

                    if (bitsIndex >= bits.Length)
                    {
                        goto endCycle;
                    }
                }
            }
        }

        endCycle: ;
    }

    /// <summary>
    /// Поскольку сообщение передаётся как есть, то всё изображение может превратится в строку.
    /// А это тупа. Поэтому есть лимит (в байтах).
    /// </summary>
    /// <param name="image"></param>
    /// <param name="bytesLimit"></param>
    /// <returns>Сообщение</returns>
    public static string DecryptMessageWithEvenColors(Image<Rgb24> image, int bytesLimit = int.MaxValue / 8)
    {
        int bitsCount = Math.Min(bytesLimit * 8, image.Width * image.Height * 8 * 3);

        var bits = new BitArray(bitsCount);
        int bitsIndex = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgb24 pixel = image[x, y];

                for (int colorIndex = 0; colorIndex < 3; colorIndex++)
                {
                    byte value = Helper.GetPixelColorValue(pixel, colorIndex);

                    bool even = Helper.IsEven(value);

                    bits[bitsIndex] = even;

                    bitsIndex++;

                    if (bitsIndex >= bitsCount)
                    {
                        goto endCycle;
                    }
                }
            }
        }

        endCycle: ;

        byte[] bytes = new byte[bitsCount / 8];
        bits.CopyTo(bytes, 0);

        return Encoding.UTF8.GetString(bytes);
    }
}