using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JoinCompanyRequest> JoinCompanyRequests => Set<JoinCompanyRequest>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeMedia> EmployeeMedia => Set<EmployeeMedia>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Salary> Salaries => Set<Salary>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>()
            .HasIndex(c => c.Code)
            .IsUnique();

        builder.Entity<Department>()
            .HasQueryFilter(d => !d.IsDeleted);

        builder.Entity<Position>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<Employee>()
            .HasQueryFilter(e => !e.IsDeleted);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<JoinCompanyRequest>()
            .HasOne(r => r.User)
            .WithMany(u => u.JoinRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JoinCompanyRequest>()
            .HasOne(r => r.Company)
            .WithMany(c => c.JoinRequests)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Department>()
            .HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Position>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Positions)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Employee>()
            .HasOne(e => e.Company)
            .WithMany(c => c.Employees)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Employee>()
            .HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Employee>()
            .HasIndex(e => new { e.CompanyId, e.EmployeeCode })
            .IsUnique();

        // ❌ Lỗi 7.1: Thêm unique index cho Email (Lỗi 7.1)
        builder.Entity<Employee>()
            .HasIndex(e => new { e.CompanyId, e.Email })
            .IsUnique();

        builder.Entity<Salary>()
            .HasIndex(s => new { s.EmployeeId, s.Month, s.Year })
            .IsUnique();

        // ❌ Lỗi 1.3: Thêm unique index cho Attendance (Lỗi 1.3)
        builder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.WorkDate })
            .IsUnique();

        // ❌ Lỗi 6.1: Thêm unique index cho JoinCompanyRequest pending (Lỗi 6.1)
        builder.Entity<JoinCompanyRequest>()
            .HasIndex(j => new { j.UserId, j.CompanyId })
            .IsUnique()
            .HasFilter("[Status] = 0"); // Chỉ pending requests

        // ❌ Lỗi 10.2: Thay đổi Cascade sang Restrict cho Attendance để bảo vệ dữ liệu (Lỗi 10.2)
        builder.Entity<Attendance>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict); // Thay từ Cascade -> Restrict

        builder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict); // Thay từ Cascade -> Restrict

        builder.Entity<Contract>()
            .HasOne(c => c.Employee)
            .WithMany(e => e.Contracts)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict); // Thay từ Cascade -> Restrict

        builder.Entity<Salary>()
            .HasOne(s => s.Employee)
            .WithMany(e => e.Salaries)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict); // Thay từ Cascade -> Restrict

        builder.Entity<EmployeeMedia>()
            .HasOne(em => em.Employee)
            .WithMany(e => e.MediaFiles)
            .HasForeignKey(em => em.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade); // Giữ Cascade cho media files là hợp lý
    }
}
