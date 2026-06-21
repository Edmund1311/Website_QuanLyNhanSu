using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public class CompanyCodeGenerator
{
    public static string Generate()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

public interface ICompanyService
{
    Task<List<Company>> GetAllAsync(string? search = null);
    Task<Company?> GetByIdAsync(int id);
    Task<Company?> GetByCodeAsync(string code);
    Task<(bool Success, string Message)> CreateAsync(Company company);
    Task<(bool Success, string Message)> UpdateAsync(Company company);
    Task ToggleLockAsync(int id);
}

public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;

    public CompanyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Company>> GetAllAsync(string? search = null)
    {
        var query = _context.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                c.Code.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public Task<Company?> GetByIdAsync(int id)
        => _context.Companies.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Company?> GetByCodeAsync(string code)
        => _context.Companies.FirstOrDefaultAsync(c => c.Code == code);

    public async Task<(bool Success, string Message)> CreateAsync(Company company)
    {
        company.Code = string.IsNullOrWhiteSpace(company.Code)
            ? CompanyCodeGenerator.Generate()
            : company.Code.Trim().ToUpperInvariant();

        if (await _context.Companies.AnyAsync(c => c.Code == company.Code))
        {
            return (false, "Mã công ty đã tồn tại.");
        }

        company.CreatedAt = DateTime.UtcNow;
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return (true, "Tạo công ty thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Company company)
    {
        var existing = await _context.Companies.FindAsync(company.Id);
        if (existing is null)
        {
            return (false, "Không tìm thấy công ty.");
        }

        existing.Name = company.Name;
        existing.Address = company.Address;
        existing.Phone = company.Phone;
        existing.Email = company.Email;
        existing.Description = company.Description;
        existing.Status = company.Status;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật công ty thành công.");
    }

    public async Task ToggleLockAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company is null)
        {
            return;
        }

        company.Status = company.Status == Models.Enums.CompanyStatus.Active
            ? Models.Enums.CompanyStatus.Locked
            : Models.Enums.CompanyStatus.Active;

        await _context.SaveChangesAsync();
    }
}
