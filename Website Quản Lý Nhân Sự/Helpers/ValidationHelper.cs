using System.Text.RegularExpressions;

namespace Website_Quản_Lý_Nhân_Sự.Helpers;

/// <summary>
/// Helper class cho validation logic chung (Lỗi 7.1, 7.2, 7.3)
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Kiểm tra email hợp lệ
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra phone hợp lệ (Việt Nam)
    /// Định dạng: 0xxxxxxxxx (10 số) hoặc +84xxxxxxxxx
    /// </summary>
    public static bool IsValidPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true; // Phone là optional

        return Regex.IsMatch(phone, @"^(0|\+84)\d{9}$");
    }

    /// <summary>
    /// Kiểm tra mã nhân viên hợp lệ (Lỗi 7.2)
    /// Định dạng: NV001, EMP0001, etc.
    /// </summary>
    public static bool IsValidEmployeeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return Regex.IsMatch(code, @"^[A-Z]{0,2}\d{3,4}$");
    }

    /// <summary>
    /// Sanitize file path để tránh Path Traversal (Lỗi 5.1)
    /// </summary>
    public static string? SanitizeFilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Chỉ cho phép path dưới /uploads/media/ hoặc /uploads/avatars/
        if (!path.StartsWith("/uploads/media/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Từ chối
        }

        // Loại bỏ "../" và ".."
        if (path.Contains(".."))
        {
            return null;
        }

        return path;
    }

    /// <summary>
    /// Kiểm tra kích thước file hợp lệ (Lỗi 5.3)
    /// Max 50MB
    /// </summary>
    public static bool IsValidFileSize(long fileSizeBytes)
    {
        const long maxSizeBytes = 50 * 1024 * 1024; // 50 MB
        return fileSizeBytes > 0 && fileSizeBytes <= maxSizeBytes;
    }

    /// <summary>
    /// Kiểm tra extension file hợp lệ
    /// </summary>
    public static bool IsValidFileExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Danh sách extension cho phép
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".gif" };

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }
}
