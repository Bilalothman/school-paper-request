using System.Diagnostics;
using System.Text.Json;
using Backend.Interfaces;

namespace Backend.Services;

public class GoogleTokenVerifier(IWebHostEnvironment environment) : IGoogleTokenVerifier
{
    public async Task<VerifiedGoogleUser> VerifyAsync(string credential, string clientId, CancellationToken cancellationToken)
    {
        var scriptPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "scripts", "verify-google-token.mjs"));
        var startInfo = new ProcessStartInfo
        {
            FileName = "node", WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(scriptPath);
        foreach (var variable in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "http_proxy", "https_proxy", "all_proxy" })
            startInfo.Environment.Remove(variable);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Google verification could not start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { credential, clientId }).AsMemory(), cancellationToken);
        process.StandardInput.Close();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask; var error = await errorTask;
        if (process.ExitCode != 0) throw new InvalidDataException(string.IsNullOrWhiteSpace(error) ? "Invalid Google credential." : error.Trim());
        return JsonSerializer.Deserialize<VerifiedGoogleUser>(output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Google returned an invalid profile.");
    }
}
