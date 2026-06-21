using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages;

public abstract class SecurePageModel : PageModel
{
    protected readonly ICurrentUserService CurrentUser;

    protected SecurePageModel(ICurrentUserService currentUser)
    {
        CurrentUser = currentUser;
    }

    protected int? RequireCompanyId()
    {
        if (CurrentUser.IsSuperAdmin)
        {
            return CurrentUser.CompanyId;
        }

        if (CurrentUser.CompanyId is null)
        {
            throw new InvalidOperationException("Tài khoản chưa thuộc công ty nào.");
        }

        return CurrentUser.CompanyId;
    }

    protected IActionResult RedirectIfNoCompany()
    {
        if (!CurrentUser.IsSuperAdmin && CurrentUser.CompanyId is null)
        {
            TempData["Message"] = "Vui lòng tham gia công ty trước khi sử dụng chức năng này.";
            return RedirectToPage("/JoinRequests/My");
        }

        return Page();
    }
}
