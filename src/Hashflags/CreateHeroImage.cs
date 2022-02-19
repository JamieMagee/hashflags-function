using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Hashflags;

public static class CreateHeroImage
{
    private const int ImageWidth = 1200;
    private const int ImageHeight = 675;
    private const int HashflagSize = 72;
    private const string FontFamily = "Segoe UI";

    private static bool IsRtl(string text)
    {
        return new Regex(@"\p{IsArabic}|\p{IsHebrew}").IsMatch(text);
    }

    [FunctionName("CreateHeroImage")]
    [StorageAccount("AzureWebJobsStorage")]
    public static async Task Run(
        [QueueTrigger("create-hero")] KeyValuePair<string, string> hf,
        [Blob("heroimages")] BlobContainerClient heroClient,
        [Blob("hashflags")] BlobContainerClient hashflagsClient,
        [Queue("tweet")] ICollector<KeyValuePair<string, string>> tweetCollector,
        ILogger log)
    {
        log.LogInformation($"Function executed at: {DateTime.Now}");

        var hashtag = '#' + hf.Key;

        var textSize = GetAdjustedFont(hashtag);
        var info = new SKImageInfo(ImageWidth, ImageHeight);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        #region hashtag

        var typefaces = hf.Key.Select(character => SKFontManager.CreateDefault().MatchCharacter(FontFamily, character)).ToList();

        using var tf = typefaces.FirstOrDefault(t => t.FamilyName != FontFamily) ??
                       SKTypeface.FromFamilyName(FontFamily);
        using var shaper = new SKShaper(tf);

        var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(29, 161, 242),
            TextSize = textSize,
            Typeface = tf
        };

        var bounds = new SKRect();
        paint.MeasureText(hashtag, ref bounds);
        var point = GetPoint(hashtag, bounds.Height, bounds.Width);
        canvas.DrawShapedText(shaper, hashtag, point.X, point.Y, paint);

        #endregion

        #region hashflag

        var response = await hashflagsClient.GetBlobClient(hf.Value).DownloadContentAsync();
        var hashflagImage = SKBitmap.Decode(response.Value.Content.ToStream());

        var x = IsRtl(hashtag) ? point.X - HashflagSize - 10 : point.X + bounds.Width + 10;
        const int y = (ImageHeight - HashflagSize) / 2;

        canvas.DrawBitmap(hashflagImage, x, y);

        #endregion

        #region watermark

        paint = new SKPaint
        {
            Color = new SKColor(20, 23, 26, 127),
            TextSize = 18,
            IsAntialias = true,
            Typeface = tf
        };
        canvas.DrawShapedText(shaper, "@HashflagArchive", 900, 575, paint);

        #endregion

        canvas.Flush();
        await heroClient.CreateIfNotExistsAsync();
        var heroBlob = heroClient.GetBlobClient(hf.Key);
        await heroBlob.UploadAsync(ToStream(surface), new BlobHttpHeaders
        {
            ContentType = "image/png"
        });
        tweetCollector.Add(hf);
    }

    private static SKPoint GetPoint(string hashflag, float textHeight, float textWidth)
    {
        var x = IsRtl(hashflag) ?
            (ImageWidth - textWidth + HashflagSize) / 2 :
            (ImageWidth - textWidth - HashflagSize) / 2;
        var y = (ImageHeight + textHeight) / 2;
        return new SKPoint(x, y);
    }

    private static int GetAdjustedFont(string hashtag, int maxWidth = 800, int maxFontSize = 96, int minFontSize = 32)
    {
        for (var adjustedSize = maxFontSize; adjustedSize >= minFontSize; adjustedSize--)
        {
            var skPaint = new SKPaint
            {
                TextSize = adjustedSize
            };
            if (maxWidth > skPaint.MeasureText(hashtag))
            {
                return adjustedSize;
            }
        }

        return minFontSize;
    }

    private static Stream ToStream(SKSurface surface)
    {
        var stream = new MemoryStream();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
