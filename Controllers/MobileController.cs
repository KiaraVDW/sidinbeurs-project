using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SidInBeurs.Data;
using SidInBeurs.Models;

namespace SidInBeurs.Controllers;

[Authorize(Roles = Roles.Exhibitor)]
public sealed class MobileController(AppRepository repository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Register(string visitorId = "SID-2001", string firstName = "Nieuwe", string lastName = "Studiekiezer", DateOnly? birthDate = null, string school = "Onbekende school", string studyArea = "Onbekend")
    {
        var db = await repository.GetAsync();
        var existing = db.Visitors.FirstOrDefault(v => v.VisitorCode.Equals(visitorId, StringComparison.OrdinalIgnoreCase));
        var visitor = existing ?? new Visitor
        {
            VisitorCode = visitorId,
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate ?? new DateOnly(2007, 1, 1),
            CurrentSchool = school,
            CurrentStudyArea = studyArea
        };
        var registration = existing is null ? null : db.Registrations.LastOrDefault(r => r.VisitorId == existing.Id);
        return View(new MobileRegistrationInput
        {
            VisitorCode = visitor.VisitorCode,
            FirstName = visitor.FirstName,
            LastName = visitor.LastName,
            BirthDate = visitor.BirthDate,
            CurrentSchool = visitor.CurrentSchool,
            CurrentStudyArea = visitor.CurrentStudyArea,
            FairId = registration?.FairId ?? db.Fairs.First().Id,
            SelectedProgramIds = registration?.ProgramIds ?? [],
            Notes = registration?.Notes ?? "",
            Programs = db.Programs,
            Fairs = db.Fairs
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(MobileRegistrationInput input)
    {
        var db = await repository.GetAsync();
        input.Programs = db.Programs;
        input.Fairs = db.Fairs;
        if (input.SelectedProgramIds.Count == 0)
        {
            ModelState.AddModelError(nameof(input.SelectedProgramIds), "Kies minstens een opleiding.");
        }
        if (!ModelState.IsValid) return View(input);

        await repository.UpsertVisitorRegistrationAsync(new Visitor
        {
            VisitorCode = input.VisitorCode,
            FirstName = input.FirstName,
            LastName = input.LastName,
            BirthDate = input.BirthDate,
            CurrentSchool = input.CurrentSchool,
            CurrentStudyArea = input.CurrentStudyArea
        }, input.FairId, input.SelectedProgramIds, input.Notes);
        TempData["Success"] = "Interesses opgeslagen.";
        return RedirectToAction(nameof(Register), new { visitorId = input.VisitorCode });
    }
}

public sealed class MobileRegistrationInput
{
    public string VisitorCode { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string CurrentSchool { get; set; } = "";
    public string CurrentStudyArea { get; set; } = "";
    public Guid FairId { get; set; }
    public List<Guid> SelectedProgramIds { get; set; } = [];
    public string Notes { get; set; } = "";
    public List<ProgramInterest> Programs { get; set; } = [];
    public List<Fair> Fairs { get; set; } = [];
}
