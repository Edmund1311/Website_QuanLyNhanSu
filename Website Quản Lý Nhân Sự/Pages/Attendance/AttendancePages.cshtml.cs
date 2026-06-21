using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;
using Website_Quản_Lý_Nhân_Sự.Services;
using AttendanceEntity = Website_Quản_Lý_Nhân_Sự.Models.Attendance;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Attendance;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    public IndexModel(ICurrentUserService currentUser, IAttendanceService attendanceService, IEmployeeService employeeService) : base(currentUser)
    { _attendanceService = attendanceService; _employeeService = employeeService; }
    public List<AttendanceEntity> Records { get; set; } = [];
    public List<Employee> Employees { get; set; } = [];
    [BindProperty(SupportsGet = true)] public int? EmployeeId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Date { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Records = await _attendanceService.GetAllAsync(CurrentUser.CompanyId.Value, EmployeeId, Date);
        Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var records = await _attendanceService.GetAllAsync(CurrentUser.CompanyId.Value, EmployeeId, Date);
        return File(ExportService.ExportAttendance(records), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChamCong.xlsx");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    public CreateModel(ICurrentUserService currentUser, IAttendanceService attendanceService, IEmployeeService employeeService) : base(currentUser)
    { _attendanceService = attendanceService; _employeeService = employeeService; }
    [BindProperty] public AttendanceEntity Attendance { get; set; } = new() { WorkDate = DateTime.Today };
    [BindProperty] public TimeSpan? CheckInTime { get; set; }
    [BindProperty] public TimeSpan? CheckOutTime { get; set; }
    public List<Employee> Employees { get; set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Attendance.CheckIn = CheckInTime; Attendance.CheckOut = CheckOutTime;
        await _attendanceService.CreateAsync(Attendance, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Thêm chấm công thành công.";
        return RedirectToPage("Index");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    public EditModel(ICurrentUserService currentUser, IAttendanceService attendanceService, IEmployeeService employeeService) : base(currentUser)
    { _attendanceService = attendanceService; _employeeService = employeeService; }
    [BindProperty] public AttendanceEntity Attendance { get; set; } = new();
    [BindProperty] public TimeSpan? CheckInTime { get; set; }
    [BindProperty] public TimeSpan? CheckOutTime { get; set; }
    public List<Employee> Employees { get; set; } = [];
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _attendanceService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Attendance = item; CheckInTime = item.CheckIn; CheckOutTime = item.CheckOut;
        Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Attendance.CheckIn = CheckInTime; Attendance.CheckOut = CheckOutTime;
        await _attendanceService.UpdateAsync(Attendance, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Cập nhật chấm công thành công.";
        return RedirectToPage("Index");
    }
}
