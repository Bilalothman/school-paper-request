using System.Diagnostics;
using System.Text.Json;
using Backend.Interfaces;

namespace Backend.Services;

public class GmailEmailSender(IConfiguration configuration, IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendVerificationCodeAsync(string email, string fullName, string code, CancellationToken cancellationToken, bool isPasswordReset = false)
    {
        var username = configuration["Gmail:Username"];
        var appPassword = configuration["Gmail:AppPassword"];
        var fromName = configuration["Gmail:FromName"] ?? "School Requests";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(appPassword))
            throw new InvalidOperationException("Gmail verification is not configured.");

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
        var payload = JsonSerializer.Serialize(new { username, appPassword, fromName, email, fullName, code, isPasswordReset });
        await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken);
        process.StandardInput.Close();
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Gmail rejected the email request." : error.Trim());
    }
}
