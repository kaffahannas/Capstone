namespace LightenUp.Web.Services;

/// <summary>
/// Stores user files under wwwroot/uploads/accounts/{userId}/{category}/.
/// </summary>
// #Class UserUploadService#
public class UserUploadService
{
    private readonly IWebHostEnvironment _env;

    public UserUploadService(IWebHostEnvironment env) => _env = env;

    public static class Categories
    {
        public const string Profile = "profile";
        public const string Documents = "documents";
        public const string Worksheets = "worksheets";
    }

    public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    public static readonly string[] ProfileExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    public static readonly string[] DocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

    // Max file sizes (in bytes)
    public const long MaxImageSizeBytes = 5 * 1024 * 1024;      // 5 MB
    public const long MaxDocumentSizeBytes = 10 * 1024 * 1024;   // 10 MB

    // #Bagian Penyimpanan File#
    // #Function GetAccountFolderPath#
    public string GetAccountFolderPath(string userId) =>
        Path.Combine(_env.WebRootPath, "uploads", "accounts", SanitizeSegment(userId));

    // #Function SaveAsync#
    public async Task<string?> SaveAsync(
        string userId,
        string category,
        IFormFile file,
        string? namePrefix = null,
        IReadOnlyCollection<string>? allowedExtensions = null,
        long? maxSizeBytes = null)
    {
        if (file.Length == 0) return null;

        // Enforce maximum file size
        var effectiveMaxSize = maxSizeBytes ?? MaxImageSizeBytes;
        if (file.Length > effectiveMaxSize)
            return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (allowedExtensions != null && !allowedExtensions.Contains(ext))
            return null;

        var safeUserId = SanitizeSegment(userId);
        var safeCategory = SanitizeSegment(category);
        var folder = Path.Combine(_env.WebRootPath, "uploads", "accounts", safeUserId, safeCategory);
        Directory.CreateDirectory(folder);

        var prefix = string.IsNullOrWhiteSpace(namePrefix) ? "" : SanitizeSegment(namePrefix) + "_";
        var fileName = $"{prefix}{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/accounts/{safeUserId}/{safeCategory}/{fileName}";
    }

    // #Function ReplaceAsync#
    public async Task<string?> ReplaceAsync(
        string userId,
        string category,
        IFormFile file,
        string? previousWebPath,
        string? namePrefix = null,
        IReadOnlyCollection<string>? allowedExtensions = null,
        long? maxSizeBytes = null)
    {
        var path = await SaveAsync(userId, category, file, namePrefix, allowedExtensions, maxSizeBytes);
        if (path != null)
            TryDeleteByWebPath(previousWebPath);
        return path;
    }

    // #Function TryDeleteByWebPath#
    public void TryDeleteByWebPath(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return;

        var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Where(c => !invalid.Contains(c) && c != '/' && c != '\\').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
    }
}
