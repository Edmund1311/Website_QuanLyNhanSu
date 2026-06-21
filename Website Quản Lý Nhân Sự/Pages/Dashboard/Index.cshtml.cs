using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Dashboard;

public class IndexModel : SecurePageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(ICurrentUserService currentUser, IDashboardService dashboardService) : base(currentUser)
    {
        _dashboardService = dashboardService;
    }

    public bool IsEmployeeView { get; set; }
    public DashboardViewModel Dashboard { get; set; } = new();
    public EmployeeDashboardViewModel? EmployeeDashboard { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.IsSuperAdmin)
        {
            return RedirectToPage("/Companies/Index");
        }

        if (CurrentUser.CompanyId is null)
        {
            return RedirectToPage("/JoinRequests/My");
        }

        if (CurrentUser.IsEmployee && !CurrentUser.CanManageHRData)
        {
            if (CurrentUser.UserId is null)
            {
                return RedirectToPage("/Account/Login");
            }

            EmployeeDashboard = await _dashboardService.GetEmployeeDashboardAsync(CurrentUser.UserId);
            if (EmployeeDashboard is null)
            {
                return RedirectToPage("/JoinRequests/My");
            }

            IsEmployeeView = true;
            return Page();
        }

        Dashboard = await _dashboardService.GetDashboardAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
}
