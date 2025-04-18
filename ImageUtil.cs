using SkiaSharp;
using System.Diagnostics;

using SKSvg = Svg.Skia.SKSvg;

namespace NetworkImageLibrary
{
    public static class ImageUtil
    {
        public static Boolean IsNetworkUrl(string url)
        {
            Trace.WriteLine("Checking if URL is a network URL: " + url);
            return (Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps));
        }

        // Reusable method to load and render an SVG from a stream
        private static SKBitmap? RenderSvgToBitmap(Stream svgStream, int imageWidth, int imageHeight)
        {
            Trace.WriteLine("Rendering SVG to SKBitmap.");
            try
            {
                SKSvg svg = new SKSvg();
                svg.Load(svgStream);

                if (svg.Picture == null)
                {
                    Trace.WriteLine("Failed to load SVG picture.");
                    return null;
                }

                SKBitmap bitmap = new SKBitmap(imageWidth, imageHeight);
                SKImageInfo imageInfo = new SKImageInfo(bitmap.Width, bitmap.Height);

                using (var surface = SKSurface.Create(imageInfo))
                {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColor.Empty);
                    canvas.Translate(imageInfo.Width / 2f, imageInfo.Height / 2f);

                    SKRect bounds = svg.Picture.CullRect;
                    float xRatio = imageInfo.Width / bounds.Width;
                    float yRatio = imageInfo.Height / bounds.Height;
                    float ratio = Math.Min(xRatio, yRatio);

                    canvas.Scale(ratio);
                    canvas.Translate(-bounds.MidX, -bounds.MidY);
                    canvas.DrawPicture(svg.Picture);
                    surface.Canvas.Flush();

                    return SKBitmap.FromImage(surface.Snapshot());
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while rendering SVG: {ex.Message}");
            }

            return null;
        }

        // Convert SVG Stream to ImageSource
        public static Task<ImageSource?> ConvertSvgToImageSource(MemoryStream svgStream, int imageWidth = 200, int imageHeight = 200)
        {
            Trace.WriteLine("Converting SVG stream to ImageSource.");
            return Task.Run(() =>
            {
                try
                {
                    svgStream.Position = 0; // Reset the stream position to the beginning
                    var bitmap = RenderSvgToBitmap(svgStream, imageWidth, imageHeight);
                    if (bitmap != null)
                    {
                        var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        var imageStream = new MemoryStream(data.ToArray());
                        return ImageSource.FromStream(() =>
                        {
                            imageStream.Position = 0; // Reset the stream position before returning
                            return new MemoryStream(imageStream.ToArray()); // Return a new MemoryStream
                        });
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Exception occurred while converting SVG to ImageSource: {ex.Message}");
                }

                return null;
            });
        }

        // Load SVG Image from URL
        public static async Task<ImageSource?> LoadSvgImageAsync(string url)
        {
            Trace.WriteLine("Loading SVG image from URL: " + url);
            try
            {
                using (var httpClient = NetworkImageManager.HttpClient)
                {
                    var response = await httpClient.GetAsync(url).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var svgData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        var stream = new MemoryStream(svgData);
                        return await ConvertSvgToImageSource(stream).ConfigureAwait(false);
                    }
                    else
                    {
                        Trace.WriteLine($"Failed to load SVG image. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while loading SVG image: {ex.Message}");
            }

            return null;
        }

        // Get ImageSource from Local File
        public static ImageSource? GetImageSourceFromLocalFile(string filePath)
        {
            Trace.WriteLine("Getting ImageSource from local file: " + filePath);

            if (filePath.StartsWith("resource://", StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine("File is a resource: " + filePath);
                var resourceName = filePath.Substring("resource://".Length);
                return ImageSource.FromResource(resourceName);
            }

            if (File.Exists(filePath))
            {
                if (filePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            return ConvertSvgToImageSource(memoryStream).Result;
                        }
                    }
                }

                return ImageSource.FromFile(filePath);
            }
            else
            {
                Trace.WriteLine($"File not found: {filePath}");
                return null;
                //return ImageSource.FromFile(filePath); // this works for resources
            }
        }
    }
}