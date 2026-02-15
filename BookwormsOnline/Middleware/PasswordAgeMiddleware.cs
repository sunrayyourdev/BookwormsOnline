using System;
using System.Threading.Tasks;
using BookwormsOnline.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookwormsOnline.Middleware
{
    public class PasswordAgeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _maxPasswordAgeMinutes;

        public PasswordAgeMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _maxPasswordAgeMinutes = configuration.GetValue<int>("SecuritySettings:PasswordSettings:MaximumAgeMinutes", 129600); // Default 90 days
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only check for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path.Value ?? string.Empty;

                // Allow access to login, logout, change password, error and static files without redirection loop
                if (!path.StartsWith("/ChangePassword", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/Login", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/Logout", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
                {
                    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await userManager.GetUserAsync(context.User);

                    if (user != null)
                    {
                        var age = DateTime.UtcNow - user.LastPasswordChangedDate;
                        if (age > TimeSpan.FromMinutes(_maxPasswordAgeMinutes))
                        {
                            context.Response.Redirect("/ChangePassword?expired=1");
                            return; // Short-circuit pipeline
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}