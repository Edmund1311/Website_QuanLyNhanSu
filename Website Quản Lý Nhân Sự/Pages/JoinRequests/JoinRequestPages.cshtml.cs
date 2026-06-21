using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.JoinRequests;

[Authorize(Roles = RoleNames.CompanyAdmin)]
public class IndexModel : SecurePageModel
{
    private readonly IJoinRequestService _joinRequestService;
    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ICurrentUserService currentUser, IJoinRequestService joinRequestService, IDepartmentService departmentService, IPositionService positionService, UserManager<ApplicationUser> userManager) : base(currentUser)
    {
        _joinRequestService = joinRequestService; _departmentService = departmentService; _positionService = positionService; _userManager = userManager;
    }

    public List<JoinCompanyRequest> Requests { get; set; } = [];
    public List<Department> Departments { get; set; } = [];
    public List<Position> Positions { get; set; } = [];
    [BindProperty] public int RequestId { get; set; }
    [BindProperty] public string EmployeeCode { get; set; } = string.Empty;
    [BindProperty] public int DepartmentId { get; set; }
    [BindProperty] public int PositionId { get; set; }
    [BindProperty] public WorkStatus WorkStatus { get; set; } = WorkStatus.Active;

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Requests = await _joinRequestService.GetForCompanyAsync(CurrentUser.CompanyId.Value, JoinRequestStatus.Pending);
        Departments = await _departmentService.GetAllAsync(CurrentUser.CompanyId.Value);
        Positions = await _positionService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var result = await _joinRequestService.ApproveAsync(RequestId, CurrentUser.CompanyId.Value, EmployeeCode, DepartmentId, PositionId, WorkStatus, _userManager);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? note)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        await _joinRequestService.RejectAsync(id, CurrentUser.CompanyId.Value, note);
        TempData["Message"] = "Đã từ chối yêu cầu.";
        return RedirectToPage();
    }
}

[Authorize(Roles = RoleNames.Employee)]
public class MyModel : SecurePageModel
{
    private readonly IJoinRequestService _joinRequestService;
    public MyModel(ICurrentUserService currentUser, IJoinRequestService joinRequestService) : base(currentUser) => _joinRequestService = joinRequestService;
    public List<JoinCompanyRequest> Requests { get; set; } = [];
    [BindProperty] public string CompanyCode { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        if (CurrentUser.UserId is not null)
            Requests = await _joinRequestService.GetForUserAsync(CurrentUser.UserId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        var result = await _joinRequestService.SubmitAsync(CurrentUser.UserId, CompanyCode);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
