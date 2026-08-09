using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record ServiceDto(int Id, string Name, string Description);
public record CreateServiceDto(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(2), MaxLength(500)] string Description);
public record CreateRequestDto([Required] int ServiceId, [MaxLength(1000)] string? Note);
public record AdminDecisionDto([MaxLength(1000)] string? Comment);
public record RequestDto(int Id, int ServiceId, string Service, string? Note, string Status, string? AdminComment, DateTime CreatedAt);
public record AdminRequestDto(int Id, string StudentName, string StudentEmail, int ServiceId, string Service, string? Note, string Status, string? AdminComment, DateTime CreatedAt);
