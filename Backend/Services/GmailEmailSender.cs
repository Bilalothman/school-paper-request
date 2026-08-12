using System.Diagnostics;
using System.Text.Json;
using Backend.Interfaces;

namespace Backend.Services;

public class GmailEmailSender(IConfiguration configuration, IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendVerificationCodeAsync(string email, string fullName, string code, CancellationToken cancellationToken, bool isPasswordReset = false)
    {
        await SendAsync(new { email, fullName, code, isPasswordReset, notificationType = "verification" }, cancellationToken);
    }

    public async Task SendRequestDecisionAsync(string email, string fullName, int requestId, string serviceName, string decision, string? adminComment, CancellationToken cancellationToken)
    {
        await SendAsync(new { email, fullName, requestId, serviceName, decision, adminComment, notificationType = "requestDecision" }, cancellationToken);
    }

    public async Task SendResultImageReadyAsync(string email, string fullName, int requestId, string serviceName, CancellationToken cancellationToken)
    {
        await SendAsync(new { email, fullName, requestId, serviceName, notificationType = "resultImageReady" }, cancellationToken);
    }

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        var username = configuration["Gmail:Username"];
        var appPassword = configuration["Gmail:AppPassword"];
        var fromName = configuration["Gmail:FromName"] ?? "School Requests";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(appPassword))
            throw new InvalidOperationException("Gmail is not configured.");

        var scriptPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "scripts", "send-verification-email.mjs"));
        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(scriptPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("The email process could not be started.");
        var payload = JsonSerializer.Serialize(new { username, appPassword, fromName, message });
        await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken);
        process.StandardInput.Close();
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Gmail rejected the email request." : error.Trim());
    }
}
