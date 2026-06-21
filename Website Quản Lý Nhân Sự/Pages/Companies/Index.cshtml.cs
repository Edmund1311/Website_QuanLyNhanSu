using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Companies;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class IndexModel : PageModel
{
    private readonly ICompanyService _companyService;

    public IndexModel(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    public List<Company> Companies { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        Companies = await _companyService.GetAllAsync(Search);
    }

    public async Task<IActionResult> OnPostToggleLockAsync(int id)
    {
        await _companyService.ToggleLockAsync(id);
        TempData["Message"] = "Cập nhật trạng thái công ty thành công.";
        return RedirectToPage();
    }
}
