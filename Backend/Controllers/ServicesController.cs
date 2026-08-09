using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/services")]
[Authorize(Roles = UserRoles.Student)]
public class ServicesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll() =>
        Ok(await db.Services.OrderBy(service => service.Name).Select(service => new ServiceDto(service.Id, service.Name, service.Description)).ToListAsync());
}
