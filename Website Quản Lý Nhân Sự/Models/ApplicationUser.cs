using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Website_Quản_Lý_Nhân_Sự.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public Employee? Employee { get; set; }
    public ICollection<JoinCompanyRequest> JoinRequests { get; set; } = [];
}
