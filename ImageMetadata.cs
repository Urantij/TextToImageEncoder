using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToImageEncoder
{
    public class ImageMetadata
    {
        public readonly byte dimensionMultiplier;
        public readonly byte version;
        public readonly ImageContentType contentType;

        public ImageMetadata(byte dimensionMultiplier, byte version, ImageContentType contentType)
        {
            this.dimensionMultiplier = dimensionMultiplier;
            this.version = version;
            this.contentType = contentType;
        }
    }
}