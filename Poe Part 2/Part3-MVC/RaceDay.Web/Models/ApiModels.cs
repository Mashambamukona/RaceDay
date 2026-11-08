using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Web.Models;

// ---------- Data coming back from the API ----------
public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string? BannerUrl { get; set; }
}

public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}

public class OrganiserEventDto : EventDto
{
    public int EnrolmentCount { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal CategoryDistance { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class MyResultDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public int TotalFinishers { get; set; }
}

// ---------- Forms the user fills in (sent to the API) ----------
public class LoginRequest
{
    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email."), EmailAddress(ErrorMessage = "Please enter a valid email."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [JsonIgnore]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
}

public class EventRequest
{
    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and time.")]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999.99, ErrorMessage = "Distance must be between 0.01 and 9999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;
}

public class CategoryRequest
{
    [Required(ErrorMessage = "Please enter a category name."), StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.01, 9999.99, ErrorMessage = "Distance must be between 0.01 and 9999.99 km.")]
    [Display(Name = "Category distance (km)")]
    public decimal CategoryDistance { get; set; }
}

public class ResultRequest
{
    [Required(ErrorMessage = "Please enter the finish time (hh:mm:ss).")]
    [Display(Name = "Finish time")]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or higher.")]
    [Display(Name = "Finish position")]
    public int? FinishPosition { get; set; }
}