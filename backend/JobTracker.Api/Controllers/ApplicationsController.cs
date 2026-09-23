using System.Security.Claims;
using JobTracker.Api.Dtos;
using JobTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // every endpoint in this controller requires a valid JWT
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationService _service;

    public ApplicationsController(IJobApplicationService service)
    {
        _service = service;
    }

    // Pulls the user ID out of the JWT's claims (set in TokenService).
    // This is how we know WHO is making the request, without a
    // separate session store.
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No user id claim on token."));

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<JobApplicationDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeArchived = false)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await _service.GetAllForUserAsync(CurrentUserId, page, pageSize, includeArchived));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobApplicationDto>> GetById(int id)
    {
        var app = await _service.GetByIdAsync(CurrentUserId, id);
        return app is null ? NotFound() : Ok(app);
    }

    [HttpPost]
    public async Task<ActionResult<JobApplicationDto>> Create(CreateJobApplicationDto dto)
    {
        var created = await _service.CreateAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobApplicationDto>> Update(int id, UpdateJobApplicationDto dto)
    {
        var updated = await _service.UpdateAsync(CurrentUserId, id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    // The everyday "remove this" action — soft delete, reversible.
    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var ok = await _service.ArchiveAsync(CurrentUserId, id);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var ok = await _service.RestoreAsync(CurrentUserId, id);
        return ok ? NoContent() : NotFound();
    }

    // Permanent delete — only succeeds on an application that's already
    // archived, enforced one layer down in the service.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(CurrentUserId, id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApplicationStatsDto>> GetStats()
    {
        return Ok(await _service.GetStatsAsync(CurrentUserId));
    }
}
