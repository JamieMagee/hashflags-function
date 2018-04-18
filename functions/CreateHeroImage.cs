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
            [Queue("tweet")] ICollector<KeyValuePair<string, string>> tweetCollector,
            TraceWriter log)
        {
            log.Info($"Function executed at: {DateTime.Now}");

            var hashtag = '#' + hf.Key;
            var isRtl = IsRtl(hf.Key);

            var sf = new StringFormat
            {
                FormatFlags = isRtl ? StringFormatFlags.DirectionRightToLeft : 0
            };
            const int imageWidth = 1200;
            const int imageHeight = 675;
            const int hashflagSize = 72;

            var img = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
            var graphics = InitialiseGraphics(img);
            var textBrush = new SolidBrush(Color.FromArgb(29, 161, 242));
            var watermarkBrush = new SolidBrush(Color.FromArgb(127, 20, 23, 26));

            var font = new Font(new FontFamily("Segoe UI"), 72);
            var watermarkFont = new Font(new FontFamily("Segoe UI"), 18);
            font = GetAdjustedFont(graphics, hashtag, font, 800, 72, 36);

            var textSize = graphics.MeasureString(hashtag, font);
            var horizontalMargin = isRtl ?
                (imageWidth - textSize.Width + hashflagSize) / 2 :
                (imageWidth - textSize.Width - hashflagSize) / 2;
            var verticalMargin = (imageHeight - textSize.Height) / 2;

            graphics.DrawString(hashtag, font, textBrush,
                new RectangleF(horizontalMargin, verticalMargin, textSize.Width, textSize.Height), sf);
            // Watermark
            graphics.DrawString("@HashflagArchive", watermarkFont, watermarkBrush, 900, 575);


            DrawHashFlag(ref graphics, hf.Value, isRtl, horizontalMargin, textSize, hashflagsContainer);

            heroContainer.CreateIfNotExistsAsync();
            var heroBlob = heroContainer.GetBlockBlobReference(hf.Key);
            heroBlob.Properties.ContentType = "image/png";
            heroBlob.UploadFromStreamAsync(ToStream(img));
            tweetCollector.Add(hf);
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
            drawing.Clear(Color.White);
            //Draw transparent pixel at the top of the image, to prevent JPEG compression on upload
            //drawing.FillRectangle(new SolidBrush(Color.FromArgb(1, 0, 0, 0)), 0, 0, 1200, 100);
            return drawing;
        }

        private static bool IsRtl(string text)
        {
            return new Regex(@"\p{IsArabic}|\p{IsHebrew}").IsMatch(text);
        }

        private static Stream ToStream(Image image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }

        private static Font GetAdjustedFont(Graphics graphics, string hashtag, Font originalFont, int maxWidth,
            int maxFontSize, int minFontSize)
        {
            // We utilize MeasureString which we get via a control instance
            for (var adjustedSize = maxFontSize; adjustedSize >= minFontSize; adjustedSize--)
            {
                var testFont = new Font(originalFont.Name, adjustedSize, originalFont.Style);

                // Test the string with the new size
                var adjustedSizeNew = graphics.MeasureString(hashtag, testFont);

                if (maxWidth > Convert.ToInt32(adjustedSizeNew.Width)) return testFont;
            }

            return new Font(originalFont.Name, minFontSize, originalFont.Style);
        }

        private static void DrawHashFlag(ref Graphics graphics, string hashtagPath, bool isRtl, float horizontalMargin,
            SizeF textSize, CloudBlobContainer hashflagsContainer)
        {
            using (var stream = new MemoryStream())
            {
                hashflagsContainer.GetBlockBlobReference(hashtagPath).DownloadRangeToStreamAsync(stream, null, null);
                var hashflagImage = Image.FromStream(stream);

                float xCoord;
                var yCoord = (675 - hashflagImage.Height) / 2;

                if (isRtl)
                    xCoord = horizontalMargin - hashflagImage.Width;
                else
                    xCoord = horizontalMargin + textSize.Width;

                graphics.DrawImage(hashflagImage,
                    new RectangleF(xCoord, yCoord, hashflagImage.Width, hashflagImage.Height));
            }
        }
    }
}