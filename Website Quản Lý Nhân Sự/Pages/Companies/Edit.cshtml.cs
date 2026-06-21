using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Companies;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class EditModel : PageModel
{
    private readonly ICompanyService _companyService;

    public EditModel(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [BindProperty]
    public Company Company { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company is null) return NotFound();
        Company = company;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var result = await _companyService.UpdateAsync(Company);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return result.Success ? RedirectToPage("Index") : Page();
    }
}
