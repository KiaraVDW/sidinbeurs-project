using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SidInBeurs.Data;
using SidInBeurs.Models;

namespace SidInBeurs.Controllers;

[Authorize(Roles = $"{Roles.Marketing},{Roles.TeamLead},{Roles.Admin}")]
public sealed class DashboardController(AppRepository repository) : Controller
{
    public async Task<IActionResult> Interests(Guid? programId)
    {
        var db = await repository.GetAsync();
        var selectedProgram = programId ?? db.Programs.First().Id;
        var rows = db.Registrations
            .Where(r => r.ProgramIds.Contains(selectedProgram))
            .Select(r => new DashboardRow(db.Visitors.Single(v => v.Id == r.VisitorId), db.Fairs.Single(f => f.Id == r.FairId), r))
            .OrderBy(r => r.Visitor.LastName)
            .ToList();
        return View(new InterestsViewModel { Programs = db.Programs, SelectedProgramId = selectedProgram, Rows = rows });
    }

    public async Task<IActionResult> Counts()
    {
        var db = await repository.GetAsync();
        var rows = db.Programs.Select(p => new CountRow(
            p,
            db.Registrations.Count(r => r.ProgramIds.Contains(p.Id)),
            db.Fairs.ToDictionary(f => f.Name, f => db.Registrations.Count(r => r.FairId == f.Id && r.ProgramIds.Contains(p.Id)))))
            .OrderByDescending(r => r.Total)
            .ToList();
        return View(new CountsViewModel { Fairs = db.Fairs, Rows = rows });
    }

    [Authorize(Roles = $"{Roles.TeamLead},{Roles.Admin}")]
    public async Task<IActionResult> Visitors()
    {
        var db = await repository.GetAsync();
        return View(db.Visitors.OrderBy(v => v.LastName).ToList());
    }

    [Authorize(Roles = $"{Roles.TeamLead},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> EditVisitor(Guid id)
    {
        var db = await repository.GetAsync();
        var visitor = db.Visitors.FirstOrDefault(v => v.Id == id);
        return visitor is null ? NotFound() : View(visitor);
    }

    [Authorize(Roles = $"{Roles.TeamLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVisitor(Visitor input)
    {
        var db = await repository.GetAsync();
        var visitor = db.Visitors.FirstOrDefault(v => v.Id == input.Id);
        if (visitor is null) return NotFound();
        visitor.VisitorCode = input.VisitorCode;
        visitor.FirstName = input.FirstName;
        visitor.LastName = input.LastName;
        visitor.BirthDate = input.BirthDate;
        visitor.CurrentSchool = input.CurrentSchool;
        visitor.CurrentStudyArea = input.CurrentStudyArea;
        await repository.SaveAsync(db);
        return RedirectToAction(nameof(Visitors));
    }

    [Authorize(Roles = $"{Roles.TeamLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVisitors(List<Guid> selectedIds)
    {
        await repository.DeleteVisitorsAsync(selectedIds);
        return RedirectToAction(nameof(Visitors));
    }
}

public sealed class InterestsViewModel
{
    public List<ProgramInterest> Programs { get; set; } = [];
    public Guid SelectedProgramId { get; set; }
    public List<DashboardRow> Rows { get; set; } = [];
}

public sealed class CountsViewModel
{
    public List<Fair> Fairs { get; set; } = [];
    public List<CountRow> Rows { get; set; } = [];
}
