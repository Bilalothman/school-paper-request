using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    public int Id { get; set; }
    [MaxLength(100)] public required string FullName { get; set; }
    [MaxLength(200)] public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    [MaxLength(20)] public required string Role { get; set; }
    [MaxLength(100)] public string? GoogleSubject { get; set; }
    public ICollection<PaperRequest> Requests { get; set; } = [];
}

public static class UserRoles
{
    public const string Student = "Student";
    public const string Admin = "Admin";
}
