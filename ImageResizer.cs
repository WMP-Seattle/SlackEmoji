using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace slackemoji
{
    public class ImageResizer
    {
        public ImageResizer()
        {
        }

        public byte[] ResizeImage(byte[] imageBytes)
        {
            byte[] finishedImage;
            // max size of a slack emoji
            const int size = 128;
            const int quality = 75;

            using (var inputMs = new MemoryStream(imageBytes))
            using (var image = new Bitmap(Image.FromStream((inputMs))))
            {
                int width, height;
                if (image.Width > image.Height)
                {
                    width = size;
                    height = Convert.ToInt32(image.Height * size / (double)image.Width);
                }
                else
                {
                    width = Convert.ToInt32(image.Width * size / (double)image.Height);
                    height = size;
                }
                using (var resized = new Bitmap(width, height))
                {
                    using (var graphics = Graphics.FromImage(resized))
                    {
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.DrawImage(image, 0, 0, width, height);

                        var qualityParamId = Encoder.Quality;
                        var encoderParameters = new EncoderParameters(1);
                        encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);
                        var codec = ImageCodecInfo.GetImageDecoders()
                            .FirstOrDefault(c => c.FormatID == ImageFormat.Png.Guid);

                        using (var outputMs = new MemoryStream())
                        {
                            resized.Save(outputMs, codec, encoderParameters);
                            finishedImage = outputMs.ToArray();
                            return finishedImage;
                        }
                    }
                }
               
            }
        }
    }
}
