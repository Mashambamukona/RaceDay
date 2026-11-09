namespace RaceDay.Web.Services;

/// <summary>Small helpers for reading who is logged in from the web session.</summary>
public static class UserSessionExtensions
{
    public static void SignIn(this ISession session, int userId, string fullName, string role)
    {
        session.SetInt32("UserId", userId);
        session.SetString("FullName", fullName);
        session.SetString("Role", role);
    }

    public static bool IsLoggedIn(this ISession session) => session.GetInt32("UserId").HasValue;
    public static int? GetUserId(this ISession session) => session.GetInt32("UserId");
    public static string? GetRole(this ISession session) => session.GetString("Role");
    public static string GetFullName(this ISession session) => session.GetString("FullName") ?? string.Empty;
}