using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public interface IAdminManagementService
{
    Task<List<ApplicationUser>> GetAllAdminsAsync();
    Task<List<Company>> GetCompaniesWithoutAdminAsync();
    Task<(bool Success, string Message)> AssignAdminToCompanyAsync(string userId, int companyId);
    Task<(bool Success, string Message)> RemoveAdminRoleAsync(string userId);
    Task<(bool Success, string Message)> CreateCompanyAdminAsync(string email, string password, int companyId);
}

public class AdminManagementService : IAdminManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminManagementService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Get all CompanyAdmin users
    /// </summary>
    public async Task<List<ApplicationUser>> GetAllAdminsAsync()
    {
        var adminRole = await _userManager.GetUsersInRoleAsync(RoleNames.CompanyAdmin);
        return adminRole
            .AsQueryable()
            .AsNoTracking()
            .Include(u => u.Company)
            .OrderBy(u => u.Email)
            .ToList();
    }

    /// <summary>
    /// Get companies that don't have an admin assigned yet
    /// </summary>
    public async Task<List<Company>> GetCompaniesWithoutAdminAsync()
    {
        var adminUsers = await _userManager.GetUsersInRoleAsync(RoleNames.CompanyAdmin);
        var companiesWithAdmin = adminUsers.Select(u => u.CompanyId).Distinct().ToList();

        return await _context.Companies
            .AsNoTracking()
            .Where(c => !companiesWithAdmin.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Assign existing user to be admin of a company
    /// </summary>
    public async Task<(bool Success, string Message)> AssignAdminToCompanyAsync(string userId, int companyId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "Không tìm thấy người dùng");
        }

        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            return (false, "Không tìm thấy công ty");
        }

        // Check if user already has admin role
        var isAdmin = await _userManager.IsInRoleAsync(user, RoleNames.CompanyAdmin);
        if (isAdmin)
        {
            return (false, "Người dùng đã là quản trị viên công ty");
        }

        user.CompanyId = companyId;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, "Cập nhật thông tin người dùng thất bại");
        }

        result = await _userManager.AddToRoleAsync(user, RoleNames.CompanyAdmin);
        if (!result.Succeeded)
        {
            return (false, "Thêm quyền quản trị viên thất bại");
        }

        return (true, $"Gán {user.Email} làm quản trị viên công ty {company.Name} thành công");
    }

    /// <summary>
    /// Remove CompanyAdmin role from a user
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveAdminRoleAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "Không tìm thấy người dùng");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, RoleNames.CompanyAdmin);
        if (!result.Succeeded)
        {
            return (false, "Xóa quyền quản trị viên thất bại");
        }

        return (true, $"Xóa quyền quản trị viên của {user.Email} thành công");
    }

    /// <summary>
    /// Create new CompanyAdmin account for a company
    /// </summary>
    public async Task<(bool Success, string Message)> CreateCompanyAdminAsync(string email, string password, int companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null)
        {
            return (false, "Không tìm thấy công ty");
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return (false, "Email đã tồn tại trong hệ thống");
        }

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = $"Admin - {company.Name}",
            CompanyId = companyId
        };

        var result = await _userManager.CreateAsync(newUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Tạo người dùng thất bại: {errors}");
        }

        result = await _userManager.AddToRoleAsync(newUser, RoleNames.CompanyAdmin);
        if (!result.Succeeded)
        {
            return (false, "Thêm quyền quản trị viên thất bại");
        }

        return (true, $"Tạo tài khoản quản trị viên cho {company.Name} thành công. Email: {email}");
    }
}
