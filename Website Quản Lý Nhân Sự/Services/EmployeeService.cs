using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(int companyId, string? search = null, int? departmentId = null, int? positionId = null);
    Task<PaginatedResult<Employee>> GetAllPaginatedAsync(int companyId, int pageNumber = 1, int pageSize = 20, string? search = null, int? departmentId = null, int? positionId = null);
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
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                e.FullName.ToLower().Contains(searchLower) ||
                e.EmployeeCode.ToLower().Contains(searchLower) ||
                e.Email.ToLower().Contains(searchLower));
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

    public async Task<PaginatedResult<Employee>> GetAllPaginatedAsync(int companyId, int pageNumber = 1, int pageSize = 20, string? search = null, int? departmentId = null, int? positionId = null)
    {
        pageNumber = PaginationHelper.ValidatePageNumber(pageNumber);
        pageSize = PaginationHelper.ValidatePageSize(pageSize);

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                e.FullName.ToLower().Contains(searchLower) ||
                e.EmployeeCode.ToLower().Contains(searchLower) ||
                e.Email.ToLower().Contains(searchLower));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (positionId.HasValue)
        {
            query = query.Where(e => e.PositionId == positionId);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Employee>(items, totalCount, pageNumber, pageSize);
    }

    public Task<Employee?> GetByIdAsync(int id, int companyId)
        => _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);

    public Task<Employee?> GetByUserIdAsync(string userId)
        => _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.UserId == userId);

    /// <summary>
    /// Get employee with tracking for update operations (fixes avatar update issue)
    /// </summary>
    private Task<Employee?> GetEmployeeByIdForUpdateAsync(int id, int companyId)
        => _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);

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

        // Cập nhật tất cả các trường cần update, không giữ lại dữ liệu cũ
        existing.FullName = employee.FullName;
        existing.Email = employee.Email;
        existing.Phone = employee.Phone;
        existing.Gender = employee.Gender;
        existing.DateOfBirth = employee.DateOfBirth;
        existing.Address = employee.Address;
        existing.DepartmentId = employee.DepartmentId;
        existing.PositionId = employee.PositionId;
        existing.WorkStatus = employee.WorkStatus;

        // QUAN TRỌNG: Xóa avatar cũ nếu có avatar mới được upload
        // Điều này được xử lý trong EditModel khi AvatarPath có giá trị
        if (!string.IsNullOrEmpty(employee.AvatarPath) && existing.AvatarPath != employee.AvatarPath)
        {
            existing.AvatarPath = employee.AvatarPath;
        }

        // Không thay đổi EmployeeCode, UserId, CompanyId, CreatedAt
        _context.Update(existing);
        await _context.SaveChangesAsync();
        return (true, "Cập nhật nhân viên thành công");
    }

    /// <summary>
    /// Xóa mềm nhân viên với proper error handling và cascade updates (Lỗi 8.3)
    /// Cập nhật: Cho phép xóa nhân viên ngay cả khi có hợp đồng Active (soft delete)
    /// Xử lý: Xóa tất cả dữ liệu liên quan trước khi soft delete employee
    /// </summary>
    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Attendances)
                .Include(e => e.LeaveRequests)
                .Include(e => e.Contracts)
                .Include(e => e.Salaries)
                .Include(e => e.MediaFiles)
                .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);

            if (employee is null)
            {
                await transaction.RollbackAsync();
                return (false, "Không tìm thấy nhân viên");
            }

            // Xóa tất cả dữ liệu liên quan để tránh Foreign Key Constraint
            if (employee.Attendances?.Any() == true)
            {
                _context.Attendances.RemoveRange(employee.Attendances);
            }

            if (employee.LeaveRequests?.Any() == true)
            {
                _context.LeaveRequests.RemoveRange(employee.LeaveRequests);
            }

            if (employee.Contracts?.Any() == true)
            {
                _context.Contracts.RemoveRange(employee.Contracts);
            }

            if (employee.Salaries?.Any() == true)
            {
                _context.Salaries.RemoveRange(employee.Salaries);
            }

            if (employee.MediaFiles?.Any() == true)
            {
                _context.EmployeeMedia.RemoveRange(employee.MediaFiles);
            }

            // Soft delete: Đánh dấu là deleted
            employee.IsDeleted = true;

            // Kiểm tra xem có cần xóa avatar
            if (!string.IsNullOrEmpty(employee.AvatarPath) && employee.AvatarPath.StartsWith("/uploads/"))
            {
                // Có thể xóa file nếu cần thiết
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Xóa nhân viên thành công");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi khi xóa nhân viên: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật avatar với validation (Lỗi 5.1, 5.3)
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAvatarAsync(int id, int companyId, string avatarPath)
    {
        // Use tracked query for update
        var employee = await GetEmployeeByIdForUpdateAsync(id, companyId);
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
