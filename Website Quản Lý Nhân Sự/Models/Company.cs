using System.ComponentModel.DataAnnotations;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Company
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Position> Positions { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<JoinCompanyRequest> JoinRequests { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
}
