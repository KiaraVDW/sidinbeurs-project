using Microsoft.AspNetCore.Mvc;
using SidInBeurs.Data;

namespace SidInBeurs.Controllers;

[ApiController]
[Route("api/stats")]
public sealed class StatsController(AppRepository repository) : ControllerBase
{
    [HttpGet("most-chosen-program")]
    public async Task<IActionResult> MostChosenProgram() => Ok(await GetExtremeAsync(descending: true));

    [HttpGet("least-chosen-program")]
    public async Task<IActionResult> LeastChosenProgram() => Ok(await GetExtremeAsync(descending: false));

    [HttpGet("fair/{fairId:guid}/visitor-count")]
    public async Task<IActionResult> FairVisitorCount(Guid fairId)
    {
        var db = await repository.GetAsync();
        var fair = db.Fairs.FirstOrDefault(f => f.Id == fairId);
        if (fair is null) return NotFound();
        return Ok(new { fair.Id, fair.Name, Count = db.Registrations.Where(r => r.FairId == fairId).Select(r => r.VisitorId).Distinct().Count() });
    }

    [HttpGet("fair/by-name/{fairName}/visitor-count")]
    public async Task<IActionResult> FairVisitorCountByName(string fairName)
    {
        var db = await repository.GetAsync();
        var fair = db.Fairs.FirstOrDefault(f => f.Name.Contains(fairName, StringComparison.OrdinalIgnoreCase));
        if (fair is null) return NotFound();
        return Ok(new { fair.Id, fair.Name, Count = db.Registrations.Where(r => r.FairId == fair.Id).Select(r => r.VisitorId).Distinct().Count() });
    }

    private async Task<object> GetExtremeAsync(bool descending)
    {
        var db = await repository.GetAsync();
        var query = db.Programs.Select(p => new { p.Id, p.Name, Count = db.Registrations.Count(r => r.ProgramIds.Contains(p.Id)) });
        return descending ? query.OrderByDescending(p => p.Count).ThenBy(p => p.Name).First() : query.OrderBy(p => p.Count).ThenBy(p => p.Name).First();
    }
}
