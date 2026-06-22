using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.AdminManagement;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class IndexModel : PageModel
{
    private readonly IAdminManagementService _adminManagementService;

    public IndexModel(IAdminManagementService adminManagementService)
    {
        _adminManagementService = adminManagementService;
    }

    public List<ApplicationUser> Admins { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Admins = await _adminManagementService.GetAllAdminsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string userId)
    {
        var result = await _adminManagementService.RemoveAdminRoleAsync(userId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
        }
        else
        {
            TempData["Message"] = result.Message;
        }

        return RedirectToPage();
    }
}
