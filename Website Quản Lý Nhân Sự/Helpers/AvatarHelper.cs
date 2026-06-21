namespace Website_Quản_Lý_Nhân_Sự.Helpers;

public static class AvatarHelper
{
    public const string DefaultAvatarPath = "/images/default-avatar.svg";

    public static string GetUrl(string? avatarPath) =>
        string.IsNullOrWhiteSpace(avatarPath) ? DefaultAvatarPath : avatarPath;
}
