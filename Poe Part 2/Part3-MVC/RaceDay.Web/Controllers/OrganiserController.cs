using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api) => _api = api;

    // ---------- Dashboard ----------
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!result.Success) ViewData["ApiError"] = result.Error;
        return View(result.Data ?? new List<OrganiserEventDto>());
    }

    // ---------- Events list ----------
    public async Task<IActionResult> Events()
    {
        var result = await _api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!result.Success) ViewData["ApiError"] = result.Error;
        return View(result.Data ?? new List<OrganiserEventDto>());
    }

    // ---------- Create ----------
    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        if (!IsValidBanner(banner, out var bannerError))
        {
            ModelState.AddModelError(string.Empty, bannerError);
            return View("EventForm", model);
        }

        var created = await _api.PostAsync<EventDto>("api/events", ToRequest(model));
        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View("EventForm", model);
        }

        if (banner is not null && banner.Length > 0)
        {
            var upload = await _api.UploadAsync<EventDto>($"api/events/{created.Data.EventId}/banner", banner);
            if (!upload.Success)
            {
                TempData["Error"] = "The event was created, but the banner could not be uploaded. " + upload.Error;
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Edit ----------
    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventDetailDto>($"api/events/{id}");
        if (!result.Success || result.Data is null || result.Data.OrganiserId != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "We could not find that event.";
            return RedirectToAction(nameof(Events));
        }

        var e = result.Data;
        return View("EventForm", new EventFormViewModel
        {
            EventId = e.EventId,
            Name = e.Name,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model, IFormFile? banner)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View("EventForm", model);

        if (!IsValidBanner(banner, out var bannerError))
        {
            ModelState.AddModelError(string.Empty, bannerError);
            return View("EventForm", model);
        }

        var updated = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View("EventForm", model);
        }

        if (banner is not null && banner.Length > 0)
        {
            var upload = await _api.UploadAsync<EventDto>($"api/events/{id}/banner", banner);
            if (!upload.Success)
            {
                TempData["Error"] = "The event was updated, but the banner could not be uploaded. " + upload.Error;
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Delete ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");
        if (result.Success) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int? eventId)
    {
        return View(await BuildCategoriesViewModel(eventId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, [Bind(Prefix = "NewCategory")] CategoryRequest model)
    {
        if (model.MinAge.HasValue && model.MaxAge.HasValue && model.MinAge > model.MaxAge)
            ModelState.AddModelError(string.Empty, "Minimum age cannot be higher than maximum age.");

        if (ModelState.IsValid)
        {
            var result = await _api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", model);
            if (result.Success)
            {
                TempData["Success"] = "Category added.";
                return RedirectToAction(nameof(Categories), new { eventId });
            }

            ModelState.AddModelError(string.Empty, result.Error);
        }

        var vm = await BuildCategoriesViewModel(eventId);
        vm.NewCategory = model;
        return View("Categories", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");
        if (result.Success) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private async Task<CategoriesViewModel> BuildCategoriesViewModel(int? eventId)
    {
        var events = await _api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!events.Success) ViewData["ApiError"] = events.Error;

        var vm = new CategoriesViewModel { Events = events.Data ?? new List<OrganiserEventDto>() };
        vm.SelectedEventId = eventId ?? vm.Events.OrderBy(e => e.EventDate).FirstOrDefault()?.EventId;

        if (vm.SelectedEventId.HasValue)
        {
            var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{vm.SelectedEventId}/categories");
            vm.Categories = cats.Data ?? new List<CategoryDto>();
        }

        return vm;
    }
    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int? eventId)
    {
        return View(await BuildEnrolmentsViewModel(eventId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEnrolmentStatus(int id, string status, int eventId)
    {
        var result = await _api.PutAsync<EnrolmentDto>($"api/enrolments/{id}/status", new { Status = status });
        if (result.Success) TempData["Success"] = $"Enrolment marked as {status}.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    private async Task<EnrolmentsViewModel> BuildEnrolmentsViewModel(int? eventId)
    {
        var events = await _api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!events.Success) ViewData["ApiError"] = events.Error;

        var vm = new EnrolmentsViewModel { Events = events.Data ?? new List<OrganiserEventDto>() };
        vm.SelectedEventId = eventId ?? vm.Events.OrderBy(e => e.EventDate).FirstOrDefault()?.EventId;

        if (vm.SelectedEventId.HasValue)
        {
            var list = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{vm.SelectedEventId}/enrolments");
            if (!list.Success) ViewData["ApiError"] = list.Error;
            vm.Enrolments = list.Data ?? new List<EnrolmentDto>();
        }

        return vm;
    }
    // ---------- Results ----------
    public async Task<IActionResult> Results(int? eventId)
    {
        return View(await BuildResultsViewModel(eventId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(int id, int eventId, string? finishTime, int? finishPosition)
    {
        if (!TimeSpan.TryParse(finishTime, out var time) || time <= TimeSpan.Zero || time >= TimeSpan.FromHours(24))
        {
            TempData["Error"] = "Please enter the finish time as hh:mm:ss, for example 00:52:31.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await _api.PostAsync<ResultDto>($"api/enrolments/{id}/result",
            new { FinishTime = time, FinishPosition = finishPosition });

        if (result.Success) TempData["Success"] = "Result saved.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Results), new { eventId });
    }

    private async Task<ResultsViewModel> BuildResultsViewModel(int? eventId)
    {
        var events = await _api.GetAsync<List<OrganiserEventDto>>("api/events/mine");
        if (!events.Success) ViewData["ApiError"] = events.Error;

        var vm = new ResultsViewModel { Events = events.Data ?? new List<OrganiserEventDto>() };
        vm.SelectedEventId = eventId ?? vm.Events.OrderBy(e => e.EventDate).FirstOrDefault()?.EventId;

        if (vm.SelectedEventId.HasValue)
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{vm.SelectedEventId}/enrolments");
            if (!enrolments.Success) ViewData["ApiError"] = enrolments.Error;
            vm.Enrolments = enrolments.Data ?? new List<EnrolmentDto>();

            foreach (var categoryId in vm.Enrolments.Select(e => e.CategoryId).Distinct())
            {
                var board = await _api.GetAsync<List<ResultDto>>($"api/categories/{categoryId}/results");
                if (board.Success && board.Data is not null)
                {
                    foreach (var r in board.Data) vm.Results[r.EnrolmentId] = r;
                }
            }
        }

        return vm;
    }
    // ---------- Helpers ----------
    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        Name = m.Name,
        Description = m.Description,
        EventDate = m.EventDate,
        Location = m.Location,
        Distance = m.Distance,
        EventType = m.EventType
    };

    private static bool IsValidBanner(IFormFile? file, out string error)
    {
        error = string.Empty;
        if (file is null || file.Length == 0) return true;

        if (!file.ContentType.StartsWith("image/"))
        {
            error = "The banner must be an image file.";
            return false;
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            error = "The banner must be smaller than 5 MB.";
            return false;
        }

        return true;
    }
}
