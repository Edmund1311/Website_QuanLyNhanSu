using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.AdminManagement;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class AssignModel : PageModel
{
    private readonly IAdminManagementService _adminManagementService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyService _companyService;

    public AssignModel(
        IAdminManagementService adminManagementService,
        UserManager<ApplicationUser> userManager,
        ICompanyService companyService)
    {
        _adminManagementService = adminManagementService;
        _userManager = userManager;
        _companyService = companyService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Company> Companies { get; set; } = [];
    public List<ApplicationUser> AvailableUsers { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Chọn người dùng là bắt buộc")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chọn công ty là bắt buộc")]
        public int CompanyId { get; set; }
    }

    public async Task OnGetAsync()
    {
        Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
        // Get all users that don't have CompanyId assigned
        AvailableUsers = await _userManager.Users
            .Where(u => u.CompanyId == null)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
            AvailableUsers = await _userManager.Users
                .Where(u => u.CompanyId == null)
                .OrderBy(u => u.Email)
                .ToListAsync();
            return Page();
        }

        var result = await _adminManagementService.AssignAdminToCompanyAsync(Input.UserId, Input.CompanyId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
            AvailableUsers = await _userManager.Users
                .Where(u => u.CompanyId == null)
                .OrderBy(u => u.Email)
                .ToListAsync();
            return Page();
        }

        TempData["Message"] = result.Message;
        return RedirectToPage("Index");
    }
}
