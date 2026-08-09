namespace Backend.Interfaces;

public record VerifiedGoogleUser(string Subject, string Email, bool EmailVerified, string Name);

public interface IGoogleTokenVerifier
{
    Task<VerifiedGoogleUser> VerifyAsync(string credential, string clientId, CancellationToken cancellationToken);
}
