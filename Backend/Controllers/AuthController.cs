using System.Security.Cryptography;
using System.Text;
using Backend.Authentication;
using Backend.Data;
using Backend.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender,
    IGoogleTokenVerifier googleTokenVerifier,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var fullName = dto.FullName.Trim();
        if (fullName.Length < 2) return BadRequest(new { message = "Full name must contain at least 2 characters." });
        if (dto.Password != dto.ConfirmPassword) return BadRequest(new { message = "Passwords do not match." });
        if (!dto.Password.Any(char.IsUpper) || !dto.Password.Any(char.IsLower) || !dto.Password.Any(char.IsDigit))
            return BadRequest(new { message = "Password must include an uppercase letter, a lowercase letter, and a number." });
        if (await db.Users.AnyAsync(item => item.Email == email, cancellationToken))
            return Conflict(new { message = "An account with this email already exists." });

        var now = DateTime.UtcNow;
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (pending is not null && pending.LastSentAt > now.AddSeconds(-30))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Please wait 30 seconds before requesting another code." });

        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
        var passwordOwner = new User { FullName = fullName, Email = email, PasswordHash = "", Role = UserRoles.Student };
        pending ??= new PendingRegistration { Email = email, FullName = fullName, PasswordHash = "", CodeHash = "" };
        pending.FullName = fullName;
        pending.PasswordHash = passwordHasher.HashPassword(passwordOwner, dto.Password);
        pending.CodeHash = HashCode(code);
        pending.ExpiresAt = now.AddMinutes(10);
        pending.LastSentAt = now;
        pending.FailedAttempts = 0;
        pending.GoogleSubject = null;
        if (pending.Id == 0) db.PendingRegistrations.Add(pending);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendVerificationCodeAsync(email, fullName, code, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send a registration verification email to {Email}", email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Verification email could not be sent. Check the Gmail configuration and try again." });
        }

        return Accepted(new { message = "A verification code was sent to your email.", email, expiresInMinutes = 10 });
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<LoginResponseDto>> VerifyEmail(VerifyEmailDto dto, CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (pending is null) return BadRequest(new { message = "No pending registration was found. Request a new code." });
        if (pending.ExpiresAt <= DateTime.UtcNow)
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = "The verification code expired. Request a new code." });
        }

        var suppliedHash = Convert.FromHexString(HashCode(dto.Code));
        var expectedHash = Convert.FromHexString(pending.CodeHash);
        if (!CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
        {
            pending.FailedAttempts++;
            if (pending.FailedAttempts >= 5) db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = pending.FailedAttempts >= 5
                ? "Too many incorrect attempts. Request a new code."
                : "Invalid verification code." });
        }

        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (pending.GoogleSubject is null && user is not null)
            return Conflict(new { message = "An account with this email already exists." });
        if (pending.GoogleSubject is not null && user is not null && user.Role != UserRoles.Student)
            return Forbid();
        if (pending.GoogleSubject is not null && user?.GoogleSubject is not null && user.GoogleSubject != pending.GoogleSubject)
            return Conflict(new { message = "This email is already connected to another Google account." });

        if (user is null)
        {
            user = new User { FullName = pending.FullName, Email = email, PasswordHash = pending.PasswordHash, Role = UserRoles.Student, GoogleSubject = pending.GoogleSubject };
            db.Users.Add(user);
        }
        else
        {
            user.GoogleSubject = pending.GoogleSubject;
        }
        db.PendingRegistrations.Remove(pending);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(new { message = "An account with this email already exists." }); }

        return Created(string.Empty, new LoginResponseDto(tokenService.CreateToken(user), new UserDto(user.Id, user.FullName, user.Email, user.Role)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(new LoginResponseDto(tokenService.CreateToken(user), new UserDto(user.Id, user.FullName, user.Email, user.Role)));
    }

    [HttpGet("google-config")]
    public ActionResult GetGoogleConfig() => Ok(new { clientId = configuration["Google:ClientId"] ?? "" });

    [HttpPost("google")]
    public async Task<ActionResult> GoogleLogin(GoogleLoginDto dto, CancellationToken cancellationToken)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Google Sign-In is not configured yet." });

        VerifiedGoogleUser payload;
        try
        {
            payload = await googleTokenVerifier.VerifyAsync(dto.Credential, clientId, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Google rejected an ID token during sign-in");
            var reason = exception.Message.Length > 180 ? exception.Message[..180] : exception.Message;
            return Unauthorized(new { message = $"Google could not verify this sign-in: {reason}" });
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email) || !payload.EmailVerified)
            return Unauthorized(new { message = "A verified Google email address is required." });

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(item => item.GoogleSubject == payload.Subject, cancellationToken)
            ?? await db.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is not null && user.Role != UserRoles.Student)
            return Forbid();
        if (user is not null && user.GoogleSubject is not null && user.GoogleSubject != payload.Subject)
            return Conflict(new { message = "This email is already connected to another Google account." });

        if (user is not null && user.GoogleSubject == payload.Subject)
        {
            return Ok(new LoginResponseDto(tokenService.CreateToken(user), new UserDto(user.Id, user.FullName, user.Email, user.Role)));
        }

        var now = DateTime.UtcNow;
        var pending = await db.PendingRegistrations.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (pending is not null && pending.LastSentAt > now.AddSeconds(-30))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Please wait 30 seconds before requesting another code." });
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
        pending ??= new PendingRegistration { Email = email, FullName = "", PasswordHash = "", CodeHash = "" };
        pending.FullName = string.IsNullOrWhiteSpace(payload.Name) ? email.Split('@')[0] : payload.Name.Trim();
        pending.PasswordHash = "GOOGLE_ACCOUNT_ONLY";
        pending.GoogleSubject = payload.Subject;
        pending.CodeHash = HashCode(code);
        pending.ExpiresAt = now.AddMinutes(10);
        pending.LastSentAt = now;
        pending.FailedAttempts = 0;
        if (pending.Id == 0) db.PendingRegistrations.Add(pending);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            await emailSender.SendVerificationCodeAsync(email, pending.FullName, code, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send a Google registration verification email to {Email}", email);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Verification email could not be sent. Please try again." });
        }
        return Accepted(new { requiresVerification = true, email, expiresInMinutes = 10 });
    }

    private string HashCode(string code)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(code)));
    }
}
