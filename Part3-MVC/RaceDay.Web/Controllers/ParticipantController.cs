using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api) => _api = api;

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (!result.Success) ViewData["ApiError"] = result.Error;

        var list = result.Data?.OrderByDescending(e => e.EnrolmentDate).ToList() ?? new List<EnrolmentDto>();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");
        if (result.Success) TempData["Success"] = "You have withdrawn from the event.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<MyResultDto>>("api/users/me/results");
        if (!result.Success) ViewData["ApiError"] = result.Error;

        return View(result.Data ?? new List<MyResultDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _api.GetAsync<UserProfileDto>("api/users/me");
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        return View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 100)
        {
            TempData["Error"] = "Please enter a name of up to 100 characters.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.PutAsync<UserProfileDto>("api/users/me", new UpdateProfileRequest { FullName = fullName.Trim() });
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            HttpContext.Session.SignIn(result.Data.UserId, result.Data.FullName, result.Data.Role);
            TempData["Success"] = "Your profile has been updated.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Profile));
        }

        if (!picture.ContentType.StartsWith("image/") || picture.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Please choose an image smaller than 5 MB.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.UploadAsync<UserProfileDto>("api/users/me/picture", picture);
        if (result.Success) TempData["Success"] = "Your profile picture has been updated.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Profile));
    }
}