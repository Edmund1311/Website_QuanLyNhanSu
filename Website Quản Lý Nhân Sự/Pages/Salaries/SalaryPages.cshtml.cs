using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Salaries;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly ISalaryService _salaryService;
    public IndexModel(ICurrentUserService currentUser, ISalaryService salaryService) : base(currentUser) => _salaryService = salaryService;
    public List<Salary> Salaries { get; set; } = [];
    [BindProperty(SupportsGet = true)] public int? Month { get; set; }
    [BindProperty(SupportsGet = true)] public int? Year { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Salaries = await _salaryService.GetAllAsync(CurrentUser.CompanyId.Value, Month, Year);
        return Page();
    }
    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var items = await _salaryService.GetAllAsync(CurrentUser.CompanyId.Value, Month, Year);
        return File(ExportService.ExportSalaries(items), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BangLuong.xlsx");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly ISalaryService _salaryService;
    private readonly IEmployeeService _employeeService;
    public CreateModel(ICurrentUserService currentUser, ISalaryService salaryService, IEmployeeService employeeService) : base(currentUser)
    { _salaryService = salaryService; _employeeService = employeeService; }
    [BindProperty] public Salary Salary { get; set; } = new() { Month = DateTime.Now.Month, Year = DateTime.Now.Year };
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
        try
        {
            await _salaryService.CreateAsync(Salary, CurrentUser.CompanyId.Value);
            TempData["Message"] = "Thêm bảng lương thành công.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
            return Page();
        }
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly ISalaryService _salaryService;
    public EditModel(ICurrentUserService currentUser, ISalaryService salaryService) : base(currentUser) => _salaryService = salaryService;
    [BindProperty] public Salary Salary { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _salaryService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Salary = item; return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        try
        {
            await _salaryService.UpdateAsync(Salary, CurrentUser.CompanyId.Value);
            TempData["Message"] = "Cập nhật bảng lương thành công.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
    }
    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var salary = await _salaryService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (salary is null) return NotFound();
        return File(ExportService.ExportSalaryPdf(salary), "application/pdf", $"PhieuLuong_{salary.Employee.FullName}.pdf");
    }
}
