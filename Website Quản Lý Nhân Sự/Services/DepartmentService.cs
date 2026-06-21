using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync(int companyId, string? search = null);
    Task<Department?> GetByIdAsync(int id, int companyId);
    Task CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task SoftDeleteAsync(int id, int companyId);
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
        var query = _context.Departments.Where(d => d.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Name.Contains(search));
        }

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public Task<Department?> GetByIdAsync(int id, int companyId)
        => _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

    public async Task CreateAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id, int companyId)
    {
        var department = await GetByIdAsync(id, companyId);
        if (department is null)
        {
            return;
        }

        department.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}

public interface IPositionService
{
    Task<List<Position>> GetAllAsync(int companyId, string? search = null);
    Task<Position?> GetByIdAsync(int id, int companyId);
    Task CreateAsync(Position position);
    Task UpdateAsync(Position position);
    Task SoftDeleteAsync(int id, int companyId);
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
        var query = _context.Positions.Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Position?> GetByIdAsync(int id, int companyId)
        => _context.Positions.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

    public async Task CreateAsync(Position position)
    {
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Position position)
    {
        _context.Positions.Update(position);
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int id, int companyId)
    {
        var position = await GetByIdAsync(id, companyId);
        if (position is null)
        {
            return;
        }

        position.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}
