using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime WorkDate { get; set; }

    public TimeSpan? CheckIn { get; set; }
    public TimeSpan? CheckOut { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(150)]
    public string DepartmentSnapshot { get; set; } = string.Empty;

    [MaxLength(150)]
    public string PositionSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Kiểm tra tính hợp lệ của bản chấm công
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        // ❌ Lỗi 1.1: Kiểm tra CheckIn < CheckOut
        if (CheckIn.HasValue && CheckOut.HasValue && CheckIn >= CheckOut)
        {
            errorMessage = "Giờ vào phải nhỏ hơn giờ ra";
            return false;
        }

        // ❌ Lỗi 1.2: Không cho phép chấm công trong tương lai
        if (WorkDate.Date > DateTime.UtcNow.Date)
        {
            errorMessage = "Không thể chấm công cho ngày tương lai";
            return false;
        }

        // ❌ Lỗi 1.4: CheckIn/CheckOut hợp lý (trong giờ làm việc)
        if (CheckIn.HasValue && (CheckIn.Value.Hours < 5 || CheckIn.Value.Hours > 23))
        {
            errorMessage = "Giờ vào không hợp lệ (phải từ 05:00 - 23:59)";
            return false;
        }

        if (CheckOut.HasValue && (CheckOut.Value.Hours < 5 || CheckOut.Value.Hours > 23))
        {
            errorMessage = "Giờ ra không hợp lệ (phải từ 05:00 - 23:59)";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tự động tính toán trạng thái dựa trên CheckIn/CheckOut (Lỗi 1.4 fix)
    /// </summary>
    public void CalculateStatus()
    {
        if (!CheckIn.HasValue)
        {
            Status = AttendanceStatus.Absent;
            return;
        }

        // Chuẩn vào: 08:00, cho phép muộn đến 08:15
        var standardCheckIn = new TimeSpan(8, 0, 0);
        var lateThreshold = new TimeSpan(8, 15, 0);

        if (CheckIn.Value > lateThreshold)
        {
            Status = AttendanceStatus.Late;
        }
        else
        {
            Status = AttendanceStatus.Present;
        }
    }
}
