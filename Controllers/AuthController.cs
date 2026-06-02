using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SidInBeurs.Data;

namespace SidInBeurs.Controllers;

public sealed class AuthController(AppRepository repository) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginInput { ReturnUrl = returnUrl ?? "/" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var user = await repository.ValidateUserAsync(input.Email, input.Password);
        if (user is null)
        {
            ModelState.AddModelError("", "Ongeldige login of wachtwoord.");
            return View(input);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync("SidCookie", new ClaimsPrincipal(new ClaimsIdentity(claims, "SidCookie")));
        return LocalRedirect(string.IsNullOrWhiteSpace(input.ReturnUrl) ? "/" : input.ReturnUrl);
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("SidCookie");
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Denied() => View();
}

public sealed class LoginInput
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ReturnUrl { get; set; } = "/";
}
