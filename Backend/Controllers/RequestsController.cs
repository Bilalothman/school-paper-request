using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize(Roles = UserRoles.Student)]
public class RequestsController(AppDbContext db, IWorkflowService workflow, ILogger<RequestsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(CreateRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Grade) || string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest(new { message = "Phone number, grade, and address are required." });

        var service = await db.Services.FindAsync([dto.ServiceId], cancellationToken);
        if (service is null) return BadRequest(new { message = "The selected service does not exist." });

        var request = new PaperRequest
        {
            StudentId = CurrentUserId(), ServiceId = service.Id,
            PhoneNumber = dto.PhoneNumber.Trim(), Grade = dto.Grade.Trim(), Address = dto.Address.Trim(),
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Requests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        string? processId = null;
        try
        {
            processId = await workflow.StartRequestProcessAsync(request.Id, cancellationToken);
            request.CamundaProcessInstanceId = processId;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is WorkflowException or HttpRequestException or TaskCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            if (processId is not null) await workflow.CancelProcessAsync(processId, CancellationToken.None);
            logger.LogError(ex, "Request workflow could not be started for request {RequestId}.", request.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The workflow service is unavailable. Your request was not submitted." });
        }

        return Created(string.Empty, ToDto(request, service.Name));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<RequestDto>>> Mine() => Ok(await db.Requests
        .Where(request => request.StudentId == CurrentUserId())
        .OrderByDescending(request => request.CreatedAt)
        .Select(request => new RequestDto(request.Id, request.ServiceId, request.Service.Name, request.PhoneNumber, request.Grade, request.Address, request.Note, request.Status, request.AdminComment, request.CreatedAt))
        .ToListAsync());

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static RequestDto ToDto(PaperRequest request, string service) => new(request.Id, request.ServiceId, service, request.PhoneNumber, request.Grade, request.Address, request.Note, request.Status, request.AdminComment, request.CreatedAt);
}
