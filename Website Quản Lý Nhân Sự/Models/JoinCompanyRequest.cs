using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class JoinCompanyRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;

    [MaxLength(500)]
    public string? Note { get; set; }
}
