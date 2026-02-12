# BookwormsOnline

A secure ASP.NET Core Razor Pages web application for book enthusiasts.

## Basic Features

- **User Authentication**: Secure login and registration system using ASP.NET Core Identity.
- **Strong Password Policy**: Enforces a minimum length of 12 characters with a mix of uppercase, lowercase, numbers, and special characters.
- **Account Security**: Includes account lockout after 3 failed attempts and Google reCAPTCHA integration to prevent automated attacks.
- **Data Protection**: Sensitive user information, such as credit card numbers, is encrypted at rest using the ASP.NET Core Data Protection API.
- **Session Management**: Secure session handling with absolute and sliding expiration, including HttpOnly and Secure cookie attributes.
- **Audit Logging**: Tracks security events and user activities for monitoring and compliance.
- **Profile Management**: Logged-in users can view their profile details securely.
- **Error Handling**: Custom error pages and status code re-execution for a professional and secure user experience.

## Tech Stack

- ASP.NET Core 8.0 (Razor Pages)
- Entity Framework Core
- Microsoft SQL Server
- ASP.NET Core Identity
