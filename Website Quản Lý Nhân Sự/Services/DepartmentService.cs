using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync(int companyId, string? search = null);
    Task<Department?> GetByIdAsync(int id, int companyId);
    Task<(bool Success, string Message)> CreateAsync(Department department, int companyId);
    Task<(bool Success, string Message)> UpdateAsync(Department department, int companyId);
    Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId);
}

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Department>> GetAllAsync(int companyId, string? search = null)
    {
        var query = _context.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(searchLower));
        }

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public Task<Department?> GetByIdAsync(int id, int companyId)
        => _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

    public async Task<(bool Success, string Message)> CreateAsync(Department department, int companyId)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            return (false, "Tên phòng ban là bắt buộc");
        }

        if (department.Name.Length > 150)
        {
            return (false, "Tên phòng ban không được vượt quá 150 ký tự");
        }

        // Check if department name already exists in company
        if (await _context.Departments.AnyAsync(d => d.CompanyId == companyId && d.Name == department.Name))
        {
            return (false, "Phòng ban với tên này đã tồn tại trong công ty");
        }

        department.CompanyId = companyId;
        department.IsDeleted = false;
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return (true, "Thêm phòng ban thành công");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Department department, int companyId)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            return (false, "Tên phòng ban là bắt buộc");
        }

        if (department.Name.Length > 150)
        {
            return (false, "Tên phòng ban không được vượt quá 150 ký tự");
        }

        var existing = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == department.Id && d.CompanyId == companyId);

        if (existing is null)
        {
            return (false, "Không tìm thấy phòng ban");
        }

        // Check if new name conflicts with another department
        if (existing.Name != department.Name && 
            await _context.Departments.AnyAsync(d => d.CompanyId == companyId && d.Name == department.Name && d.Id != department.Id))
        {
            return (false, "Phòng ban với tên này đã tồn tại trong công ty");
        }

        existing.Name = department.Name;
        existing.Description = department.Description;
        _context.Departments.Update(existing);
        await _context.SaveChangesAsync();
        return (true, "Cập nhật phòng ban thành công");
    }

    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

        if (department is null)
        {
            return (false, "Không tìm thấy phòng ban");
        }

        // Check if department has employees
        var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
        if (hasEmployees)
        {
            return (false, "Không thể xóa phòng ban đang có nhân viên");
        }

        department.IsDeleted = true;
        await _context.SaveChangesAsync();
        return (true, "Xóa phòng ban thành công");
    }
}

public interface IPositionService
{
    Task<List<Position>> GetAllAsync(int companyId, string? search = null);
    Task<Position?> GetByIdAsync(int id, int companyId);
    Task<(bool Success, string Message)> CreateAsync(Position position, int companyId);
    Task<(bool Success, string Message)> UpdateAsync(Position position, int companyId);
    Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId);
}

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _context;

    public PositionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Position>> GetAllAsync(int companyId, string? search = null)
    {
        var query = _context.Positions
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Position?> GetByIdAsync(int id, int companyId)
        => _context.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

    public async Task<(bool Success, string Message)> CreateAsync(Position position, int companyId)
    {
        if (string.IsNullOrWhiteSpace(position.Name))
        {
            return (false, "Tên chức vụ là bắt buộc");
        }

        if (position.Name.Length > 150)
        {
            return (false, "Tên chức vụ không được vượt quá 150 ký tự");
        }

        if (position.BaseSalary <= 0)
        {
            return (false, "Lương cơ bản phải lớn hơn 0");
        }

        // Check if position name already exists in company
        if (await _context.Positions.AnyAsync(p => p.CompanyId == companyId && p.Name == position.Name))
        {
            return (false, "Chức vụ với tên này đã tồn tại trong công ty");
        }

        position.CompanyId = companyId;
        position.IsDeleted = false;
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
        return (true, "Thêm chức vụ thành công");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Position position, int companyId)
    {
        if (string.IsNullOrWhiteSpace(position.Name))
        {
            return (false, "Tên chức vụ là bắt buộc");
        }

        if (position.Name.Length > 150)
        {
            return (false, "Tên chức vụ không được vượt quá 150 ký tự");
        }

        if (position.BaseSalary <= 0)
        {
            return (false, "Lương cơ bản phải lớn hơn 0");
        }

        var existing = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == position.Id && p.CompanyId == companyId);

        if (existing is null)
        {
            return (false, "Không tìm thấy chức vụ");
        }

        // Check if new name conflicts with another position
        if (existing.Name != position.Name && 
            await _context.Positions.AnyAsync(p => p.CompanyId == companyId && p.Name == position.Name && p.Id != position.Id))
        {
            return (false, "Chức vụ với tên này đã tồn tại trong công ty");
        }

        existing.Name = position.Name;
        existing.Description = position.Description;
        existing.BaseSalary = position.BaseSalary;
        _context.Positions.Update(existing);
        await _context.SaveChangesAsync();
        return (true, "Cập nhật chức vụ thành công");
    }

    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id, int companyId)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (position is null)
        {
            return (false, "Không tìm thấy chức vụ");
        }

        // Check if position has employees
        var hasEmployees = await _context.Employees.AnyAsync(e => e.PositionId == id);
        if (hasEmployees)
        {
            return (false, "Không thể xóa chức vụ đang có nhân viên");
        }

        position.IsDeleted = true;
        await _context.SaveChangesAsync();
        return (true, "Xóa chức vụ thành công");
    }
}
