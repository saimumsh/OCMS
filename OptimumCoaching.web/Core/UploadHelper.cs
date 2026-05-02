using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace OptimumCoaching.web.Core
{
    public static class UploadHelper
    {
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public const long MaxImageBytes = 4 * 1024 * 1024;

        // Saves the uploaded image into wwwroot/uploads/{folder}/{key}.{ext}
        // and returns the relative URL. Returns (true, "", null) when no file is provided.
        public static async Task<(bool Success, string Message, string? Url)> TrySaveImageAsync(
            IWebHostEnvironment env,
            IFormFile? file,
            string folder,
            Guid key)
        {
            if (file == null || file.Length == 0) return (true, string.Empty, null);

            if (file.Length > MaxImageBytes)
                return (false, "Image must be 4 MB or smaller", null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                return (false, "Allowed formats: jpg, jpeg, png, gif, webp", null);

            var uploadsRoot = Path.Combine(env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{key:N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, string.Empty, $"/uploads/{folder}/{fileName}");
        }

        public static void TryDeleteImage(IWebHostEnvironment env, string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var trimmed = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(env.WebRootPath, trimmed);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch { /* best effort */ }
        }
    }
}
