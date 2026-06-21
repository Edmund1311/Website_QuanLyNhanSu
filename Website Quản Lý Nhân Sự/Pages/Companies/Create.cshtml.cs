using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Companies;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class CreateModel : PageModel
{
    private readonly ICompanyService _companyService;

    public CreateModel(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [BindProperty]
    public Company Company { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _companyService.CreateAsync(Company);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return Page();
        }

        TempData["Message"] = result.Message;
        return RedirectToPage("Index");
    }
}
