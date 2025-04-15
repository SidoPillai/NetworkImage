using System.Collections.Concurrent;
using System.Diagnostics;
using System.Web;

namespace NetworkImageLibrary
{
    public enum CacheStrategy
    {
        None,
        Memory,
        Disk
    }

    public class NetworkImage : Image
    {
        private static readonly string cacheDir = Path.Combine(FileSystem.CacheDirectory, "ImageCache");

        private static readonly ConcurrentDictionary<string, ImageSource> ImageCache = new();

        // Add a placeholder image url property
        public static readonly BindableProperty PlaceholderImageUrlProperty =
            BindableProperty.Create(nameof(PlaceholderImageUrl), typeof(string), typeof(NetworkImage), default(string));

        public static readonly BindableProperty CacheStrategyProperty =
            BindableProperty.Create(nameof(CacheStrategy), typeof(CacheStrategy), typeof(NetworkImage), CacheStrategy.None);

        public static readonly BindableProperty UrlProperty =
            BindableProperty.Create(nameof(Url), typeof(string), typeof(NetworkImage), default(string), propertyChanged: OnUrlChanged);

        public static readonly BindableProperty LoadThumbnailProperty =
            BindableProperty.Create(nameof(LoadThumbnail), typeof(bool), typeof(NetworkImage), false);

        public static readonly BindableProperty ContentScaleProperty =
            BindableProperty.Create(nameof(ContentScale), typeof(Aspect), typeof(NetworkImage), Aspect.AspectFit);

        public static readonly BindableProperty TokenProperty =
            BindableProperty.Create(nameof(Token), typeof(string), typeof(NetworkImage), default(string));


        public static readonly BindableProperty RequestWidthProperty =
            BindableProperty.Create(nameof(RequestWidth), typeof(int), typeof(NetworkImage), 1600);

        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public string PlaceholderImageUrl
        {
            get => (string)GetValue(PlaceholderImageUrlProperty);
            set => SetValue(PlaceholderImageUrlProperty, value);
        }

        public CacheStrategy CacheStrategy
        {
            get => (CacheStrategy)GetValue(CacheStrategyProperty);
            set => SetValue(CacheStrategyProperty, value);
        }

        public bool LoadThumbnail
        {
            get => (bool)GetValue(LoadThumbnailProperty);
            set => SetValue(LoadThumbnailProperty, value);
        }

        public Aspect ContentScale
        {
            get => (Aspect)GetValue(ContentScaleProperty);
            set => SetValue(ContentScaleProperty, value);
        }

        public string Token
        {
            get => (string)GetValue(TokenProperty);
            set => SetValue(TokenProperty, value);
        }

        public int RequestWidth
        {
            get => (int)GetValue(RequestWidthProperty);
            set => SetValue(RequestWidthProperty, value);
        }

        private static async void OnUrlChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var networkImage = (NetworkImage)bindable;
            var url = (string)newValue;


            // Check if URL is a valid network URL
            if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                // Check if the URL is already cached in memory
                if (networkImage.CacheStrategy == CacheStrategy.Memory && ImageCache.TryGetValue(url, out var cachedImage))
                {
                    Trace.WriteLine("Using cached thumbnail from memory: " + url);
                    networkImage.Source = cachedImage;
                    return;
                }

                // Check if cache directory exists, and if file exists then return the cached image
                if (networkImage.CacheStrategy == CacheStrategy.Disk && Directory.Exists(cacheDir))
                {
                    var fileName = networkImage.GetCacheFileName(url);
                    var filePath = Path.Combine(cacheDir, fileName);
                    if (File.Exists(filePath))
                    {
                        Trace.WriteLine("Using cached thumbnail: " + filePath);
                        networkImage.Source = ImageSource.FromFile(filePath);
                        return;
                    }
                }

                // Load thumbnail if specified
                if (networkImage.LoadThumbnail)
                {
                    // Load low-res thumbnail first
                    var thumbnailSource = await networkImage.GetImageSourceAsync(url, true);
                    networkImage.Source = thumbnailSource;

                    // Add a delay to simulate loading
                    await Task.Delay(400);
                }

                // Load high-res image
                var imageSource = await networkImage.GetImageSourceAsync(url, false);
                networkImage.Source = imageSource;
            }
            else
            {
                // Check if URL is a local file
                if (File.Exists(url))
                {
                    networkImage.Source = ImageSource.FromFile(url);
                    return;
                }

                // Check if URL is a resource
                if (url.StartsWith("resource://"))
                {
                    var resourcePath = url.Replace("resource://", "");
                    networkImage.Source = ImageSource.FromResource(resourcePath);
                    return;
                }

                // Default to loading as a local file if no other conditions are met
                networkImage.Source = ImageSource.FromFile(url);
            }
        }

        private async Task<ImageSource> GetImageSourceAsync(string url, bool loadThumbnail)
        {           
            // Check if the URL is already cached in memory
            if (CacheStrategy == CacheStrategy.Memory && ImageCache.TryGetValue(url, out ImageSource? cachedImage))
            {
                Trace.WriteLine("Using cached image from memory: " + url);
                return cachedImage;
            }

            // Check if cache directory exists, if not create it
            if (CacheStrategy == CacheStrategy.Disk && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            // Check if the image is already cached on disk
            var fileName = GetCacheFileName(url);
            var filePath = Path.Combine(cacheDir, fileName);
            if (File.Exists(filePath))
            {
                Trace.WriteLine("Using cached image: " + filePath);
                return ImageSource.FromFile(filePath);
            }

            // If not cached, download the image
            try
            {
                using (var client = new HttpClient())
                {
                    var uriBuilder = new UriBuilder(url);
                    var query = HttpUtility.ParseQueryString(uriBuilder.Query);                    

                    if (loadThumbnail)
                    {
                        query["w"] = "200";
                    }
                    else
                    {
                        query["w"] = RequestWidth.ToString();
                    }

                    if (!string.IsNullOrEmpty(Token))
                    {
                        query["token"] = Token;
                    }
                    uriBuilder.Query = query.ToString();
                    var uri = uriBuilder.Uri;

                    var response = await client.GetAsync(uri);

                    if (response.IsSuccessStatusCode)
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        // Check if the image is already cached in memory
                        if (CacheStrategy == CacheStrategy.Memory)
                        {
                            var imageSource = ImageSource.FromStream(() => stream);
                            ImageCache[url] = imageSource;
                            return imageSource;
                        }

                        // If not cached, save the image to disk
                        using (var fileStream = File.Create(filePath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        Trace.WriteLine("Image downloaded and cached: " + filePath);
                        return ImageSource.FromFile(filePath);
                    }
                    else
                    {
                        Trace.WriteLine($"Failed to load image. Status code: {response.StatusCode}");
                        Trace.WriteLine($"Request URL: {uri}");
                    }
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

        private string GetCacheFileName(string url)
        {
            Trace.WriteLine("Generating cache file name for URL: " + url);

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }

            var uri = new Uri(url);
            var fileName = uri.AbsolutePath.Replace("/", "_") + ".cache";
            return fileName;
        }
    }
}
