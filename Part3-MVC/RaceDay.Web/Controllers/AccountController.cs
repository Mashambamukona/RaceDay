using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToHome(HttpContext.Session.GetRole());

        return View(new LoginRequest());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<AuthResponse>("api/auth/login", model);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data.UserId, result.Data.FullName, result.Data.Role);
        return RedirectToHome(result.Data.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToHome(HttpContext.Session.GetRole());

        return View(new RegisterRequest());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.PostAsync<AuthResponse>("api/auth/register", model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<bool>("api/auth/logout");
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome(string? role) =>
        role == "Organiser"
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Home");
}