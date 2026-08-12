namespace Backend.Interfaces;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string email, string fullName, string code, CancellationToken cancellationToken, bool isPasswordReset = false);
    Task SendRequestDecisionAsync(string email, string fullName, int requestId, string serviceName, string decision, string? adminComment, CancellationToken cancellationToken);
    Task SendResultImageReadyAsync(string email, string fullName, int requestId, string serviceName, CancellationToken cancellationToken);
}
