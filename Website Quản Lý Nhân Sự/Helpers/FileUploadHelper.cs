using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Helpers;

public static class FileUploadHelper
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".mov", ".avi", ".webm"];
    private static readonly HashSet<string> DocumentExtensions = [".pdf", ".doc", ".docx"];

    public static async Task<string> SaveFileAsync(IFormFile file, string webRootPath, string folder)
    {
        var uploadsRoot = Path.Combine(webRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }

    public static MediaType ResolveMediaType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (ImageExtensions.Contains(extension))
        {
            return MediaType.Image;
        }

        if (VideoExtensions.Contains(extension))
        {
            return MediaType.Video;
        }

        return MediaType.Document;
    }
}
