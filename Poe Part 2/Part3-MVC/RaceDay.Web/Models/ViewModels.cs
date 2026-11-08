namespace RaceDay.Web.Models;

public class HomeIndexViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Location { get; set; }
    public string? EventType { get; set; }
    public DateTime? Date { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public bool IsParticipant { get; set; }
    public bool AlreadyEnrolled { get; set; }
}