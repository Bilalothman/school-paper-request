using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class PaperRequest
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int ServiceId { get; set; }
    public PaperService Service { get; set; } = null!;
    [MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string Grade { get; set; } = string.Empty;
    [MaxLength(300)] public string Address { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Note { get; set; }
    [MaxLength(20)] public string Status { get; set; } = RequestStatuses.Submitted;
    [MaxLength(1000)] public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? CamundaProcessInstanceId { get; set; }
}

public static class RequestStatuses
{
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
