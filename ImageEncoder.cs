using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ToImageEncoder
{
    /* Значит, формат такой. Картинка может быть неизменённой и амплифайд
     * В любом случае, первый пиксель всегда содержит инфу, во сколько раз увеличен пиксель
     * Если значение 1, то просто читать текст от этого пикселя дальше
     * Если больше, то первый символ имеет этот отпечаток метаинфы, и нужно брать инфу вокруг него. */
    public static class ImageEncoder
    {
        static void WriteMetadata(Image<Rgb24> image, int multiplier, byte version, ImageContentType contentType)
        {
            image[0, 0] = new Rgb24((byte)(multiplier - 1), version, (byte)contentType);
        }

        public static ImageMetadata ReadMetadata(Image<Rgb24> image)
        {
            var metaPixel = image[0, 0];

            return new ImageMetadata((byte)(metaPixel.R + 1), metaPixel.G, (ImageContentType)metaPixel.B);
        }

        public static Image<Rgb24> EncodeRaw(byte[] content, ImageContentType contentType, EncodingOptions? options = null)
        {
            options = options ?? new EncodingOptions();

            if (options.version == 0)
            {
                return CreateImage(content, contentType, options);
            }
            else
            {
                throw new Exception("каво");
            }
        }

        public static byte[] DecodeRaw(Image<Rgb24> image)
        {
            var meta = ReadMetadata(image);

            if (meta.version == 0)
            {
                return ReadImage(image, meta);
            }
            else
            {
                throw new Exception("каво");
            }
        }

        /// <summary>
        /// var encoder = new PngEncoder();
        /// await image.SaveAsync(path, encoder);
        /// </summary>
        /// <param name="message"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Image<Rgb24> EncodeTextUTF8(string message, EncodingOptions? options = null)
        {
            options = options ?? new EncodingOptions();

            if (options.version == 0)
            {
                var bytes = Encoding.UTF8.GetBytes(message);

                return CreateImage(bytes, ImageContentType.TextUTF8, options);
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
        public static string DecodeTextUTF8(Image<Rgb24> image)
        {
            var meta = ReadMetadata(image);

            if (meta.contentType != ImageContentType.TextUTF8)
            {
                throw new Exception($"не тот контент тайп ({meta.contentType} != {ImageContentType.TextUTF8})");
            }

            if (meta.version == 0)
            {
                byte[] bytes = ReadImage(image, meta);

                return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            }
            else
            {
                throw new Exception("каво");
            }
        }

        static Image<Rgb24> CreateImage(byte[] content, ImageContentType contentType, EncodingOptions options)
        {
            //это только на текст, без меты
            var needPixels = (int)Math.Ceiling(content.Length / 3f);

            /* мы хотим квадратную картинку.
             * но если информация не помещается в квадрат, лучше добавить 1 строку
             * чем и строку и столб */
            var dimension = (int)Math.Ceiling(Math.Sqrt(needPixels));

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

            var width = dimension;
            var height = dimension;

            var image = new Image<Rgb24>(width, height);

            int byteIndex = 0;

            bool skipFirstPixel = !amplified;
            for (int y = 0; y < height; y++)
            {
                for (int x = skipFirstPixel ? 1 : 0; x < width; x++)
                {
                    var red = byteIndex < content.Length ? content[byteIndex++] : (byte)0;
                    var green = byteIndex < content.Length ? content[byteIndex++] : (byte)0;
                    var blue = byteIndex < content.Length ? content[byteIndex++] : (byte)0;

                    image[x, y] = new Rgb24(red, green, blue);
                }

                skipFirstPixel = false;
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

            WriteMetadata(image, mult, options.version, contentType);

            return image;
        }

        static byte[] ReadImage(Image<Rgb24> image, ImageMetadata meta)
        {
            bool amplified = meta.dimensionMultiplier > 1;

            var informationPixels = image.Width * image.Height;
            if (amplified)
            {
                informationPixels /= (meta.dimensionMultiplier * meta.dimensionMultiplier);
            }
            else
            {
                informationPixels--;
            }

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
            for (int y = 0; y < image.Height; y += meta.dimensionMultiplier)
            {
                for (int x = skipFirstPixel ? meta.dimensionMultiplier : 0; x < image.Width; x += meta.dimensionMultiplier)
                {
                    var pixel = image[x, y];

                    bytes[byteIndex++] = pixel.R;
                    bytes[byteIndex++] = pixel.G;
                    bytes[byteIndex++] = pixel.B;
                }

                skipFirstPixel = false;
            }

            return bytes;
        }
    }
}