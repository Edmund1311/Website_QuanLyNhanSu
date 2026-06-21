using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Employee
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    [Required, MaxLength(30)]
    [RegularExpression(@"^[A-Z]{0,2}\d{3,4}$", 
        ErrorMessage = "Mã nhân viên phải theo định dạng: NV001, EMP0001, etc.")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.Other;

    [MaxLength(300)]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? AvatarPath { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow.Date;

    public WorkStatus WorkStatus { get; set; } = WorkStatus.Active;

    public bool IsDeleted { get; set; }

    public ICollection<EmployeeMedia> MediaFiles { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Salary> Salaries { get; set; } = [];

    /// <summary>
    /// Kiểm tra tính hợp lệ của nhân viên (Lỗi 7.2, 7.3)
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        // ❌ Lỗi 7.2: Kiểm tra format EmployeeCode
        if (!Regex.IsMatch(EmployeeCode, @"^[A-Z]{0,2}\d{3,4}$"))
        {
            errorMessage = "Mã nhân viên phải theo định dạng: NV001, EMP0001";
            return false;
        }

        // ❌ Lỗi 7.3: Kiểm tra số điện thoại
        if (!string.IsNullOrWhiteSpace(Phone))
        {
            if (!Regex.IsMatch(Phone, @"^0\d{9}$|^\+84\d{9}$"))
            {
                errorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10 chữ số";
                return false;
            }
        }

        // Kiểm tra DateOfBirth
        if (DateOfBirth.HasValue)
        {
            if (DateOfBirth.Value > DateTime.UtcNow.Date)
            {
                errorMessage = "Ngày sinh không thể trong tương lai";
                return false;
            }

            var age = DateTime.UtcNow.Year - DateOfBirth.Value.Year;
            if (age < 18)
            {
                errorMessage = "Nhân viên phải >= 18 tuổi";
                return false;
            }

            if (age > 100)
            {
                errorMessage = "Ngày sinh không hợp lệ";
                return false;
            }
        }

        // Kiểm tra HireDate
        if (HireDate > DateTime.UtcNow.Date)
        {
            errorMessage = "Ngày tuyển dụng không thể trong tương lai";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tính số năm làm việc
    /// </summary>
    public int GetYearsOfService()
    {
        var today = DateTime.UtcNow.Date;
        var years = today.Year - HireDate.Year;

        if (HireDate.Date > today.AddYears(-years))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    /// <summary>
    /// Lấy số ngày phép năm dựa trên thâm niên
    /// </summary>
    public int GetAnnualLeaveAllowance()
    {
        var years = GetYearsOfService();

        if (years < 1) return 12;
        if (years < 5) return 12;
        if (years < 10) return 14;
        return 15; // 10+ năm
    }
}
