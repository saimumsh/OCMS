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

        // Generic file uploader for class materials (documents / videos / etc.).
        // Whitelist limits us to common course-material formats.
        public static readonly string[] AllowedMaterialExtensions =
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
            ".txt", ".zip", ".mp4", ".webm", ".mp3", ".m4a"
        };
        public const long MaxMaterialBytes = 50 * 1024 * 1024; // 50 MB

        // Preserves the original filename (kept after a Guid prefix to avoid
        // collisions). Returns the relative URL under /uploads/{folder}/.
        public static async Task<(bool Success, string Message, string? Url)> TrySaveFileAsync(
            IWebHostEnvironment env,
            IFormFile? file,
            string folder,
            Guid key)
        {
            if (file == null || file.Length == 0) return (true, string.Empty, null);

            if (file.Length > MaxMaterialBytes)
                return (false, $"File must be {MaxMaterialBytes / (1024 * 1024)} MB or smaller", null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedMaterialExtensions.Contains(ext))
                return (false, $"Allowed formats: {string.Join(", ", AllowedMaterialExtensions)}", null);

            var uploadsRoot = Path.Combine(env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            safeName = string.Concat(safeName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').Take(60));
            if (string.IsNullOrEmpty(safeName)) safeName = "file";

            var fileName = $"{(key == Guid.Empty ? Guid.NewGuid() : key):N}-{safeName}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return (true, string.Empty, $"/uploads/{folder}/{fileName}");
        }
    }
}
