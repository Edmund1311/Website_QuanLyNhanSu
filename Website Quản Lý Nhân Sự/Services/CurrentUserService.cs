using System.Security.Claims;
using Website_Quản_Lý_Nhân_Sự.Constants;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    int? CompanyId { get; }
    bool IsSuperAdmin { get; }
    bool IsCompanyAdmin { get; }
    bool IsHR { get; }
    bool IsEmployee { get; }
    bool CanManageCompanyData { get; }
    bool CanManageHRData { get; }
    bool CanApproveJoinRequests { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public int? CompanyId
    {
        get
        {
            var value = User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => User?.IsInRole(RoleNames.SuperAdmin) == true;
    public bool IsCompanyAdmin => User?.IsInRole(RoleNames.CompanyAdmin) == true;
    public bool IsHR => User?.IsInRole(RoleNames.HR) == true;
    public bool IsEmployee => User?.IsInRole(RoleNames.Employee) == true;

    public bool CanManageCompanyData => IsCompanyAdmin || IsHR;
    public bool CanManageHRData => IsCompanyAdmin || IsHR;
    public bool CanApproveJoinRequests => IsCompanyAdmin;
}
