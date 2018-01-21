using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
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
            var font = new Font(new FontFamily("Calibri"), 100, FontStyle.Regular, GraphicsUnit.Pixel);

            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);
            //measure the string to see how big the image needs to be
            SizeF textSize = drawing.MeasureString(hashtag, font, 800);

            //set the stringformat flags to rtl
            StringFormat sf = new StringFormat
            {
                //uncomment the next line for right to left languages
                //sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                Trimming = StringTrimming.Word
            };
            //free up the dummy image and old graphics object
            img.Dispose();
            drawing.Dispose();

            //create a new image of the right size
            img = new Bitmap(1024, 512);

            drawing = Graphics.FromImage(img);
            //Adjust for high quality
            drawing.CompositingQuality = CompositingQuality.HighQuality;
            drawing.InterpolationMode = InterpolationMode.HighQualityBilinear;
            drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
            drawing.SmoothingMode = SmoothingMode.HighQuality;
            drawing.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            //paint the background
            drawing.Clear(Color.Transparent);

            //create a brush for the text
            Brush textBrush = new SolidBrush(Color.Black);

            var horizontalMargin = (1024 - textSize.Width) / 2;
            var verticalMargin = (512 - textSize.Height) / 2;
            drawing.DrawString(hashtag, font, textBrush, new RectangleF(horizontalMargin, verticalMargin, textSize.Width, textSize.Height), sf);

            using (var stream = new MemoryStream())
            {
                hashflagsContainer.GetBlockBlobReference(hf.Value).DownloadToStream(stream);
                var hashflagImage = Image.FromStream(stream);
                drawing.DrawImage(hashflagImage, new RectangleF(horizontalMargin + textSize.Width, 220, hashflagImage.Width, hashflagImage.Height));
            }

            drawing.Save();

            textBrush.Dispose();
            drawing.Dispose();

            heroContainer.CreateIfNotExists();
            var heroBlob = heroContainer.GetBlockBlobReference(hf.Key);
            heroBlob.Properties.ContentType = "image/png";
            heroBlob.UploadFromStream(ToStream(img, ImageFormat.Png));
            img.Dispose();
        }

        private static Stream ToStream(Image image, ImageFormat format)
        {
            var stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;
            return stream;
        }
    }
}
