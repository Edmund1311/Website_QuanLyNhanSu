using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(int companyId, string? search = null, int? departmentId = null, int? positionId = null);
    Task<Employee?> GetByIdAsync(int id, int companyId);
    Task<Employee?> GetByUserIdAsync(string userId);
    Task<(bool Success, string Message)> CreateAsync(Employee employee, int companyId);
    Task<(bool Success, string Message)> UpdateAsync(Employee employee, int companyId);
    Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId);
    Task<(bool Success, string Message)> UpdateAvatarAsync(int id, int companyId, string avatarPath);
}

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Employee>> GetAllAsync(int companyId, string? search = null, int? departmentId = null, int? positionId = null)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                e.EmployeeCode.Contains(search) ||
                e.Email.Contains(search));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (positionId.HasValue)
        {
            query = query.Where(e => e.PositionId == positionId);
        }

        return await query.OrderBy(e => e.FullName).ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(int id, int companyId)
        => _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);

    public Task<Employee?> GetByUserIdAsync(string userId)
        => _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == userId);

    /// <summary>
    /// Tạo nhân viên với validation (Lỗi 11.3)
    /// </summary>
    public async Task<(bool Success, string Message)> CreateAsync(Employee employee, int companyId)
    {
        // ❌ Lỗi 11.3: Validate nhân viên trước khi tạo
        if (!employee.IsValid(out var errorMessage))
        {
            return (false, errorMessage ?? "Dữ liệu nhân viên không hợp lệ");
        }

        // Kiểm tra Department, Position tồn tại
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == employee.DepartmentId && d.CompanyId == companyId);
        if (department is null)
        {
            return (false, "Phòng ban không tồn tại hoặc không thuộc công ty này");
        }

        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == employee.PositionId && p.CompanyId == companyId);
        if (position is null)
        {
            return (false, "Chức vụ không tồn tại hoặc không thuộc công ty này");
        }

        // Kiểm tra mã nhân viên không trùng
        if (await _context.Employees.AnyAsync(e => e.CompanyId == companyId && e.EmployeeCode == employee.EmployeeCode))
        {
            return (false, "Mã nhân viên đã tồn tại trong công ty");
        }

        // Kiểm tra email không trùng (Lỗi 7.1)
        if (await _context.Employees.AnyAsync(e => e.CompanyId == companyId && e.Email == employee.Email))
        {
            return (false, "Email đã tồn tại trong công ty");
        }

        employee.CompanyId = companyId;
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return (true, "Tạo nhân viên thành công");
    }

    /// <summary>
    /// Cập nhật nhân viên với validation (Lỗi 11.3)
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(Employee employee, int companyId)
    {
        var existing = await GetByIdAsync(employee.Id, companyId);
        if (existing is null)
        {
            return (false, "Không tìm thấy nhân viên");
        }

        // Validate dữ liệu mới
        if (!employee.IsValid(out var errorMessage))
        {
            return (false, errorMessage ?? "Dữ liệu nhân viên không hợp lệ");
        }

        // Kiểm tra email không trùng với nhân viên khác (Lỗi 7.1)
        if (existing.Email != employee.Email && 
            await _context.Employees.AnyAsync(e => e.CompanyId == companyId && e.Email == employee.Email && e.Id != employee.Id))
        {
            return (false, "Email đã tồn tại trong công ty");
        }

        existing.FullName = employee.FullName;
        existing.Email = employee.Email;
        existing.Phone = employee.Phone;
        existing.Gender = employee.Gender;
        existing.DateOfBirth = employee.DateOfBirth;
        existing.Address = employee.Address;
        existing.DepartmentId = employee.DepartmentId;
        existing.PositionId = employee.PositionId;
        existing.WorkStatus = employee.WorkStatus;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật nhân viên thành công");
    }

    /// <summary>
    /// Xóa mềm nhân viên với proper error handling (Lỗi 8.3)
    /// </summary>
    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId)
    {
        var employee = await GetByIdAsync(id, companyId);
        if (employee is null)
        {
            // ❌ Lỗi 8.3: Throw exception thay vì return im lặng
            return (false, "Không tìm thấy nhân viên");
        }

        // Kiểm tra không thể xóa nếu có hợp đồng Active
        var hasActiveContract = await _context.Contracts.AnyAsync(c => c.EmployeeId == id && c.Status == Models.Enums.ContractStatus.Active);
        if (hasActiveContract)
        {
            return (false, "Không thể xóa nhân viên có hợp đồng còn hiệu lực");
        }

        employee.IsDeleted = true;
        await _context.SaveChangesAsync();

        return (true, "Xóa nhân viên thành công");
    }

    /// <summary>
    /// Cập nhật avatar với validation (Lỗi 5.1, 5.3)
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAvatarAsync(int id, int companyId, string avatarPath)
    {
        var employee = await GetByIdAsync(id, companyId);
        if (employee is null)
        {
            return (false, "Không tìm thấy nhân viên");
        }

        // ❌ Lỗi 5.1: Sanitize file path để tránh Path Traversal
        var sanitizedPath = ValidationHelper.SanitizeFilePath(avatarPath);
        if (string.IsNullOrEmpty(sanitizedPath))
        {
            return (false, "Đường dẫn file không hợp lệ");
        }

        employee.AvatarPath = sanitizedPath;
        await _context.SaveChangesAsync();

        return (true, "Cập nhật avatar thành công");
    }
}
