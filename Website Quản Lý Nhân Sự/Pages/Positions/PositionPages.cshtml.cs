using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Positions;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly IPositionService _positionService;
    public IndexModel(ICurrentUserService currentUser, IPositionService positionService) : base(currentUser) => _positionService = positionService;
    public List<Position> Positions { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) { TempData["Error"] = "Tài khoản chưa thuộc công ty."; return RedirectToPage("/Dashboard/Index"); }
        Positions = await _positionService.GetAllAsync(CurrentUser.CompanyId.Value, Search);
        return Page();
    }
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        await _positionService.SoftDeleteAsync(id, CurrentUser.CompanyId.Value);
        TempData["Message"] = "Xóa chức vụ thành công.";
        return RedirectToPage();
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly IPositionService _positionService;
    public CreateModel(ICurrentUserService currentUser, IPositionService positionService) : base(currentUser) => _positionService = positionService;
    [BindProperty] public Position Position { get; set; } = new();
    public IActionResult OnGet() => CurrentUser.CompanyId is null ? RedirectToPage("/Dashboard/Index") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Position.CompanyId = CurrentUser.CompanyId.Value;
        await _positionService.CreateAsync(Position);
        TempData["Message"] = "Thêm chức vụ thành công.";
        return RedirectToPage("Index");
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly IPositionService _positionService;
    public EditModel(ICurrentUserService currentUser, IPositionService positionService) : base(currentUser) => _positionService = positionService;
    [BindProperty] public Position Position { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _positionService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Position = item; return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Position.CompanyId = CurrentUser.CompanyId.Value;
        await _positionService.UpdateAsync(Position);
        TempData["Message"] = "Cập nhật chức vụ thành công.";
        return RedirectToPage("Index");
    }
}
