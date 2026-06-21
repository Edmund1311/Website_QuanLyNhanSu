using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class EmployeeMedia
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public MediaType MediaType { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
