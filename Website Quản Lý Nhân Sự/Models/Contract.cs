using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Contract
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required, MaxLength(50)]
    public string ContractCode { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Lương phải >= 0")]
    public decimal Salary { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Active;

    [MaxLength(150)]
    public string DepartmentSnapshot { get; set; } = string.Empty;

    [MaxLength(150)]
    public string PositionSnapshot { get; set; } = string.Empty;

    [Range(0, 999_999_999)]
    public decimal BaseSalarySnapshot { get; set; }

    /// <summary>
    /// Kiểm tra tính hợp lệ của hợp đồng (Lỗi 3.1)
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        // ❌ Lỗi 3.1: Kiểm tra EndDate > StartDate
        if (EndDate <= StartDate)
        {
            errorMessage = "Ngày kết thúc phải sau ngày bắt đầu";
            return false;
        }

        // Hợp đồng tối thiểu 1 tháng
        var months = (EndDate.Year - StartDate.Year) * 12 + (EndDate.Month - StartDate.Month);
        if (months < 1)
        {
            errorMessage = "Hợp đồng phải có thời hạn tối thiểu 1 tháng";
            return false;
        }

        // Lương phải > 0
        if (Salary <= 0)
        {
            errorMessage = "Lương phải lớn hơn 0";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tự động cập nhật trạng thái hợp đồng dựa trên ngày hết hạn (Lỗi 3.3)
    /// </summary>
    public void UpdateStatus()
    {
        var today = DateTime.UtcNow.Date;

        if (EndDate.Date < today)
        {
            Status = ContractStatus.Expired;
        }
        else if ((EndDate.Date - today).TotalDays <= 30)
        {
            Status = ContractStatus.ExpiringSoon;
        }
        else
        {
            Status = ContractStatus.Active;
        }
    }
}
