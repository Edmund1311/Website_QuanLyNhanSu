using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(int companyId);
    Task<EmployeeDashboardViewModel?> GetEmployeeDashboardAsync(string userId);
}

public class DashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalPositions { get; set; }
    public int TotalContracts { get; set; }
    public int ExpiringContracts { get; set; }
    public int ExpiredContracts { get; set; }
    public List<ChartItem> EmployeesByDepartment { get; set; } = [];
    public List<ChartItem> ContractsByStatus { get; set; } = [];
}

public record ChartItem(string Label, int Value);

public class EmployeeDashboardViewModel
{
    public Employee Employee { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public int WorkDaysThisMonth { get; set; }
    public int StandardWorkDays { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal EstimatedSalary { get; set; }
    public bool HasOfficialSalary { get; set; }
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int companyId)
    {
        var model = new DashboardViewModel();

        IQueryable<Employee> employees = _context.Employees.Include(e => e.Department).Where(e => e.CompanyId == companyId);
        IQueryable<Department> departments = _context.Departments.Where(d => d.CompanyId == companyId);
        IQueryable<Position> positions = _context.Positions.Where(p => p.CompanyId == companyId);
        IQueryable<Contract> contracts = _context.Contracts.Where(c => c.Employee.CompanyId == companyId);

        model.TotalEmployees = await employees.CountAsync();
        model.TotalDepartments = await departments.CountAsync();
        model.TotalPositions = await positions.CountAsync();
        model.TotalContracts = await contracts.CountAsync();
        model.ExpiringContracts = await contracts.CountAsync(c => c.Status == ContractStatus.ExpiringSoon);
        model.ExpiredContracts = await contracts.CountAsync(c => c.Status == ContractStatus.Expired);

        model.EmployeesByDepartment = await employees
            .GroupBy(e => e.Department.Name)
            .Select(g => new ChartItem(g.Key, g.Count()))
            .ToListAsync();

        model.ContractsByStatus = await contracts
            .GroupBy(c => c.Status)
            .Select(g => new ChartItem(g.Key.ToString(), g.Count()))
            .ToListAsync();

        return model;
    }

    public async Task<EmployeeDashboardViewModel?> GetEmployeeDashboardAsync(string userId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var workDays = await _context.Attendances.CountAsync(a =>
            a.EmployeeId == employee.Id &&
            a.WorkDate >= monthStart &&
            a.WorkDate < monthEnd &&
            (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));

        var standardWorkDays = CountWeekdaysInMonth(now.Year, now.Month);
        var baseSalary = employee.Position?.BaseSalary ?? 0;

        var officialSalary = await _context.Salaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id && s.Month == now.Month && s.Year == now.Year);

        decimal estimatedSalary;
        var hasOfficial = officialSalary is not null;
        if (hasOfficial)
        {
            estimatedSalary = officialSalary!.TotalSalary;
        }
        else if (standardWorkDays > 0)
        {
            estimatedSalary = Math.Round(baseSalary * workDays / standardWorkDays, 0);
        }
        else
        {
            estimatedSalary = 0;
        }

        return new EmployeeDashboardViewModel
        {
            Employee = employee,
            CompanyName = employee.Company?.Name ?? string.Empty,
            WorkDaysThisMonth = workDays,
            StandardWorkDays = standardWorkDays,
            BaseSalary = baseSalary,
            EstimatedSalary = estimatedSalary,
            HasOfficialSalary = hasOfficial
        };
    }

    private static int CountWeekdaysInMonth(int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var day = 1; day <= days; day++)
        {
            var dow = new DateTime(year, month, day).DayOfWeek;
            if (dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }
}

public interface IJoinRequestService
{
    Task<List<JoinCompanyRequest>> GetForCompanyAsync(int companyId, JoinRequestStatus? status = null);
    Task<List<JoinCompanyRequest>> GetForUserAsync(string userId);
    Task<(bool Success, string Message)> SubmitAsync(string userId, string companyCode);
    Task<(bool Success, string Message)> ApproveAsync(int requestId, int companyId, string employeeCode, int departmentId, int positionId, WorkStatus workStatus, UserManager<ApplicationUser> userManager);
    Task RejectAsync(int requestId, int companyId, string? note);
}

public class JoinRequestService : IJoinRequestService
{
    private readonly ApplicationDbContext _context;

    public JoinRequestService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<JoinCompanyRequest>> GetForCompanyAsync(int companyId, JoinRequestStatus? status = null)
    {
        var query = _context.JoinCompanyRequests
            .Include(r => r.User)
            .Where(r => r.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status);
        }

        return query.OrderByDescending(r => r.SubmittedAt).ToListAsync();
    }

    public Task<List<JoinCompanyRequest>> GetForUserAsync(string userId)
        => _context.JoinCompanyRequests
            .Include(r => r.Company)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

    public async Task<(bool Success, string Message)> SubmitAsync(string userId, string companyCode)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Code == companyCode.Trim().ToUpperInvariant());
        if (company is null)
        {
            return (false, "Mã công ty không hợp lệ.");
        }

        if (company.Status == CompanyStatus.Locked)
        {
            return (false, "Công ty đang bị khóa.");
        }

        var pending = await _context.JoinCompanyRequests.AnyAsync(r =>
            r.UserId == userId &&
            r.CompanyId == company.Id &&
            r.Status == JoinRequestStatus.Pending);

        if (pending)
        {
            return (false, "Bạn đã gửi yêu cầu và đang chờ duyệt.");
        }

        _context.JoinCompanyRequests.Add(new JoinCompanyRequest
        {
            UserId = userId,
            CompanyId = company.Id
        });

        await _context.SaveChangesAsync();
        return (true, "Gửi yêu cầu tham gia thành công.");
    }

    public async Task<(bool Success, string Message)> ApproveAsync(
        int requestId,
        int companyId,
        string employeeCode,
        int departmentId,
        int positionId,
        WorkStatus workStatus,
        UserManager<ApplicationUser> userManager)
    {
        var request = await _context.JoinCompanyRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId);

        if (request is null)
        {
            return (false, "Không tìm thấy yêu cầu.");
        }

        if (await _context.Employees.AnyAsync(e => e.CompanyId == companyId && e.EmployeeCode == employeeCode))
        {
            return (false, "Mã nhân viên đã tồn tại.");
        }

        var user = request.User;
        user.CompanyId = companyId;
        await userManager.AddToRoleAsync(user, RoleNames.Employee);

        var employee = new Employee
        {
            UserId = user.Id,
            CompanyId = companyId,
            DepartmentId = departmentId,
            PositionId = positionId,
            EmployeeCode = employeeCode,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkStatus = workStatus,
            HireDate = DateTime.UtcNow.Date
        };

        request.Status = JoinRequestStatus.Approved;
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return (true, "Duyệt yêu cầu thành công.");
    }

    public async Task RejectAsync(int requestId, int companyId, string? note)
    {
        var request = await _context.JoinCompanyRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId);

        if (request is null)
        {
            return;
        }

        request.Status = JoinRequestStatus.Rejected;
        request.Note = note;
        await _context.SaveChangesAsync();
    }
}
