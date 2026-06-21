using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.LeaveRequests;

[Authorize]
public class IndexModel : SecurePageModel
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;
    public IndexModel(ICurrentUserService currentUser, ILeaveRequestService leaveRequestService, IEmployeeService employeeService) : base(currentUser)
    { _leaveRequestService = leaveRequestService; _employeeService = employeeService; }
    public List<LeaveRequest> Requests { get; set; } = [];
    public bool CanManageHR => CurrentUser.CanManageHRData;

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CanManageHRData && CurrentUser.CompanyId.HasValue)
            Requests = await _leaveRequestService.GetForCompanyAsync(CurrentUser.CompanyId.Value);
        else if (CurrentUser.IsEmployee && CurrentUser.UserId is not null)
        {
            var employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
            if (employee is null) { TempData["Error"] = "Bạn chưa được gán hồ sơ nhân viên."; return RedirectToPage("/JoinRequests/My"); }
            Requests = await _leaveRequestService.GetForEmployeeAsync(employee.Id);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        if (!CurrentUser.CanManageHRData || CurrentUser.CompanyId is null) return Forbid();
        await _leaveRequestService.ApproveAsync(id, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Đã duyệt đơn nghỉ phép.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        if (!CurrentUser.CanManageHRData || CurrentUser.CompanyId is null) return Forbid();
        await _leaveRequestService.RejectAsync(id, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Đã từ chối đơn nghỉ phép.";
        return RedirectToPage();
    }
}

[Authorize(Roles = RoleNames.Employee)]
public class CreateModel : SecurePageModel
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;
    public CreateModel(ICurrentUserService currentUser, ILeaveRequestService leaveRequestService, IEmployeeService employeeService) : base(currentUser)
    { _leaveRequestService = leaveRequestService; _employeeService = employeeService; }
    [BindProperty] public LeaveRequest LeaveRequest { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        var employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (employee is null) { TempData["Error"] = "Bạn chưa thuộc công ty."; return RedirectToPage("/JoinRequests/My"); }
        LeaveRequest.EmployeeId = employee.Id;
        await _leaveRequestService.CreateAsync(LeaveRequest, employee.CompanyId);
        TempData["Message"] = "Gửi đơn nghỉ phép thành công.";
        return RedirectToPage("Index");
    }
}
