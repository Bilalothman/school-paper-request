using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/services")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminServicesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll() =>
        Ok(await db.Services.OrderBy(item => item.Name)
            .Select(item => new ServiceDto(item.Id, item.Name, item.Description)).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(CreateServiceDto dto)
    {
        var name = dto.Name.Trim();
        var description = dto.Description.Trim();
        if (await db.Services.AnyAsync(item => item.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "A service with this name already exists." });

        var service = new PaperService { Name = name, Description = description };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return Created(string.Empty, new ServiceDto(service.Id, service.Name, service.Description));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var service = await db.Services.FindAsync(id);
        if (service is null) return NotFound(new { message = "Service was not found." });
        if (await db.Requests.AnyAsync(request => request.ServiceId == id))
            return Conflict(new { message = "This service cannot be removed because students have already requested it." });

        db.Services.Remove(service);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
