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
public class AdminRequestsController(AppDbContext db, IWorkflowService workflow, ILogger<AdminRequestsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminRequestDto>>> GetAll() => Ok(await db.Requests
        .OrderByDescending(request => request.CreatedAt)
        .Select(request => new AdminRequestDto(request.Id, request.Student.FullName, request.Student.Email, request.ServiceId, request.Service.Name, request.PhoneNumber, request.Grade, request.Address, request.Note, request.Status, request.AdminComment, request.CreatedAt))
        .ToListAsync());

    [HttpPost("{id:int}/approve")]
    public Task<ActionResult> Approve(int id, AdminDecisionDto dto, CancellationToken cancellationToken) => Decide(id, RequestStatuses.Approved, dto, cancellationToken);

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
