using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Departments;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly IDepartmentService _departmentService;

    public IndexModel(ICurrentUserService currentUser, IDepartmentService departmentService) : base(currentUser)
    {
        _departmentService = departmentService;
    }

    public List<Department> Departments { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null)
        {
            TempData["Error"] = "Tài khoản chưa thuộc công ty.";
            return RedirectToPage("/Dashboard/Index");
        }

        Departments = await _departmentService.GetAllAsync(CurrentUser.CompanyId.Value, Search);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        await _departmentService.SoftDeleteAsync(id, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Xóa phòng ban thành công.";
        return RedirectToPage();
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly IDepartmentService _departmentService;

    public CreateModel(ICurrentUserService currentUser, IDepartmentService departmentService) : base(currentUser)
    {
        _departmentService = departmentService;
    }

    [BindProperty]
    public Department Department { get; set; } = new();

    public IActionResult OnGet()
    {
        if (CurrentUser.CompanyId is null)
        {
            TempData["Error"] = "Tài khoản chưa thuộc công ty.";
            return RedirectToPage("/Dashboard/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        if (!ModelState.IsValid) return Page();

        Department.CompanyId = CurrentUser.CompanyId.Value;
        await _departmentService.CreateAsync(Department);
        TempData["Message"] = "Thêm phòng ban thành công.";
        return RedirectToPage("Index");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly IDepartmentService _departmentService;

    public EditModel(ICurrentUserService currentUser, IDepartmentService departmentService) : base(currentUser)
    {
        _departmentService = departmentService;
    }

    [BindProperty]
    public Department Department { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _departmentService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Department = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        if (!ModelState.IsValid) return Page();
        Department.CompanyId = CurrentUser.CompanyId.Value;
        await _departmentService.UpdateAsync(Department);
        TempData["Message"] = "Cập nhật phòng ban thành công.";
        return RedirectToPage("Index");
    }
}
