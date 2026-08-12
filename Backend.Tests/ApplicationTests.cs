using Backend.Authentication;
using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Backend.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

namespace Backend.Tests;

public class ApplicationTests
{
    [Fact]
    public async Task Register_ThenVerify_CreatesHashedStudentAccount()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<User>();
        var emailSender = new FakeEmailSender();
        var controller = CreateAuthController(db, hasher, emailSender);

        var result = await controller.Register(new RegisterRequestDto("New Student", "NEW@school.com", "Student123!", "Student123!"), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.Empty(db.Users);
        Assert.Single(db.PendingRegistrations);
        var verified = await controller.VerifyEmail(new VerifyEmailDto("new@school.com", emailSender.Code!), CancellationToken.None);
        var response = Assert.IsType<CreatedResult>(verified.Result);
        var login = Assert.IsType<LoginResponseDto>(response.Value);
        var saved = Assert.Single(db.Users);
        Assert.Equal(UserRoles.Student, saved.Role);
        Assert.Equal("new@school.com", saved.Email);
        Assert.NotEqual("Student123!", saved.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(saved, saved.PasswordHash, "Student123!"));
        Assert.Equal(UserRoles.Student, login.User.Role);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        await using var db = CreateDb();
        db.Users.Add(new User { FullName = "Existing", Email = "student@school.com", PasswordHash = "hash", Role = UserRoles.Student });
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db, new PasswordHasher<User>());

        var result = await controller.Register(new RegisterRequestDto("Another Student", "STUDENT@school.com", "Student123!", "Student123!"), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAndRole()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { FullName = "Student", Email = "student@school.com", PasswordHash = "", Role = UserRoles.Student };
        user.PasswordHash = hasher.HashPassword(user, "Student123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-key-that-is-longer-than-thirty-two-bytes",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        }).Build();
        var controller = new AuthController(db, hasher, new TokenService(configuration), new FakeEmailSender(), new FakeGoogleTokenVerifier(), configuration, NullLogger<AuthController>.Instance);

        var result = await controller.Login(new LoginRequestDto("student@school.com", "Student123!"));

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var login = Assert.IsType<LoginResponseDto>(response.Value);
        Assert.Equal(UserRoles.Student, login.User.Role);
        Assert.NotEmpty(login.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { FullName = "Student", Email = "student@school.com", PasswordHash = "", Role = UserRoles.Student };
        user.PasswordHash = hasher.HashPassword(user, "Student123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = CreateAuthController(db, hasher);

        var result = await controller.Login(new LoginRequestDto("student@school.com", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GoogleSignIn_WithUnknownAccount_DoesNotCreateRegistration()
    {
        await using var db = CreateDb();
        var sender = new FakeEmailSender();
        var controller = CreateAuthController(db, new PasswordHasher<User>(), sender);

        var result = await controller.GoogleLogin(new GoogleLoginDto("credential"), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.PendingRegistrations);
        Assert.Null(sender.Code);
    }

    [Fact]
    public async Task GoogleRegistration_WithUnknownAccount_StartsVerification()
    {
        await using var db = CreateDb();
        var sender = new FakeEmailSender();
        var controller = CreateAuthController(db, new PasswordHasher<User>(), sender);

        var result = await controller.GoogleLogin(new GoogleLoginDto("credential", AllowRegistration: true), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.Single(db.PendingRegistrations);
        Assert.NotNull(sender.Code);
    }

    [Fact]
    public async Task ForgotPassword_WithValidCode_ChangesPassword()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<User>();
        var user = new User { FullName = "Student", Email = "student@school.com", PasswordHash = "", Role = UserRoles.Student };
        user.PasswordHash = hasher.HashPassword(user, "Student123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();
        var controller = CreateAuthController(db, hasher, sender);

        var requested = await controller.ForgotPassword(new ForgotPasswordDto(user.Email), CancellationToken.None);
        var reset = await controller.ResetPassword(
            new ResetPasswordDto(user.Email, sender.Code!, "Changed123!", "Changed123!"), CancellationToken.None);
        var login = await controller.Login(new LoginRequestDto(user.Email, "Changed123!"));

        Assert.IsType<AcceptedResult>(requested);
        Assert.IsType<OkObjectResult>(reset);
        Assert.IsType<OkObjectResult>(login.Result);
        Assert.Empty(db.PasswordResets);
    }

    [Fact]
    public async Task Admin_CanApproveSubmittedRequest_OnlyOnce()
    {
        await using var db = CreateDb();
        db.Requests.Add(new PaperRequest { Id = 7, StudentId = 1, ServiceId = 1, Status = RequestStatuses.Submitted, CamundaProcessInstanceId = "process-7" });
        await db.SaveChangesAsync();
        var workflow = new FakeWorkflowService();
        var controller = new AdminRequestsController(db, workflow, NullLogger<AdminRequestsController>.Instance, TestEnvironment.Instance);

        var first = await controller.Approve(7, new AdminDecisionDto("Ready tomorrow"), CancellationToken.None);
        var second = await controller.Approve(7, new AdminDecisionDto(null), CancellationToken.None);

        Assert.IsType<OkObjectResult>(first);
        Assert.IsType<ConflictObjectResult>(second);
        Assert.Equal(RequestStatuses.Approved, (await db.Requests.FindAsync(7))!.Status);
        Assert.Equal("Ready tomorrow", (await db.Requests.FindAsync(7))!.AdminComment);
        Assert.Null((await db.Requests.FindAsync(7))!.ResultImage);
        Assert.Equal(1, workflow.CompletedCount);
    }

    [Fact]
    public async Task Admin_CannotProcessMissingRequest()
    {
        await using var db = CreateDb();
        var controller = new AdminRequestsController(db, new FakeWorkflowService(), NullLogger<AdminRequestsController>.Instance, TestEnvironment.Instance);

        var result = await controller.Reject(99, new AdminDecisionDto(null), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Admin_CanUploadResultImage_OnlyAfterApproval()
    {
        await using var db = CreateDb();
        db.Requests.Add(new PaperRequest { Id = 8, StudentId = 1, ServiceId = 1, Status = RequestStatuses.Submitted, CamundaProcessInstanceId = "process-8" });
        await db.SaveChangesAsync();
        var controller = new AdminRequestsController(db, new FakeWorkflowService(), NullLogger<AdminRequestsController>.Instance, TestEnvironment.Instance);
        var bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47 };
        var upload = new ResultImageUploadDto
        {
            Image = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "result.png") { Headers = new HeaderDictionary(), ContentType = "image/png" }
        };

        var beforeApproval = await controller.UploadResultImage(8, upload, CancellationToken.None);
        await controller.Approve(8, new AdminDecisionDto(null), CancellationToken.None);
        var afterApproval = await controller.UploadResultImage(8, upload, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(beforeApproval);
        Assert.IsType<OkObjectResult>(afterApproval);
        var saved = (await db.Requests.FindAsync(8))!;
        Assert.Null(saved.ResultImage);
        Assert.NotNull(saved.ResultImageFileName);
        Assert.True(File.Exists(Path.Combine(TestEnvironment.Instance.ContentRootPath, "App_Data", "result-images", saved.ResultImageFileName)));
        File.Delete(Path.Combine(TestEnvironment.Instance.ContentRootPath, "App_Data", "result-images", saved.ResultImageFileName));
    }

    [Fact]
    public async Task Admin_CanAddAndRemoveUnusedService()
    {
        await using var db = CreateDb();
        var controller = new AdminServicesController(db);

        var created = await controller.Create(new CreateServiceDto("Graduation Certificate", "Official graduation document"));
        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        var service = Assert.IsType<ServiceDto>(createdResult.Value);
        Assert.Single(db.Services);

        var deleted = await controller.Delete(service.Id);
        Assert.IsType<NoContentResult>(deleted);
        Assert.Empty(db.Services);
    }

    [Fact]
    public async Task Admin_CannotRemoveServiceUsedByRequest()
    {
        await using var db = CreateDb();
        db.Services.Add(new PaperService { Id = 4, Name = "Transcript", Description = "Grades" });
        db.Requests.Add(new PaperRequest { StudentId = 1, ServiceId = 4, Status = RequestStatuses.Submitted });
        await db.SaveChangesAsync();
        var controller = new AdminServicesController(db);

        var result = await controller.Delete(4);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Services);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateAuthController(AppDbContext db, IPasswordHasher<User> hasher, FakeEmailSender? sender = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-only-key-that-is-longer-than-thirty-two-bytes",
            ["Google:ClientId"] = "test-client-id.apps.googleusercontent.com"
        }).Build();
        return new AuthController(db, hasher, new FakeTokenService(), sender ?? new FakeEmailSender(), new FakeGoogleTokenVerifier(), configuration, NullLogger<AuthController>.Instance);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public string? Code { get; private set; }
        public Task SendVerificationCodeAsync(string email, string fullName, string code, CancellationToken cancellationToken, bool isPasswordReset = false)
        {
            Code = code;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<VerifiedGoogleUser> VerifyAsync(string credential, string clientId, CancellationToken cancellationToken) =>
            Task.FromResult(new VerifiedGoogleUser("google-subject", "google@gmail.com", true, "Google Student"));
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string CreateToken(User user) => "token";
    }

    private sealed class FakeWorkflowService : IWorkflowService
    {
        public int CompletedCount { get; private set; }
        public Task<string> StartRequestProcessAsync(int requestId, CancellationToken cancellationToken) => Task.FromResult($"process-{requestId}");
        public Task CompleteReviewTaskAsync(string processInstanceId, string decision, CancellationToken cancellationToken) { CompletedCount++; return Task.CompletedTask; }
        public Task CancelProcessAsync(string processInstanceId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public static TestEnvironment Instance { get; } = new();
        public string ApplicationName { get; set; } = "Backend.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
