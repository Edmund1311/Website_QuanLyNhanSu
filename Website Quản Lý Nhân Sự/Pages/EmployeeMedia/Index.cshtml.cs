using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;
using EmployeeMediaEntity = Website_Quản_Lý_Nhân_Sự.Models.EmployeeMedia;

namespace Website_Quản_Lý_Nhân_Sự.Pages.EmployeeMedia;

[Authorize(Roles = RoleNames.Employee)]
public class IndexModel : SecurePageModel
{
    private readonly IEmployeeMediaService _mediaService;
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public IndexModel(ICurrentUserService currentUser, IEmployeeMediaService mediaService, IEmployeeService employeeService, IWebHostEnvironment env) : base(currentUser)
    {
        _mediaService = mediaService; _employeeService = employeeService; _env = env;
    }

    public Employee? Employee { get; set; }
    public List<EmployeeMediaEntity> MediaFiles { get; set; } = [];
    [BindProperty] public IFormFile? UploadFile { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        Employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (Employee is null) { TempData["Error"] = "Bạn chưa có hồ sơ nhân viên."; return RedirectToPage("/JoinRequests/My"); }
        MediaFiles = await _mediaService.GetForEmployeeAsync(Employee.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        Employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (Employee is null || UploadFile is null) return RedirectToPage();

        var path = await FileUploadHelper.SaveFileAsync(UploadFile, _env.WebRootPath, "media");
        await _mediaService.CreateAsync(new EmployeeMediaEntity
        {
            EmployeeId = Employee.Id,
            FileName = UploadFile.FileName,
            FilePath = path,
            MediaType = FileUploadHelper.ResolveMediaType(UploadFile.FileName)
        });

        TempData["Message"] = "Upload file thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        Employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (Employee is null) return NotFound();
        var media = await _mediaService.GetByIdAsync(id, Employee.Id);
        if (media is null) return NotFound();
        var physical = Path.Combine(_env.WebRootPath, media.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return PhysicalFile(physical, "application/octet-stream", media.FileName);
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (CurrentUser.UserId is null) return RedirectToPage("/Account/Login");
        Employee = await _employeeService.GetByUserIdAsync(CurrentUser.UserId);
        if (Employee is null) return NotFound();
        await _mediaService.DeleteAsync(id, Employee.Id);
        TempData["Message"] = "Đã xóa file.";
        return RedirectToPage();
    }
}
