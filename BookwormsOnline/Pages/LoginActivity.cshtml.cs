using BookwormsOnline.Models;
using BookwormsOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages;

[Authorize]
public class LoginActivityModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _sessionService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginActivityModel> _logger;

    public LoginActivityModel(
        UserManager<ApplicationUser> userManager,
        IUserSessionService sessionService,
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginActivityModel> logger)
    {
        _userManager = userManager;
        _sessionService = sessionService;
        _signInManager = signInManager;
        _logger = logger;
    }

    public List<UserSession> ActiveSessions { get; set; } = new();
    public string CurrentSessionId { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        CurrentSessionId = HttpContext.Session.Id;
        ActiveSessions = await _sessionService.GetActiveSessionsAsync(user.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int sessionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var currentSessionId = HttpContext.Session.Id;
            
            // Load sessions to check if we're revoking the current session
            ActiveSessions = await _sessionService.GetActiveSessionsAsync(user.Id);
            var sessionToRevoke = ActiveSessions.FirstOrDefault(s => s.Id == sessionId);

            await _sessionService.RevokeSessionAsync(sessionId, user.Id);

            // If revoking current session, sign out
            if (sessionToRevoke?.SessionId == currentSessionId)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Login");
            }

            SuccessMessage = "Session revoked successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error revoking session {sessionId}");
            ErrorMessage = "An error occurred while revoking the session.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAllAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var currentSessionId = HttpContext.Session.Id;
            await _sessionService.RevokeAllOtherSessionsAsync(currentSessionId, user.Id);

            SuccessMessage = "All other sessions revoked successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all sessions");
            ErrorMessage = "An error occurred while revoking sessions.";
        }

        return RedirectToPage();
    }

    public string GetBrowserName(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Browser";

        if (userAgent.Contains("Edg/"))
            return "Microsoft Edge";
        if (userAgent.Contains("Chrome/"))
            return "Google Chrome";
        if (userAgent.Contains("Firefox/"))
            return "Mozilla Firefox";
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/"))
            return "Safari";
        if (userAgent.Contains("Opera/") || userAgent.Contains("OPR/"))
            return "Opera";

        return "Unknown Browser";
    }

    public string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        if (userAgent.Contains("Mobile"))
            return "Mobile";
        if (userAgent.Contains("Tablet"))
            return "Tablet";

        return "Desktop";
    }
}
