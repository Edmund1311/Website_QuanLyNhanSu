using System.ComponentModel.DataAnnotations;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Salary
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Range(1, 12, ErrorMessage = "Tháng phải từ 1 - 12")]
    public int Month { get; set; }

    [Range(2000, 2100, ErrorMessage = "Năm phải từ 2000 - 2100")]
    public int Year { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Lương cơ bản phải >= 0")]
    public decimal BaseSalary { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Phụ cấp phải >= 0")]
    public decimal Allowance { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Thưởng phải >= 0")]
    public decimal Bonus { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Khấu trừ phải >= 0")]
    public decimal Deduction { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Tổng lương phải >= 0")]
    public decimal TotalSalary { get; set; }

    [MaxLength(150)]
    public string DepartmentSnapshot { get; set; } = string.Empty;

    [MaxLength(150)]
    public string PositionSnapshot { get; set; } = string.Empty;

    [Range(0, 999_999_999)]
    public decimal BaseSalarySnapshot { get; set; }

    /// <summary>
    /// Kiểm tra tính hợp lệ của bảng lương (Lỗi 4.2, 4.3, 4.4)
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        // ❌ Lỗi 4.3: Kiểm tra TotalSalary không âm
        var calculatedTotal = BaseSalary + Allowance + Bonus - Deduction;
        if (calculatedTotal < 0)
        {
            errorMessage = "Tổng lương không thể âm. Khấu trừ quá cao.";
            return false;
        }

        // ❌ Lỗi 4.4: Kiểm tra lương cơ bản hợp lý
        if (BaseSalary == 0)
        {
            errorMessage = "Lương cơ bản phải lớn hơn 0";
            return false;
        }

        if (BaseSalary > 500_000_000m)
        {
            errorMessage = "Lương cơ bản quá cao (vượt 500 triệu)";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tính toán lại tổng lương từ các thành phần
    /// </summary>
    public void CalculateTotalSalary()
    {
        TotalSalary = BaseSalary + Allowance + Bonus - Deduction;

        // Đảm bảo không âm
        if (TotalSalary < 0)
        {
            TotalSalary = 0;
        }
    }
}
