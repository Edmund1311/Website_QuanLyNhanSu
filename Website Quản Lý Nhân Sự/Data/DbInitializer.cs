using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Website_Quản_Lý_Nhân_Sự.Constants;
using Website_Quản_Lý_Nhân_Sự.Data;
using Website_Quản_Lý_Nhân_Sự.Helpers;
using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Data;

public static class DbInitializer
{
    public const string DefaultSuperAdminEmail = "admin@hrms.local";
    public const string DefaultSuperAdminPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        string[] roles =
        [
            RoleNames.SuperAdmin,
            RoleNames.CompanyAdmin,
            RoleNames.HR,
            RoleNames.Employee
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (await userManager.FindByEmailAsync(DefaultSuperAdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = DefaultSuperAdminEmail,
                Email = DefaultSuperAdminEmail,
                EmailConfirmed = true,
                FullName = "Super Admin"
            };

            await userManager.CreateAsync(admin, DefaultSuperAdminPassword);
            await userManager.AddToRoleAsync(admin, RoleNames.SuperAdmin);
        }

        // ❌ Lỗi 10.3: Thêm Transaction cho seeding (Fix seeding failures)
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await SeedCompaniesAsync(context, userManager);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task SeedCompaniesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Companies.AnyAsync(c => c.Code == "DEMO01"))
        {
            var demo = await context.Companies.FirstAsync(c => c.Code == "DEMO01");

            // CẬP NHẬT: Thêm IgnoreQueryFilters() khi đếm số lượng nhân viên
            if (await context.Employees.IgnoreQueryFilters().CountAsync(e => e.CompanyId == demo.Id) >= 8)
            {
                await SeedExtraCompaniesAsync(context);
                return;
            }
        }

        if (await context.Companies.AnyAsync())
        {
            var demo = await context.Companies.FirstOrDefaultAsync(c => c.Code == "DEMO01");
            if (demo is not null)
            {
                await SeedDemoCompanyDataAsync(context, userManager, demo);
                await SeedExtraCompaniesAsync(context);
            }

            return;
        }

        var demoCompany = new Company
        {
            Name = "Công ty Demo HRMS",
            Code = "DEMO01",
            Address = "123 Nguyễn Huệ, TP.HCM",
            Phone = "0901234567",
            Email = "contact@demo-hrms.local",
            Description = "Công ty mẫu để demo hệ thống quản lý nhân sự.",
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        context.Companies.Add(demoCompany);
        await context.SaveChangesAsync();

        await SeedDemoCompanyDataAsync(context, userManager, demoCompany);
        await SeedExtraCompaniesAsync(context);
    }

    private static async Task SeedExtraCompaniesAsync(ApplicationDbContext context)
    {
        var extraCompanies =
            new (string Code, string Name, string Email)[]
            {
                ("TECH02", "Công ty Công nghệ ABC", "contact@techabc.local"),
                ("RETL03", "Công ty Bán lẻ XYZ", "info@retailxyz.local"),
                ("MANF04", "Công ty Sản xuất DEF", "hello@manfdef.local"),
                ("SERV05", "Công ty Dịch vụ GHI", "support@servghi.local")
            };

        foreach (var (code, name, email) in extraCompanies)
        {
            if (await context.Companies.AnyAsync(c => c.Code == code))
            {
                continue;
            }

            context.Companies.Add(new Company
            {
                Name = name,
                Code = code,
                Address = "Việt Nam",
                Phone = "0281234567",
                Email = email,
                Description = $"Công ty demo {name}.",
                Status = CompanyStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-Random.Shared.Next(1, 12))
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoCompanyDataAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        Company demoCompany)
    {
        // CẬP NHẬT: Thêm IgnoreQueryFilters() khi đếm số lượng nhân viên
        if (await context.Employees.IgnoreQueryFilters().CountAsync(e => e.CompanyId == demoCompany.Id) >= 8)
        {
            return;
        }

        var departments = await EnsureDepartmentsAsync(context, demoCompany.Id);
        var positions = await EnsurePositionsAsync(context, demoCompany.Id);
        await EnsureUsersAsync(userManager, demoCompany.Id);

        var companyAdmin = await userManager.FindByEmailAsync("admin@demo-hrms.local");
        var hrUser = await userManager.FindByEmailAsync("hr@demo-hrms.local");
        var employeeUser = await userManager.FindByEmailAsync("employee@demo-hrms.local");

        var employeeProfiles = new (string Code, string Name, string Email, string Phone, Gender Gender, int Dept, int Pos, string? UserId, string Avatar)[]
        {
            ("NV001", "Nguyễn Văn Demo", "employee@demo-hrms.local", "0909999888", Gender.Male, 0, 0, employeeUser?.Id, "/images/avatars/nv001.svg"),
            ("NV002", "Trần Thị Lan", "lan.tran@demo-hrms.local", "0912345001", Gender.Female, 0, 1, null, "/images/avatars/nv002.svg"),
            ("NV003", "Phạm Hoàng Minh", "minh.pham@demo-hrms.local", "0912345002", Gender.Male, 2, 4, null, "/images/avatars/nv003.svg"),
            ("NV004", "Lê Mai Anh", "mai.le@demo-hrms.local", "0912345003", Gender.Female, 3, 0, null, "/images/avatars/nv004.svg"),
            ("NV005", "Hoàng Tuấn Kiệt", "kiet.hoang@demo-hrms.local", "0912345004", Gender.Male, 4, 4, null, "/images/avatars/nv005.svg"),
            ("NV006", "Võ Khánh Linh", "linh.vo@demo-hrms.local", "0912345005", Gender.Female, 1, 3, null, "/images/avatars/nv006.svg"),
            ("NV007", "Đặng Anh Tú", "tu.dang@demo-hrms.local", "0912345005", Gender.Male, 2, 0, null, "/images/avatars/nv007.svg"),
            ("NV008", "Bùi Quang Huy", "huy.bui@demo-hrms.local", "0912345006", Gender.Male, 3, 1, null, "/images/avatars/nv008.svg")
        };

        var employees = new List<Employee>();
        foreach (var profile in employeeProfiles)
        {
            // CẬP NHẬT: Thêm IgnoreQueryFilters() khi kiểm tra trùng mã nhân viên
            if (await context.Employees
                    .IgnoreQueryFilters()
                    .AnyAsync(e => e.CompanyId == demoCompany.Id && e.EmployeeCode == profile.Code))
            {
                continue;
            }

            employees.Add(new Employee
            {
                UserId = profile.UserId,
                CompanyId = demoCompany.Id,
                DepartmentId = departments[profile.Dept].Id,
                PositionId = positions[profile.Pos].Id,
                EmployeeCode = profile.Code,
                FullName = profile.Name,
                Email = profile.Email,
                Phone = profile.Phone,
                Gender = profile.Gender,
                DateOfBirth = DateTime.UtcNow.Date.AddYears(-Random.Shared.Next(24, 45)),
                Address = "TP. Hồ Chí Minh",
                AvatarPath = profile.Avatar,
                HireDate = DateTime.UtcNow.Date.AddMonths(-Random.Shared.Next(3, 36)),
                WorkStatus = WorkStatus.Active
            });
        }

        if (employees.Count > 0)
        {
            context.Employees.AddRange(employees);
            await context.SaveChangesAsync();
        }

        var allEmployees = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.CompanyId == demoCompany.Id)
            .ToListAsync();

        var demoEmployee = allEmployees.FirstOrDefault(e => e.EmployeeCode == "NV001");
        if (demoEmployee is not null)
        {
            demoEmployee.AvatarPath ??= "/images/avatars/nv001.svg";
            demoEmployee.FullName = "Nguyễn Văn Demo";
            await context.SaveChangesAsync();
        }

        await SeedAttendancesAsync(context, allEmployees);
        await SeedLeaveRequestsAsync(context, allEmployees);
        await SeedContractsAsync(context, allEmployees);
        await SeedSalariesAsync(context, allEmployees);
        await SeedEmployeeMediaAsync(context, allEmployees);
        await SeedJoinRequestsAsync(context, demoCompany.Id, userManager);
    }

    private static async Task<List<Department>> EnsureDepartmentsAsync(ApplicationDbContext context, int companyId)
    {
        var names = new[] { "Nhân sự", "Kế toán", "Kỹ thuật", "Kinh doanh", "Marketing" };
        var departments = await context.Departments.Where(d => d.CompanyId == companyId).ToListAsync();

        foreach (var name in names)
        {
            if (departments.All(d => d.Name != name))
            {
                var dept = new Department { Name = name, Description = $"Phòng {name}", CompanyId = companyId };
                context.Departments.Add(dept);
                await context.SaveChangesAsync();
                departments.Add(dept);
            }
        }

        return departments;
    }

    private static async Task<List<Position>> EnsurePositionsAsync(ApplicationDbContext context, int companyId)
    {
        var defs = new (string Name, decimal Salary)[]
        {
            ("Nhân viên", 10_000_000),
            ("Trưởng phòng", 18_000_000),
            ("Giám đốc", 30_000_000),
            ("Kế toán viên", 12_000_000),
            ("Kỹ sư", 15_000_000)
        };

        var positions = await context.Positions.Where(p => p.CompanyId == companyId).ToListAsync();
        foreach (var (name, salary) in defs)
        {
            if (positions.All(p => p.Name != name))
            {
                var pos = new Position { Name = name, BaseSalary = salary, CompanyId = companyId };
                context.Positions.Add(pos);
                await context.SaveChangesAsync();
                positions.Add(pos);
            }
        }

        return positions;
    }

    private static async Task EnsureUsersAsync(UserManager<ApplicationUser> userManager, int companyId)
    {
        if (await userManager.FindByEmailAsync("admin@demo-hrms.local") is null)
        {
            var companyAdmin = new ApplicationUser
            {
                UserName = "admin@demo-hrms.local",
                Email = "admin@demo-hrms.local",
                EmailConfirmed = true,
                FullName = "Admin Demo",
                CompanyId = companyId
            };
            await userManager.CreateAsync(companyAdmin, "Admin@123");
            await userManager.AddToRoleAsync(companyAdmin, RoleNames.CompanyAdmin);
        }

        if (await userManager.FindByEmailAsync("hr@demo-hrms.local") is null)
        {
            var hrUser = new ApplicationUser
            {
                UserName = "hr@demo-hrms.local",
                Email = "hr@demo-hrms.local",
                EmailConfirmed = true,
                FullName = "HR Demo",
                CompanyId = companyId
            };
            await userManager.CreateAsync(hrUser, "Hr@123456");
            await userManager.AddToRoleAsync(hrUser, RoleNames.HR);
        }

        if (await userManager.FindByEmailAsync("employee@demo-hrms.local") is null)
        {
            var employeeUser = new ApplicationUser
            {
                UserName = "employee@demo-hrms.local",
                Email = "employee@demo-hrms.local",
                EmailConfirmed = true,
                FullName = "Nguyễn Văn Demo",
                CompanyId = companyId
            };
            await userManager.CreateAsync(employeeUser, "Employee@123");
            await userManager.AddToRoleAsync(employeeUser, RoleNames.Employee);
        }
    }

    private static async Task SeedAttendancesAsync(ApplicationDbContext context, List<Employee> employees)
    {
        if (await context.Attendances.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var records = new List<Attendance>();

        foreach (var employee in employees.Take(6))
        {
            for (var day = 1; day <= Math.Min(now.Day, 15); day++)
            {
                var date = new DateTime(now.Year, now.Month, day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                // ❌ Lỗi 1.4 FIX: Thay vì hard code day==3 là Late, dùng logic thực tế
                // Tạo CheckIn time: vào sớm 08:00, vào muộn 08:20, hoặc vắng mặt
                TimeSpan? checkIn = null;
                TimeSpan? checkOut = null;

                var randStatus = Random.Shared.Next(0, 100);
                if (randStatus < 80) // 80% on time
                {
                    checkIn = new TimeSpan(8, Random.Shared.Next(0, 10), 0); // 08:00-08:10
                    checkOut = new TimeSpan(17, Random.Shared.Next(0, 30), 0); // 17:00-17:30
                }
                else if (randStatus < 95) // 15% late
                {
                    checkIn = new TimeSpan(8, Random.Shared.Next(15, 45), 0); // 08:15-08:45
                    checkOut = new TimeSpan(17, Random.Shared.Next(0, 30), 0);
                }
                // else 5% absent - checkIn và checkOut null

                var attendance = new Attendance
                {
                    EmployeeId = employee.Id,
                    WorkDate = date,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    DepartmentSnapshot = employee.Department?.Name ?? "N/A",
                    PositionSnapshot = employee.Position?.Name ?? "N/A"
                };

                // Auto-calculate status dựa trên CheckIn/CheckOut
                attendance.CalculateStatus();

                records.Add(attendance);
            }
        }

        context.Attendances.AddRange(records);
        await context.SaveChangesAsync();
    }

    private static async Task SeedLeaveRequestsAsync(ApplicationDbContext context, List<Employee> employees)
    {
        if (await context.LeaveRequests.AnyAsync())
        {
            return;
        }

        var requests = new List<LeaveRequest>
        {
            new()
            {
                EmployeeId = employees[0].Id,
                StartDate = DateTime.UtcNow.Date.AddDays(10),
                EndDate = DateTime.UtcNow.Date.AddDays(12),
                LeaveType = "Nghỉ phép năm",
                Reason = "Du lịch cùng gia đình",
                Status = LeaveRequestStatus.Pending,
                DepartmentSnapshot = employees[0].Department?.Name ?? "",
                PositionSnapshot = employees[0].Position?.Name ?? ""
            },
            new()
            {
                EmployeeId = employees[1].Id,
                StartDate = DateTime.UtcNow.Date.AddDays(-5),
                EndDate = DateTime.UtcNow.Date.AddDays(-3),
                LeaveType = "Nghỉ ốm",
                Reason = "Cảm cúm",
                Status = LeaveRequestStatus.Approved,
                DepartmentSnapshot = employees[1].Department?.Name ?? "",
                PositionSnapshot = employees[1].Position?.Name ?? ""
            },
            new()
            {
                EmployeeId = employees[2].Id,
                StartDate = DateTime.UtcNow.Date.AddDays(5),
                EndDate = DateTime.UtcNow.Date.AddDays(6),
                LeaveType = "Nghỉ việc riêng",
                Reason = "Việc gia đình",
                Status = LeaveRequestStatus.Pending,
                DepartmentSnapshot = employees[2].Department?.Name ?? "",
                PositionSnapshot = employees[2].Position?.Name ?? ""
            },
            new()
            {
                EmployeeId = employees[3].Id,
                StartDate = DateTime.UtcNow.Date.AddDays(-20),
                EndDate = DateTime.UtcNow.Date.AddDays(-18),
                LeaveType = "Nghỉ phép năm",
                Reason = "Nghỉ dưỡng",
                Status = LeaveRequestStatus.Approved,
                DepartmentSnapshot = employees[3].Department?.Name ?? "",
                PositionSnapshot = employees[3].Position?.Name ?? ""
            },
            new()
            {
                EmployeeId = employees[4].Id,
                StartDate = DateTime.UtcNow.Date.AddDays(15),
                EndDate = DateTime.UtcNow.Date.AddDays(16),
                LeaveType = "Nghỉ không lương",
                Reason = "Công việc cá nhân",
                Status = LeaveRequestStatus.Rejected,
                DepartmentSnapshot = employees[4].Department?.Name ?? "",
                PositionSnapshot = employees[4].Position?.Name ?? ""
            }
        };

        context.LeaveRequests.AddRange(requests);
        await context.SaveChangesAsync();
    }

    private static async Task SeedContractsAsync(ApplicationDbContext context, List<Employee> employees)
    {
        if (await context.Contracts.AnyAsync())
        {
            return;
        }

        var contracts = new List<Contract>();
        for (var i = 0; i < employees.Count; i++)
        {
            var emp = employees[i];
            var baseSalary = emp.Position?.BaseSalary ?? 10_000_000m;
            contracts.Add(new Contract
            {
                EmployeeId = emp.Id,
                ContractCode = $"HD{emp.EmployeeCode}",
                StartDate = emp.HireDate,
                EndDate = emp.HireDate.AddYears(2),
                Salary = baseSalary,
                Status = SnapshotHelper.ResolveContractStatus(emp.HireDate.AddYears(2)),
                DepartmentSnapshot = emp.Department?.Name ?? "",
                PositionSnapshot = emp.Position?.Name ?? "",
                BaseSalarySnapshot = baseSalary
            });
        }

        if (contracts.Count > 1)
        {
            contracts[1].EndDate = DateTime.UtcNow.Date.AddDays(20);
            contracts[1].Status = ContractStatus.ExpiringSoon;
            contracts[^1].EndDate = DateTime.UtcNow.Date.AddDays(-10);
            contracts[^1].Status = ContractStatus.Expired;
        }

        context.Contracts.AddRange(contracts);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSalariesAsync(ApplicationDbContext context, List<Employee> employees)
    {
        if (await context.Salaries.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var salaries = new List<Salary>();

        foreach (var emp in employees)
        {
            var baseSalary = emp.Position?.BaseSalary ?? 10_000_000m;
            for (var monthOffset = 2; monthOffset >= 0; monthOffset--)
            {
                var date = now.AddMonths(-monthOffset);
                var allowance = 500_000m;
                var bonus = monthOffset == 0 ? 1_000_000m : 0m;
                var deduction = 200_000m;
                salaries.Add(new Salary
                {
                    EmployeeId = emp.Id,
                    Month = date.Month,
                    Year = date.Year,
                    BaseSalary = baseSalary,
                    Allowance = allowance,
                    Bonus = bonus,
                    Deduction = deduction,
                    TotalSalary = SnapshotHelper.CalculateTotalSalary(baseSalary, allowance, bonus, deduction),
                    DepartmentSnapshot = emp.Department?.Name ?? "",
                    PositionSnapshot = emp.Position?.Name ?? "",
                    BaseSalarySnapshot = baseSalary
                });
            }
        }

        context.Salaries.AddRange(salaries);
        await context.SaveChangesAsync();
    }

    private static async Task SeedEmployeeMediaAsync(ApplicationDbContext context, List<Employee> employees)
    {
        if (await context.EmployeeMedia.AnyAsync())
        {
            return;
        }

        var media = new List<EmployeeMedia>
        {
            new() { EmployeeId = employees[0].Id, FileName = "CCCD.pdf", FilePath = "/uploads/media/demo-cccd.pdf", MediaType = MediaType.Document },
            new() { EmployeeId = employees[0].Id, FileName = "Bang-cap.jpg", FilePath = "/uploads/media/demo-bangcap.jpg", MediaType = MediaType.Image },
            new() { EmployeeId = employees[1].Id, FileName = "Hop-dong.pdf", FilePath = "/uploads/media/demo-hopdong.pdf", MediaType = MediaType.Document },
            new() { EmployeeId = employees[2].Id, FileName = "Chung-chi.pdf", FilePath = "/uploads/media/demo-chungchi.pdf", MediaType = MediaType.Document },
            new() { EmployeeId = employees[3].Id, FileName = "Anh-the.jpg", FilePath = "/uploads/media/demo-anhthe.jpg", MediaType = MediaType.Image }
        };

        context.EmployeeMedia.AddRange(media);
        await context.SaveChangesAsync();
    }

    private static async Task SeedJoinRequestsAsync(
        ApplicationDbContext context,
        int companyId,
        UserManager<ApplicationUser> userManager)
    {
        if (await context.JoinCompanyRequests.AnyAsync())
        {
            return;
        }

        var pendingUser = new ApplicationUser
        {
            UserName = "pending@demo-hrms.local",
            Email = "pending@demo-hrms.local",
            EmailConfirmed = true,
            FullName = "Nguyễn Chờ Duyệt"
        };

        if (await userManager.FindByEmailAsync(pendingUser.Email!) is null)
        {
            await userManager.CreateAsync(pendingUser, "Pending@123");
            await userManager.AddToRoleAsync(pendingUser, RoleNames.Employee);
        }
        else
        {
            pendingUser = (await userManager.FindByEmailAsync(pendingUser.Email!))!;
        }

        context.JoinCompanyRequests.Add(new JoinCompanyRequest
        {
            UserId = pendingUser.Id,
            CompanyId = companyId,
            Status = JoinRequestStatus.Pending,
            Note = "Xin tham gia công ty demo"
        });

        await context.SaveChangesAsync();
    }
}