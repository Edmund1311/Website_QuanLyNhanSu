using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.AdminManagement;

[Authorize(Roles = RoleNames.SuperAdmin)]
public class CreateModel : PageModel
{
    private readonly IAdminManagementService _adminManagementService;
    private readonly ICompanyService _companyService;

    public CreateModel(IAdminManagementService adminManagementService, ICompanyService companyService)
    {
        _adminManagementService = adminManagementService;
        _companyService = companyService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Company> Companies { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chọn công ty là bắt buộc")]
        public int CompanyId { get; set; }
    }

    public async Task OnGetAsync()
    {
        Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
            return Page();
        }

        var result = await _adminManagementService.CreateCompanyAdminAsync(Input.Email, Input.Password, Input.CompanyId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            Companies = await _adminManagementService.GetCompaniesWithoutAdminAsync();
            return Page();
        }

        TempData["Message"] = result.Message;
        return RedirectToPage("Index");
    }
}
