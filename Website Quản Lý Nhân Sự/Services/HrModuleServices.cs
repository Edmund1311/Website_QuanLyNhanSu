using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IAttendanceService
{
    Task<List<Attendance>> GetAllAsync(int companyId, int? employeeId = null, DateTime? date = null);
    Task<Attendance?> GetByIdAsync(int id, int companyId);
    Task CreateAsync(Attendance attendance, int companyId);
    Task UpdateAsync(Attendance attendance, int companyId);
    Task DeleteAsync(int id, int companyId);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeService _employeeService;

    public AttendanceService(ApplicationDbContext context, IEmployeeService employeeService)
    {
        _context = context;
        _employeeService = employeeService;
    }

    public async Task<List<Attendance>> GetAllAsync(int companyId, int? employeeId = null, DateTime? date = null)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.Employee.CompanyId == companyId);

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        if (date.HasValue)
        {
            query = query.Where(a => a.WorkDate.Date == date.Value.Date);
        }

        return await query.OrderByDescending(a => a.WorkDate).ToListAsync();
    }

    public Task<Attendance?> GetByIdAsync(int id, int companyId)
        => _context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id && a.Employee.CompanyId == companyId);

    public async Task CreateAsync(Attendance attendance, int companyId)
    {
        var employee = await _employeeService.GetByIdAsync(attendance.EmployeeId, companyId)
            ?? throw new InvalidOperationException("Nhân viên không hợp lệ.");

        SnapshotHelper.ApplyEmployeeSnapshot(employee, attendance);
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Attendance attendance, int companyId)
    {
        var existing = await GetByIdAsync(attendance.Id, companyId)
            ?? throw new InvalidOperationException("Không tìm thấy chấm công.");

        existing.WorkDate = attendance.WorkDate;
        existing.CheckIn = attendance.CheckIn;
        existing.CheckOut = attendance.CheckOut;
        existing.Status = attendance.Status;
        existing.Note = attendance.Note;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int companyId)
    {
        var existing = await GetByIdAsync(id, companyId);
        if (existing is null)
        {
            return;
        }

        _context.Attendances.Remove(existing);
        await _context.SaveChangesAsync();
    }
}

public interface ILeaveRequestService
{
    Task<List<LeaveRequest>> GetForCompanyAsync(int companyId);
    Task<List<LeaveRequest>> GetForEmployeeAsync(int employeeId);
    Task CreateAsync(LeaveRequest request, int companyId);
    Task ApproveAsync(int id, int companyId);
    Task RejectAsync(int id, int companyId);
}

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeService _employeeService;

    public LeaveRequestService(ApplicationDbContext context, IEmployeeService employeeService)
    {
        _context = context;
        _employeeService = employeeService;
    }

    public Task<List<LeaveRequest>> GetForCompanyAsync(int companyId)
        => _context.LeaveRequests
            .Include(l => l.Employee)
            .Where(l => l.Employee.CompanyId == companyId)
            .OrderByDescending(l => l.SubmittedAt)
            .ToListAsync();

    public Task<List<LeaveRequest>> GetForEmployeeAsync(int employeeId)
        => _context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.SubmittedAt)
            .ToListAsync();

    public async Task CreateAsync(LeaveRequest request, int companyId)
    {
        var employee = await _employeeService.GetByIdAsync(request.EmployeeId, companyId)
            ?? throw new InvalidOperationException("Nhân viên không hợp lệ.");

        SnapshotHelper.ApplyEmployeeSnapshot(employee, request);
        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();
    }

    public async Task ApproveAsync(int id, int companyId)
    {
        var request = await _context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id && l.Employee.CompanyId == companyId);

        if (request is null)
        {
            return;
        }

        request.Status = LeaveRequestStatus.Approved;
        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(int id, int companyId)
    {
        var request = await _context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id && l.Employee.CompanyId == companyId);

        if (request is null)
        {
            return;
        }

        request.Status = LeaveRequestStatus.Rejected;
        await _context.SaveChangesAsync();
    }
}

public interface IContractService
{
    Task<List<Contract>> GetAllAsync(int companyId, string? search = null);
    Task<Contract?> GetByIdAsync(int id, int companyId);
    Task CreateAsync(Contract contract, int companyId);
    Task UpdateAsync(Contract contract, int companyId);
    Task DeleteAsync(int id, int companyId);
    Task RefreshStatusesAsync(int companyId);
}

public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeService _employeeService;

    public ContractService(ApplicationDbContext context, IEmployeeService employeeService)
    {
        _context = context;
        _employeeService = employeeService;
    }

    public async Task<List<Contract>> GetAllAsync(int companyId, string? search = null)
    {
        var query = _context.Contracts
            .AsNoTracking()
            .Include(c => c.Employee)
            .Where(c => c.Employee.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => c.ContractCode.ToLower().Contains(searchLower) || c.Employee.FullName.ToLower().Contains(searchLower));
        }

        return await query.OrderByDescending(c => c.StartDate).ToListAsync();
    }

    public Task<Contract?> GetByIdAsync(int id, int companyId)
        => _context.Contracts
            .AsNoTracking()
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id && c.Employee.CompanyId == companyId);

    public async Task CreateAsync(Contract contract, int companyId)
    {
        var employee = await _employeeService.GetByIdAsync(contract.EmployeeId, companyId)
            ?? throw new InvalidOperationException("Nhân viên không hợp lệ.");

        SnapshotHelper.ApplyEmployeeSnapshot(employee, contract);
        contract.Status = SnapshotHelper.ResolveContractStatus(contract.EndDate);
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Contract contract, int companyId)
    {
        var existing = await GetByIdAsync(contract.Id, companyId)
            ?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");

        existing.ContractCode = contract.ContractCode;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.Salary = contract.Salary;
        existing.Status = SnapshotHelper.ResolveContractStatus(contract.EndDate);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int companyId)
    {
        var existing = await GetByIdAsync(id, companyId);
        if (existing is null)
        {
            return;
        }

        _context.Contracts.Remove(existing);
        await _context.SaveChangesAsync();
    }

    public async Task RefreshStatusesAsync(int companyId)
    {
        var contracts = await _context.Contracts
            .Include(c => c.Employee)
            .Where(c => c.Employee.CompanyId == companyId)
            .ToListAsync();

        foreach (var contract in contracts)
        {
            contract.Status = SnapshotHelper.ResolveContractStatus(contract.EndDate);
        }

        await _context.SaveChangesAsync();
    }
}

public interface ISalaryService
{
    Task<List<Salary>> GetAllAsync(int companyId, int? month = null, int? year = null);
    Task<Salary?> GetByIdAsync(int id, int companyId);
    Task CreateAsync(Salary salary, int companyId);
    Task UpdateAsync(Salary salary, int companyId);
    Task DeleteAsync(int id, int companyId);
}

public class SalaryService : ISalaryService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmployeeService _employeeService;

    public SalaryService(ApplicationDbContext context, IEmployeeService employeeService)
    {
        _context = context;
        _employeeService = employeeService;
    }

    public async Task<List<Salary>> GetAllAsync(int companyId, int? month = null, int? year = null)
    {
        var query = _context.Salaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.Employee.CompanyId == companyId);

        if (month.HasValue)
        {
            query = query.Where(s => s.Month == month);
        }

        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year);
        }

        return await query.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync();
    }

    public Task<Salary?> GetByIdAsync(int id, int companyId)
        => _context.Salaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id && s.Employee.CompanyId == companyId);

    public async Task CreateAsync(Salary salary, int companyId)
    {
        var employee = await _employeeService.GetByIdAsync(salary.EmployeeId, companyId)
            ?? throw new InvalidOperationException("Nhân viên không hợp lệ.");

        SnapshotHelper.ApplyEmployeeSnapshot(employee, salary);
        salary.TotalSalary = SnapshotHelper.CalculateTotalSalary(
            salary.BaseSalary, salary.Allowance, salary.Bonus, salary.Deduction);

        _context.Salaries.Add(salary);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Salary salary, int companyId)
    {
        var existing = await GetByIdAsync(salary.Id, companyId)
            ?? throw new InvalidOperationException("Không tìm thấy bảng lương.");

        existing.Month = salary.Month;
        existing.Year = salary.Year;
        existing.BaseSalary = salary.BaseSalary;
        existing.Allowance = salary.Allowance;
        existing.Bonus = salary.Bonus;
        existing.Deduction = salary.Deduction;
        existing.TotalSalary = SnapshotHelper.CalculateTotalSalary(
            salary.BaseSalary, salary.Allowance, salary.Bonus, salary.Deduction);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int companyId)
    {
        var existing = await GetByIdAsync(id, companyId);
        if (existing is null)
        {
            return;
        }

        _context.Salaries.Remove(existing);
        await _context.SaveChangesAsync();
    }
}

public interface IEmployeeMediaService
{
    Task<List<EmployeeMedia>> GetForEmployeeAsync(int employeeId);
    Task<EmployeeMedia?> GetByIdAsync(int id, int employeeId);
    Task CreateAsync(EmployeeMedia media);
    Task DeleteAsync(int id, int employeeId);
}

public class EmployeeMediaService : IEmployeeMediaService
{
    private readonly ApplicationDbContext _context;

    public EmployeeMediaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<EmployeeMedia>> GetForEmployeeAsync(int employeeId)
        => _context.EmployeeMedia
            .AsNoTracking()
            .Where(m => m.EmployeeId == employeeId)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();

    public Task<EmployeeMedia?> GetByIdAsync(int id, int employeeId)
        => _context.EmployeeMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.EmployeeId == employeeId);

    public async Task CreateAsync(EmployeeMedia media)
    {
        _context.EmployeeMedia.Add(media);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int employeeId)
    {
        var media = await GetByIdAsync(id, employeeId);
        if (media is null)
        {
            return;
        }

        _context.EmployeeMedia.Remove(media);
        await _context.SaveChangesAsync();
    }
}
