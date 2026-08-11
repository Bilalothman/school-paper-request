using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class PasswordReset
{
    public int Id { get; set; }
    [MaxLength(200)] public required string Email { get; set; }
    [MaxLength(64)] public required string CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public int FailedAttempts { get; set; }
}
