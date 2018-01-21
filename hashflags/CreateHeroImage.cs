using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace hashflags
{
    public static class CreateHeroImage
    {
        [FunctionName("CreateHeroImage")]
        [StorageAccount("AzureWebJobsStorage")]
        public static void Run(
            [QueueTrigger("create-hero")] KeyValuePair<string, string> hf,
            [Blob("heroimages")] CloudBlobContainer heroContainer,
            [Blob("hashflags")] CloudBlobContainer hashflagsContainer,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var hashtag = '#' + hf.Key;
            var isRtl = IsRtl(hf.Key);

            var sf = new StringFormat
            {
                FormatFlags = isRtl ? StringFormatFlags.DirectionRightToLeft : 0,
                Trimming = StringTrimming.Word
            };
            var img = new Bitmap(1024, 512);
            var graphics = InitialiseGraphics(img);
            Brush textBrush = new SolidBrush(Color.Black);

            var font = new Font(new FontFamily("Calibri"), 72);
            font = GetAdjustedFont(graphics, hashtag, font, 800, 72, 36);

            var textSize = graphics.MeasureString(hashtag, font);
            var horizontalMargin = (1024 - textSize.Width) / 2;
            var verticalMargin = (512 - textSize.Height) / 2;
            graphics.DrawString(hashtag, font, textBrush, new RectangleF(horizontalMargin, verticalMargin, textSize.Width, textSize.Height), sf);

            DrawHashFlag(ref graphics, hf.Value, isRtl, horizontalMargin, textSize, hashflagsContainer);

            heroContainer.CreateIfNotExists();
            var heroBlob = heroContainer.GetBlockBlobReference(hf.Key);
            heroBlob.Properties.ContentType = "image/png";
            heroBlob.UploadFromStream(ToStream(img, ImageFormat.Png));
        }

        private static Graphics InitialiseGraphics(Image img)
        {
            var drawing = Graphics.FromImage(img);

            //Adjust for high quality
            drawing.CompositingQuality = CompositingQuality.HighQuality;
            drawing.InterpolationMode = InterpolationMode.HighQualityBilinear;
            drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
            drawing.SmoothingMode = SmoothingMode.HighQuality;
            drawing.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            //paint the background
            drawing.Clear(Color.Transparent);

            return drawing;
        }

        private static bool IsRtl(string text)
        {
            return new Regex(@"\p{IsArabic}|\p{IsHebrew}").IsMatch(text);
        }

        private static Stream ToStream(Image image, ImageFormat format)
        {
            var stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        private static Font GetAdjustedFont(Graphics graphics, string hashtag, Font originalFont, int maxWidth, int maxFontSize, int minFontSize)
        {
            // We utilize MeasureString which we get via a control instance           
            for (var adjustedSize = maxFontSize; adjustedSize >= minFontSize; adjustedSize--)
            {
                var testFont = new Font(originalFont.Name, adjustedSize, originalFont.Style);

                // Test the string with the new size
                var adjustedSizeNew = graphics.MeasureString(hashtag, testFont);

                if (maxWidth > Convert.ToInt32(adjustedSizeNew.Width))
                {
                    // Good font, return it
                    return testFont;
                }
            }
            return new Font(originalFont.Name, minFontSize, originalFont.Style);
        }

        private static void DrawHashFlag(ref Graphics graphics, string hashtagPath, bool isRtl, float horizontalMargin, SizeF textSize, CloudBlobContainer hashflagsContainer)
        {
            using (var stream = new MemoryStream())
            {
                hashflagsContainer.GetBlockBlobReference(hashtagPath).DownloadToStream(stream);
                var hashflagImage = Image.FromStream(stream);

                float xCoord;
                var yCoord = (512 - hashflagImage.Height) / 2;

                if (isRtl)
                {
                   xCoord = horizontalMargin - hashflagImage.Width;
                }
                else
                {
                    xCoord = horizontalMargin + textSize.Width;
                }

                graphics.DrawImage(hashflagImage, new RectangleF(xCoord, yCoord, hashflagImage.Width, hashflagImage.Height));
            }
        }
    }
}
