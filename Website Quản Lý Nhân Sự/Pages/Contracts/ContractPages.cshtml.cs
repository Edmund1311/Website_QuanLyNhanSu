using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Services;

namespace Website_Quản_Lý_Nhân_Sự.Pages.Contracts;

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class IndexModel : SecurePageModel
{
    private readonly IContractService _contractService;
    public IndexModel(ICurrentUserService currentUser, IContractService contractService) : base(currentUser) => _contractService = contractService;
    public List<Contract> Contracts { get; set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        await _contractService.RefreshStatusesAsync(CurrentUser.CompanyId.Value);
        Contracts = await _contractService.GetAllAsync(CurrentUser.CompanyId.Value, Search);
        return Page();
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class CreateModel : SecurePageModel
{
    private readonly IContractService _contractService;
    private readonly IEmployeeService _employeeService;
    public CreateModel(ICurrentUserService currentUser, IContractService contractService, IEmployeeService employeeService) : base(currentUser)
    { _contractService = contractService; _employeeService = employeeService; }
    [BindProperty] public Contract Contract { get; set; } = new();
    public List<Employee> Employees { get; set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        try
        {
            await _contractService.CreateAsync(Contract, CurrentUser.CompanyId.Value);
            TempData["Message"] = "Thêm hợp đồng thành công.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
            return Page();
        }
    }
}

[Authorize(Roles = $"{RoleNames.CompanyAdmin},{RoleNames.HR}")]
public class EditModel : SecurePageModel
{
    private readonly IContractService _contractService;
    private readonly IEmployeeService _employeeService;
    public EditModel(ICurrentUserService currentUser, IContractService contractService, IEmployeeService employeeService) : base(currentUser)
    { _contractService = contractService; _employeeService = employeeService; }
    [BindProperty] public Contract Contract { get; set; } = new();
    public List<Employee> Employees { get; set; } = [];
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var item = await _contractService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (item is null) return NotFound();
        Contract = item;
        Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        try
        {
            await _contractService.UpdateAsync(Contract, CurrentUser.CompanyId.Value);
            TempData["Message"] = "Cập nhật hợp đồng thành công.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            Employees = await _employeeService.GetAllAsync(CurrentUser.CompanyId.Value);
            return Page();
        }
    }
    public async Task<IActionResult> OnGetExportPdfAsync(int id)
    {
        if (CurrentUser.CompanyId is null) return RedirectToPage("/Dashboard/Index");
        var contract = await _contractService.GetByIdAsync(id, CurrentUser.CompanyId.Value);
        if (contract is null) return NotFound();
        return File(ExportService.ExportContractPdf(contract), "application/pdf", $"HopDong_{contract.ContractCode}.pdf");
    }
}
