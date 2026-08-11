using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(AppDbContext db, IPasswordHasher<User> passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([CurrentUserId()], cancellationToken);
        if (user is null) return NotFound(new { message = "User account was not found." });
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role));
    }

    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([CurrentUserId()], cancellationToken);
        if (user is null) return NotFound(new { message = "User account was not found." });

        if (user.PasswordHash == "GOOGLE_ACCOUNT_ONLY")
            return BadRequest(new { message = "Password changes are not available for Google-only accounts." });
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "The current password is incorrect." });
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new { message = "New passwords do not match." });
        if (!dto.NewPassword.Any(char.IsUpper) || !dto.NewPassword.Any(char.IsLower) || !dto.NewPassword.Any(char.IsDigit))
            return BadRequest(new { message = "Password must include an uppercase letter, a lowercase letter, and a number." });
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.NewPassword) != PasswordVerificationResult.Failed)
            return BadRequest(new { message = "The new password must be different from the current password." });

        user.PasswordHash = passwordHasher.HashPassword(user, dto.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Your password was changed successfully." });
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
