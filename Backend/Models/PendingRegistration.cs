using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class PendingRegistration
{
    public int Id { get; set; }
    [MaxLength(100)] public required string FullName { get; set; }
    [MaxLength(200)] public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    [MaxLength(64)] public required string CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public int FailedAttempts { get; set; }
    [MaxLength(100)] public string? GoogleSubject { get; set; }
}
