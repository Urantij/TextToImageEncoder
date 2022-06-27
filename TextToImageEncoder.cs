using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TextToImageEncoder
{
    /* Значит, формат такой. Картинка может быть неизменённой и амплифайд
     * В любом случае, первый пиксель всегда содержит инфу, во сколько раз увеличен пиксель
     * Если значение 1, то просто читать текст от этого пикселя дальше
     * Если больше, то первый символ имеет этот отпечаток метаинфы, и нужно брать инфу вокруг него. */
    public static class TextToImageEncoder
    {
        /// <summary>
        /// var encoder = new PngEncoder();
        /// await image.SaveAsync(path, encoder);
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Image<Rgb24> EncodeText(string message)
        {
            return EncodeText(message, Encoding.UTF8, new EncodingOptions());
        }

        public static Image<Rgb24> EncodeText(string message, Encoding encoding, EncodingOptions options)
        {
            byte textEncodingByte;
            if (encoding == Encoding.UTF8)
            {
                textEncodingByte = 1;
            }
            else
            {
                throw new Exception("кокой коднг");
            }

            if (options.version == 0)
            {
                var bytes = encoding.GetBytes(message);

                //это только на текст, без меты
                var needPixels = (int)Math.Ceiling(bytes.Length / 3f);

                /* мы хотим квадратную картинку.
                 * но если информация не помещается в квадрат, лучше добавить 1 строку
                 * чем и строку и столб */
                var dimension = IntegerSqrt(needPixels);

                var mult = options.targetMinDimension / dimension;
                if (mult > byte.MaxValue + 1)
                {
                    //мультиплер не может быть 0. так что на одно значение сдвигаем
                    mult = byte.MaxValue + 1;
                }

                bool amplified = mult > 1;

                if (!amplified)
                {
                    needPixels++;
                }

                var requiredExtraPixels = needPixels - (dimension * dimension);

                var width = dimension;
                var height = dimension + (requiredExtraPixels > 0 ? 1 : 0);
                //var height = requiredExtraPixels > 0 ? dimension + (int)Math.Ceiling((float)requiredExtraPixels / dimension) : dimension;

                var image = new Image<Rgb24>(width, height);

                int byteIndex = 0;

                bool skipFirstPixel = !amplified;
                for (int y = 0; y < height; y++)
                {
                    for (int x = skipFirstPixel ? 1 : 0; x < width; x++)
                    {
                        skipFirstPixel = false;

                        var red = byteIndex < bytes.Length ? bytes[byteIndex++] : (byte)0;
                        var green = byteIndex < bytes.Length ? bytes[byteIndex++] : (byte)0;
                        var blue = byteIndex < bytes.Length ? bytes[byteIndex++] : (byte)0;

                        image[x, y] = new Rgb24(red, green, blue);
                    }
                }

                if (amplified)
                {
                    var size = new Size(width * mult, height * mult);
                    image.Mutate(act => act.Resize(new ResizeOptions()
                    {
                        Size = size,

                        Sampler = KnownResamplers.NearestNeighbor
                    }));
                }

                var multByte = (byte)(mult - 1);

                image[0, 0] = new Rgb24(multByte, options.version, textEncodingByte);

                return image;
            }
            else
            {
                throw new Exception("каво");
            }
        }

        /// <summary>
        /// Image.Load<Rgb24>(path)
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string DecodeText(Image<Rgb24> image)
        {
            var metaPixel = image[0, 0];
            var version = metaPixel.G;

            if (version == 0)
            {
                var mult = metaPixel.R + 1;
                var textEncodingByte = metaPixel.B;

                Encoding encoding;
                if (textEncodingByte == 1)
                {
                    encoding = Encoding.UTF8;
                }
                else
                {
                    throw new Exception("кокой коднг");
                }

                bool amplified = mult > 1;

                var informationPixels = image.Width * image.Height - (amplified ? 0 : 1);

                byte[] bytes = new byte[informationPixels * 3];
                int byteIndex = 0;

                //невероятные оптимизации
                if (amplified)
                {
                    var firstPixel = image[1, 0];

                    bytes[byteIndex++] = firstPixel.R;
                    bytes[byteIndex++] = firstPixel.G;
                    bytes[byteIndex++] = firstPixel.B;
                }

                bool skipFirstPixel = true;
                for (int y = 0; y < image.Height; y += mult)
                {
                    for (int x = skipFirstPixel ? mult : 0; x < image.Width; x += mult)
                    {
                        skipFirstPixel = false;

                        var pixel = image[x, y];

                        bytes[byteIndex++] = pixel.R;
                        bytes[byteIndex++] = pixel.G;
                        bytes[byteIndex++] = pixel.B;
                    }
                }

                return encoding.GetString(bytes);
            }
            else
            {
                throw new Exception("каво");
            }
        }

        static int IntegerSqrt(int n)
        {
            if (n < 2)
                return 2;

            int shift = 2;
            int nShifted = n >> shift;

            while (nShifted != 0 && nShifted != n)
            {
                shift += 2;
                nShifted = n >> shift;
            }

            shift -= 2;

            var result = 0;
            while (shift >= 0)
            {
                result = result << 1;
                var candidateResult = result + 1;

                if (candidateResult * candidateResult <= n >> shift)
                    result = candidateResult;

                shift -= 2;
            }

            return result;
        }
    }
}