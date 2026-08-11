using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record LoginRequestDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record RegisterRequestDto(
    [Required, MaxLength(100)] string FullName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required] string ConfirmPassword);

public record VerifyEmailDto(
    [Required, EmailAddress] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Code);
public record GoogleLoginDto([Required] string Credential);

public record UserDto(int Id, string FullName, string Email, string Role);
public record LoginResponseDto(string Token, UserDto User);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(100)] string NewPassword,
    [Required] string ConfirmPassword);

public record ForgotPasswordDto([Required, EmailAddress, MaxLength(200)] string Email);

public record ResetPasswordDto(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Code,
    [Required, MinLength(8), MaxLength(100)] string NewPassword,
    [Required] string ConfirmPassword);
