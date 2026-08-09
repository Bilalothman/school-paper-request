namespace Backend.Interfaces;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string fullName, string code, CancellationToken cancellationToken);
}
