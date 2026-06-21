using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Employees;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;

    public IndexModel(ICurrentUserService currentUser, IEmployeeService employeeService, IDepartmentService departmentService, IPositionService positionService) : base(currentUser)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
        _positionService = positionService;
    }

    public List<Employee> Employees { get; set; } = [];
    public List<Department> Departments { get; set; } = [];
    public List<Position> Positions { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int? DepartmentId { get; set; }
    [BindProperty(SupportsGet = true)] public int? PositionId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) { TempData["Error"] = "Tài khoản chưa thuộc công ty."; return RedirectToPage("/Dashboard/Index"); }
        var companyId = CurrentUser.CompanyId.Value;
        Employees = await _employeeService.GetAllAsync(companyId, Search, DepartmentId, PositionId);
        Departments = await _departmentService.GetAllAsync(companyId);
        Positions = await _positionService.GetAllAsync(companyId);
        return Page();
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value, Search, DepartmentId, PositionId);
        var bytes = ExportService.ExportEmployees(employees);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachNhanVien.xlsx");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        await _employeeService.SoftDeleteAsync(id, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Xóa nhân viên thành công.";
        return RedirectToPage();
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;
    private readonly IWebHostEnvironment _env;

    public CreateModel(ICurrentUserService currentUser, IEmployeeService employeeService, IDepartmentService departmentService, IPositionService positionService, IWebHostEnvironment env) : base(currentUser)
    {
        _employeeService = employeeService; _departmentService = departmentService; _positionService = positionService; _env = env;
    }

    [BindProperty] public Employee Employee { get; set; } = new();
    [BindProperty] public IFormFile? AvatarFile { get; set; }
    public List<Department> Departments { get; set; } = [];
    public List<Position> Positions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Departments = await _departmentService.GetAllAsync(CurrentUser.CompanyId.Value);
        Positions = await _positionService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Employee.CompanyId = CurrentUser.CompanyId.Value;
        if (AvatarFile is not null)
            Employee.AvatarPath = await FileUploadHelper.SaveFileAsync(AvatarFile, _env.WebRootPath, "avatars");
        await _employeeService.CreateAsync(Employee, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Thêm nhân viên thành công.";
        return RedirectToPage("Index");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly IPositionService _positionService;
    private readonly IWebHostEnvironment _env;

    public EditModel(ICurrentUserService currentUser, IEmployeeService employeeService, IDepartmentService departmentService, IPositionService positionService, IWebHostEnvironment env) : base(currentUser)
    {
        _employeeService = employeeService; _departmentService = departmentService; _positionService = positionService; _env = env;
    }

    [BindProperty] public Employee Employee { get; set; } = new();
    [BindProperty] public IFormFile? AvatarFile { get; set; }
    public List<Department> Departments { get; set; } = [];
    public List<Position> Positions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _employeeService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Employee = item;
        Departments = await _departmentService.GetAllAsync(CurrentUser.CompanyId.Value);
        Positions = await _positionService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Employee.CompanyId = CurrentUser.CompanyId.Value;
        if (AvatarFile is not null)
            Employee.AvatarPath = await FileUploadHelper.SaveFileAsync(AvatarFile, _env.WebRootPath, "avatars");
        await _employeeService.UpdateAsync(Employee, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Cập nhật nhân viên thành công.";
        return RedirectToPage("Index");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR},{RoleNames.Employee}")]
public class DetailsModel : SecurePageModel
{
    private readonly IEmployeeService _employeeService;
    public DetailsModel(ICurrentUserService currentUser, IEmployeeService employeeService) : base(currentUser) => _employeeService = employeeService;
    public Employee? Employee { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null && !CurrentUser.IsEmployee) return RedirectToPage("/Dashboard/Index");
        if (CurrentUser.IsEmployee)
        {
            var mine = await _employeeService.GetByUserIdAsync(CurrentUser.UserId!);
            if (mine is null || mine.Id != id) return Forbid();
            Employee = mine; return Page();
        }
        Employee = await _employeeService.GetByIdAsync(id, CurrentUser.CompanyId!.Value);
        return Employee is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        await OnGetAsync(id);
        if (Employee is null) return NotFound();
        return File(ExportService.ExportEmployeeProfilePdf(Employee), "application/pdf", $"HoSo_{Employee.EmployeeCode}.pdf");
    }
}

[Authorize(Roles = RoleNames.Employee)]
public class ProfileModel : SecurePageModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public ProfileModel(ICurrentUserService currentUser, IEmployeeService employeeService, IWebHostEnvironment env) : base(currentUser)
    {
        _employeeService = employeeService;
        _env = env;
    }

    public Employee? Employee { get; set; }
    [BindProperty] public IFormFile? AvatarFile { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        Employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (Employee is null)
        {
            TempData["Error"] = "Bạn chưa được gán hồ sơ nhân viên.";
            return RedirectToPage("/JoinRequests/My");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.UserId is null || CurrentUser.CompanyId is null) return RedirectToPage("/Account/Login");
        var employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (employee is null) return RedirectToPage("/JoinRequests/My");

        if (AvatarFile is not null)
        {
            var avatarPath = await FileUploadHelper.SaveFileAsync(AvatarFile, _env.WebRootPath, "avatars");
            await _employeeService.UpdateAvatarAsync(employee.Id, employee.CompanyId, avatarPath);
            TempData["Message"] = "Cập nhật ảnh đại diện thành công.";
        }

        return RedirectToPage();
    }
}
