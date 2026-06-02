using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SidInBeurs.Data;
using SidInBeurs.Models;

namespace SidInBeurs.Controllers;

[Authorize(Roles = Roles.Admin)]
public sealed class AdminController(AppRepository repository) : Controller
{
    public async Task<IActionResult> Users()
    {
        var db = await repository.GetAsync();
        return View(new UsersViewModel { Users = db.Users.OrderBy(u => u.Email).ToList(), Roles = AllRoles });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserInput input)
    {
        var token = await repository.CreateUserAsync(input.Email, input.DisplayName, input.Role);
        TempData["InviteUrl"] = Url.Action(nameof(CompleteInvite), "Admin", new { token }, Request.Scheme);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var db = await repository.GetAsync();
        db.Users.RemoveAll(u => u.Id == id && u.Email != "admin@sidin.local");
        await repository.SaveAsync(db);
        return RedirectToAction(nameof(Users));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> CompleteInvite(string token)
    {
        var db = await repository.GetAsync();
        var user = db.Users.FirstOrDefault(u => u.InviteToken == token);
        return user is null ? NotFound() : View(new CompleteInviteInput { Token = token, Email = user.Email });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteInvite(CompleteInviteInput input)
    {
        if (input.Password.Length < 8)
        {
            ModelState.AddModelError(nameof(input.Password), "Gebruik minstens 8 tekens.");
            return View(input);
        }
        var ok = await repository.CompleteInviteAsync(input.Token, input.Password);
        return ok ? RedirectToAction("Login", "Auth") : NotFound();
    }

    private static readonly List<string> AllRoles = [Roles.Exhibitor, Roles.Marketing, Roles.TeamLead, Roles.Admin];
}

public sealed class UsersViewModel
{
    public List<AppUser> Users { get; set; } = [];
    public List<string> Roles { get; set; } = [];
}

public sealed class CreateUserInput
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = Roles.Marketing;
}

public sealed class CompleteInviteInput
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
