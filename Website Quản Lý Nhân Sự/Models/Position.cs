using System.ComponentModel.DataAnnotations;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class Position
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal BaseSalary { get; set; }

    public bool IsDeleted { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = [];
}
