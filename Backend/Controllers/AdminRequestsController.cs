using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/requests")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminRequestsController(AppDbContext db, IWorkflowService workflow, ILogger<AdminRequestsController> logger, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminRequestDto>>> GetAll() => Ok(await db.Requests
        .OrderByDescending(request => request.CreatedAt)
        .Select(request => new AdminRequestDto(request.Id, request.Student.FullName, request.Student.Email, request.ServiceId, request.Service.Name, request.PhoneNumber, request.Grade, request.Address, request.Note, request.Status, request.AdminComment, request.ResultImageFileName != null, request.CreatedAt))
        .ToListAsync());

    [HttpPost("{id:int}/approve")]
    public Task<ActionResult> Approve(int id, AdminDecisionDto dto, CancellationToken cancellationToken) =>
        Decide(id, RequestStatuses.Approved, dto, cancellationToken);

    [HttpPost("{id:int}/result-image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadResultImage(int id, [FromForm] ResultImageUploadDto dto, CancellationToken cancellationToken)
    {
        var request = await db.Requests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound(new { message = "Request not found." });
        if (request.Status != RequestStatuses.Approved) return Conflict(new { message = "A result image can only be added after the request is approved." });
        if (dto.Image is null || dto.Image.Length == 0) return BadRequest(new { message = "Please select a result image to upload." });
        if (dto.Image.Length > 5 * 1024 * 1024) return BadRequest(new { message = "The result image must be 5 MB or smaller." });
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(dto.Image.ContentType)) return BadRequest(new { message = "Only JPG, PNG, and WebP images are allowed." });

        var uploadDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "result-images");
        Directory.CreateDirectory(uploadDirectory);
        var extension = dto.Image.ContentType.ToLowerInvariant() switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".webp" };
        var storedFileName = $"request-{id}-{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(uploadDirectory, storedFileName);
        await using (var stream = System.IO.File.Create(storedPath))
            await dto.Image.CopyToAsync(stream, cancellationToken);

        var previousFileName = request.ResultImageFileName;
        request.ResultImage = null;
        request.ResultImageFileName = storedFileName;
        request.ResultImageContentType = dto.Image.ContentType;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { System.IO.File.Delete(storedPath); throw; }
        if (!string.IsNullOrWhiteSpace(previousFileName))
        {
            var previousPath = Path.Combine(uploadDirectory, Path.GetFileName(previousFileName));
            if (!string.Equals(previousPath, storedPath, StringComparison.OrdinalIgnoreCase)) System.IO.File.Delete(previousPath);
        }
        return Ok(new { message = "Result image added successfully." });
    }

    [HttpPost("{id:int}/reject")]
    public Task<ActionResult> Reject(int id, AdminDecisionDto dto, CancellationToken cancellationToken) => Decide(id, RequestStatuses.Rejected, dto, cancellationToken);

    private async Task<ActionResult> Decide(int id, string decision, AdminDecisionDto dto, CancellationToken cancellationToken)
    {
        var request = await db.Requests.FindAsync([id], cancellationToken);
        if (request is null) return NotFound(new { message = "Request not found." });
        if (request.Status != RequestStatuses.Submitted) return Conflict(new { message = "Request has already been processed." });
        if (string.IsNullOrWhiteSpace(request.CamundaProcessInstanceId)) return Conflict(new { message = "Request has no active workflow." });

        try
        {
            await workflow.CompleteReviewTaskAsync(request.CamundaProcessInstanceId, decision, cancellationToken);
        }
        catch (Exception ex) when (ex is WorkflowException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not complete workflow for request {RequestId}.", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The workflow service is unavailable. The request was not changed." });
        }

        request.Status = decision;
        request.AdminComment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Request {decision.ToLowerInvariant()} successfully." });
    }
}
