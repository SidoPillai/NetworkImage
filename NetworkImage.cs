using System.Diagnostics;
using System.Net.Http;
using System.Web;

namespace NetworkImageLibrary
{
    public class NetworkImage : Image
    {
        #region Url
        public static readonly BindableProperty UrlProperty =
            BindableProperty.Create(nameof(Url), typeof(string), typeof(NetworkImage), default(string), propertyChanged: OnUrlChanged);
        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }
        #endregion

        #region RequestWidth
        public static readonly BindableProperty RequestWidthProperty =
            BindableProperty.Create(nameof(RequestWidth), typeof(int), typeof(NetworkImage), -1);
        public int RequestWidth
        {
            get => (int)GetValue(RequestWidthProperty);
            set => SetValue(RequestWidthProperty, value);
        }
        #endregion

        #region LoadThumbnail
        public static readonly BindableProperty LoadThumbnailProperty =
            BindableProperty.Create(nameof(LoadThumbnail), typeof(bool), typeof(NetworkImage), false);
        public bool LoadThumbnail
        {
            get => (bool)GetValue(LoadThumbnailProperty);
            set => SetValue(LoadThumbnailProperty, value);
        }
        #endregion

        #region PlaceholderImage
        public static readonly BindableProperty PlaceholderImageUrlProperty =
           BindableProperty.Create(nameof(PlaceholderImageUrl), typeof(string), typeof(NetworkImage), default(string));

        public string PlaceholderImageUrl
        {
            get => (string)GetValue(PlaceholderImageUrlProperty);
            set => SetValue(PlaceholderImageUrlProperty, value);
        }
        #endregion

        #region CachingStrategy
        public static readonly BindableProperty CacheStrategyProperty =
            BindableProperty.Create(nameof(CacheStrategy), typeof(CacheStrategy), typeof(NetworkImage), CacheStrategy.None);

        public CacheStrategy CacheStrategy
        {
            get => (CacheStrategy)GetValue(CacheStrategyProperty);
            set => SetValue(CacheStrategyProperty, value);
        }
        #endregion

        #region Token
        public static readonly BindableProperty TokenProperty =
            BindableProperty.Create(nameof(Token), typeof(string), typeof(NetworkImage), default(string));
        public string Token
        {
            get => (string)GetValue(TokenProperty);
            set => SetValue(TokenProperty, value);
        }
        #endregion

        private static void OnUrlChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable == null)
            {
                Trace.WriteLine("BindableObject is null in OnUrlChanged.");
                return;
            }
            if (newValue == null)
            {
                Trace.WriteLine("New value is null in OnUrlChanged.");
                ((NetworkImage)bindable).Source = ((NetworkImage)bindable).GetPlaceholderImage();
                return;
            }
            if (oldValue == newValue)
            {
                Trace.WriteLine("Old value is the same as new value in OnUrlChanged.");
                return;
            }

            var networkImage = (NetworkImage)bindable;
            var url = (string)newValue;

            try
            {
                // Check if the URL is null or empty
                if (string.IsNullOrEmpty(url))
                {
                    Trace.WriteLine("URL is null or empty in OnUrlChanged.");
                    networkImage.Source = networkImage.GetPlaceholderImage();
                    return;
                }

                Trace.WriteLine("URL changed to: " + url);

                if (ImageUtil.IsNetworkUrl(url))
                {
                    Trace.WriteLine("URL is a network URL: " + url);

                    ImageSource? imageSource = networkImage.CacheStrategy switch
                    {
                        CacheStrategy.Memory => networkImage.GetImageFromMemCache(url),
                        CacheStrategy.Disk => networkImage.GetImageFromDiskCache(url).Result,
                        CacheStrategy.None => networkImage.GetImageSourceAsync(url, networkImage.LoadThumbnail).Result,
                        _ => throw new ArgumentOutOfRangeException(nameof(networkImage.CacheStrategy), $"Unexpected CacheStrategy value: {networkImage.CacheStrategy}")
                    };

                    if (imageSource != null)
                    {
                        Trace.WriteLine("Based on cache strategy: " + networkImage.CacheStrategy);
                        Trace.WriteLine("Using image: " + url );
                        networkImage.Source = imageSource;
                        return;
                    }
                    else
                    {
                        Trace.WriteLine("Image not found in cache, loading from URL: " + url);
                        // If not in cache, load the image from the URL
                        networkImage.Source = networkImage.GetImageSourceAsync(url, networkImage.LoadThumbnail).Result;
                        return;
                    }
                }
                else
                {
                    Trace.WriteLine("Not a network url. loading from file");
                    
                    // Load from local file
                    networkImage.Source = ImageUtil.GetImageSourceFromLocalFile(url);
                    return;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"Error in OnUrlChanged: {ex.Message}");
                networkImage.Source = networkImage.GetPlaceholderImage();
            }
        }


        #region Helper Methods

        // Get the Image Source from the memory cache
        private ImageSource? GetImageFromMemCache(string url)
        {
            Trace.WriteLine("Checking memory cache for image: " + url);
            var cachedImage = NetworkImageManager.GetFromCache(url);
            if (cachedImage != null)
            {
                Trace.WriteLine("Using cached image from memory: " + url);
                return cachedImage;
            }

            return null;
        }

        // Get the Image Source from the disk cache
        private async Task<ImageSource?> GetImageFromDiskCache(string url)
        {
            Trace.WriteLine("Checking disk cache for image: " + url);
            // Check if the image is already cached on disk
            var fileName = NetworkImageManager.GetCacheFileName(url);
            var cacheDir = NetworkImageManager.CacheDir;
            var filePath = Path.Combine(cacheDir, fileName);

            if (File.Exists(filePath))
            {
                Trace.WriteLine("Using cached image: " + filePath);

                if (filePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    return await ImageUtil.LoadSvgImageAsync(filePath);
                }
                else
                {
                    return ImageSource.FromFile(filePath);
                }
            }

            return null;
        }        

        // Get the Image Source from the URL
        private async Task<ImageSource?> GetImageSourceAsync(string url, bool loadThumbnail)
        {
            Trace.WriteLine("Loading image from URL: " + url);
            try
            {
                var uriBuilder = new UriBuilder(url);
                var path = uriBuilder.Path;
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                
                if (loadThumbnail)
                {
                    query["w"] = "200";
                }
                else
                {
                    if (RequestWidth != -1)
                    {
                        query["w"] = RequestWidth.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(Token))
                {
                    query["token"] = Token;
                }

                uriBuilder.Query = query.ToString();
                var uri = uriBuilder.Uri;

                Trace.WriteLine("Final URL: " + uri.ToString());
               
                var byteArray = await NetworkImageManager.HttpClient.GetByteArrayAsync(uri.ToString()).ConfigureAwait(false);
                if (byteArray != null)
                {
                    var stream = new MemoryStream(byteArray);
                    if (CacheStrategy == CacheStrategy.Memory)
                    {
                        var fileName = NetworkImageManager.GetCacheFileName(url);
                        if (fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            if (stream == null)
                            {
                                Trace.WriteLine("Stream is null.");
                                return null;
                            }

                            var svgImageSource = await ImageUtil.LoadSvgImageAsync(uri.ToString());

                            //var svgImageSource = ImageUtil.ConvertSvgToImageSource(stream);
                            if (svgImageSource == null)
                            {
                                Trace.WriteLine("Failed to convert SVG to ImageSource.");
                                return null;
                            }

                            NetworkImageManager.AddToCache(url, svgImageSource);
                            return svgImageSource;
                        }
                        
                        var imageSource = ImageSource.FromStream(() => stream);
                        NetworkImageManager.AddToCache(url, imageSource);
                        return imageSource;
                    }

                    if (CacheStrategy == CacheStrategy.Disk)
                    {
                        var fileName = NetworkImageManager.GetCacheFileName(url);
                        var cacheDir = NetworkImageManager.CacheDir;
                        var filePath = Path.Combine(cacheDir, fileName);
                        using (var fileStream = File.Create(filePath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                        // Save to disk cache
                        Trace.WriteLine("Image downloaded and cached: " + filePath);
                        if (filePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            return await ImageUtil.LoadSvgImageAsync(filePath);
                        }

                        return ImageSource.FromFile(filePath);
                    }

                    if (CacheStrategy == CacheStrategy.None)
                    {
                        if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            var svgImageSource = await ImageUtil.ConvertSvgToImageSource(stream);
                            return svgImageSource;
                        }
                        else
                        {
                            return ImageSource.FromStream(() => stream);
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"Request URL: {uri}");
                    }
                }
                else
                {
                    Trace.WriteLine($"Failed to load image from URL: {url}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while loading image: {ex.Message}");
            }

            // If all else fails, return a placeholder image
            if (!string.IsNullOrEmpty(PlaceholderImageUrl))
            {
                return ImageSource.FromFile(PlaceholderImageUrl);
            }

            return null;
        }

        private ImageSource? GetPlaceholderImage()
        {
            Trace.WriteLine("Returning placeholder image.");
            // Logic to get the placeholder image
            // This could involve checking if the placeholder image URL is set
            // and returning the corresponding image source
            if (!string.IsNullOrEmpty(PlaceholderImageUrl))
            {
                return ImageSource.FromFile(PlaceholderImageUrl);
            }
            return null;
        }
        #endregion
    }
}
