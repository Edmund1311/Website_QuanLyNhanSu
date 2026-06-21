using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [MaxLength(100)]
    public string LeaveType { get; set; } = "Nghỉ phép";

    [MaxLength(500)]
    public string? Reason { get; set; }

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(150)]
    public string DepartmentSnapshot { get; set; } = string.Empty;

    [MaxLength(150)]
    public string PositionSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Kiểm tra tính hợp lệ của đơn xin nghỉ (Lỗi 2.1, 2.2, 2.3)
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        // ❌ Lỗi 2.1: Kiểm tra EndDate >= StartDate
        if (EndDate < StartDate)
        {
            errorMessage = "Ngày kết thúc phải >= ngày bắt đầu";
            return false;
        }

        // Không cho xin nghỉ quá 30 ngày liên tục
        var days = (EndDate.Date - StartDate.Date).TotalDays + 1;
        if (days > 30)
        {
            errorMessage = "Không thể xin nghỉ quá 30 ngày liên tục";
            return false;
        }

        // Không cho xin nghỉ trong quá khứ quá 3 ngày
        if (StartDate.Date < DateTime.UtcNow.Date.AddDays(-3))
        {
            errorMessage = "Không thể xin nghỉ cho ngày quá xa trong quá khứ";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tính số ngày nghỉ không tính thứ 7, chủ nhật (Lỗi 2.3)
    /// </summary>
    public int GetBusinessDays()
    {
        int count = 0;
        for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Kiểm tra xung đột với các đơn khác cùng nhân viên (Lỗi 2.2)
    /// Được gọi từ Service
    /// </summary>
    public bool HasTimeConflict(LeaveRequest other)
    {
        if (other.Id == this.Id) return false;
        if (other.Status == LeaveRequestStatus.Rejected) return false;

        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }
}
