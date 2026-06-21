using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages;

public class IndexModel : PageModel
{
    private readonly ICompanyService _companyService;

    public IndexModel(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [BindProperty]
    public string CompanyCode { get; set; } = string.Empty;

    public bool CompanyFound { get; set; }
    public string? CompanyName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostCheckCompanyAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyCode))
        {
            CompanyFound = false;
            CompanyName = null;
            return Page();
        }

        var company = await _companyService.GetByCodeAsync(CompanyCode.Trim().ToUpperInvariant());
        CompanyFound = company is not null;
        CompanyName = company?.Name;
        return Page();
    }
}
