using System.Collections.Concurrent;
using System.Diagnostics;

namespace NetworkImageLibrary
{
    public static class NetworkImageManager
    {
        public static readonly string CacheDir = Path.Combine(FileSystem.CacheDirectory, "ImageCache");
        private static readonly ConcurrentDictionary<string, ImageSource> ImageCache = new();
        public static HttpClient HttpClient = new HttpClient();

        // Clears the cache for network images based on the caching strategy
        public static void ClearCacheDir()
        {
            // Logic to clear the cache
            // This could involve deleting files from the disk cache directory
            // and clearing the memory cache
            if (Directory.Exists(CacheDir))
            {
                try
                {
                    Directory.Delete(CacheDir, true);
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., log the error)
                    Console.WriteLine($"Error clearing cache: {ex.Message}");
                }
            }
        }

        // Clears the memory cache for network images
        public static void ClearCache()
        {
            ImageCache.Clear();
        }

        public static void AddToCache(string key, ImageSource image)
        {
            if (!ImageCache.ContainsKey(key))
            {
                ImageCache.TryAdd(key, image);
            }
        }

        public static ImageSource? GetFromCache(string key)
        {
            if (ImageCache.TryGetValue(key, out var image))
            {
                return image;
            }
            return null;
        }

        public static void RemoveFromCache(string key)
        {
            if (ImageCache.ContainsKey(key))
            {
                ImageCache.TryRemove(key, out _);
            }
        }

        public static string GetCacheFileName(string url)
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
