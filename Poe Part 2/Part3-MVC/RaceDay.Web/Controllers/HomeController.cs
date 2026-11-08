using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? location, string? eventType, DateTime? date)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location.Trim())}");
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (date.HasValue) query.Add($"date={date.Value:yyyy-MM-dd}");

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var result = await _api.GetAsync<List<EventDto>>(url);

        var vm = new HomeIndexViewModel { Location = location, EventType = eventType, Date = date };

        if (result.Success && result.Data is not null)
        {
            vm.Events = result.Data
                .Where(e => e.EventDate >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .ToList();
        }
        else
        {
            ViewData["ApiError"] = result.Error;
        }

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = "We could not find that event.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new EventDetailsViewModel
        {
            Event = result.Data,
            IsParticipant = HttpContext.Session.GetRole() == "Participant"
        };

        if (vm.IsParticipant)
        {
            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
            vm.AlreadyEnrolled = mine.Success && mine.Data is not null && mine.Data.Any(e => e.EventId == id);
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, SessionAuthorize("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrol");
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You are enrolled! Your enrolment stays Pending until the organiser confirms it.";
        return RedirectToAction("MyEnrolments", "Participant");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}