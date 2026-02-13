# BookwormsOnline

A comprehensive and secure ASP.NET Core Razor Pages web application designed with enterprise-grade security features for book enthusiasts.

## Core Features

### 🔐 Authentication & Authorization
- **User Authentication**: Secure login and registration system using ASP.NET Core Identity
- **Email Verification**: Email-based account confirmation and password reset workflows
- **Two-Factor Authentication (2FA)**: Support for authenticator apps (TOTP) for enhanced account security
- **Concurrent Session Control**: Security stamp invalidation to prevent multiple simultaneous sessions for the same user
- **Account Lockout**: Automatic lockout after 3 failed login attempts with 15-minute cooldown period

### 🛡️ Password Security
- **Strong Password Policy**: Enforces minimum 12 characters with:
  - Uppercase letters (A-Z)
  - Lowercase letters (a-z)
  - Numeric digits (0-9)
  - Special characters (!@#$%^&*, etc.)
- **Password Expiry Enforcement**: Mandatory password change every 90 days with middleware-level enforcement
- **Password History Tracking**: Maintains history of changed passwords to prevent reuse
- **Secure Password Reset**: Email-based reset tokens with expiration validation

### 🤖 Bot & Abuse Prevention
- **Google reCAPTCHA v3 Integration**: Advanced bot detection on login and registration pages
  - Dynamic score-based risk assessment
  - Fresh token generation on every form submission attempt
  - Intelligent retry handling with lowered thresholds for legitimate users
  - Comprehensive logging of verification attempts and scores

### 📊 Data Protection & Privacy
- **Encryption at Rest**: Credit card numbers encrypted using ASP.NET Core Data Protection API
- **Secure Session Management**: 
  - 20-minute absolute and sliding expiration
  - HttpOnly cookies to prevent XSS attacks
  - Secure cookie flag for HTTPS-only transmission
  - Strict SameSite policy to prevent CSRF attacks
- **Personal Data Management**: GDPR-compliant personal data marking and handling

### 📁 File Upload Security
- **Photo Upload Service** with multi-layer validation:
  - File extension validation (.jpg only)
  - MIME type verification (image/jpeg)
  - Magic number signature detection (JPEG file header validation)
  - File size limits (2MB maximum)
  - Secure file path handling with sanitization
- **Secure Storage**: Uploaded files stored in protected `/uploads` folder

### 📝 Comprehensive Audit Logging
- **Security Event Tracking**: Logs all authentication events:
  - Successful logins
  - Failed login attempts
  - Account lockouts
  - Password changes
  - Two-factor authentication events
- **Audit Log Details**:
  - User ID and email address
  - Specific action performed
  - Exact timestamp
  - Client IP address
- **Compliance Ready**: Full audit trail for security compliance and forensic analysis

### 💬 User Profile Management
- **Profile Information**: Secure storage and display of:
  - First and last names
  - Email address
  - Mobile number
  - Billing and shipping addresses
  - Credit card information (encrypted)
  - Profile photo
- **Secure Profile Access**: Only accessible to authenticated users
- **Password Change Interface**: Dedicated page for self-service password updates

### 📧 Email Integration
- **Email Notifications** via Mailtrap:
  - Account confirmation emails
  - Password reset emails
  - Two-factor authentication codes
  - Account security alerts
- **HTML Email Templates**: Professional formatted email messages
- **Error Handling**: Graceful handling of email service failures

### 🎨 User Experience
- **Custom Error Pages**: Professional error handling with status code mapping
- **Responsive Design**: Bootstrap-based responsive UI
- **Form Validation**: Client-side and server-side validation
- **User Feedback**: Clear success and error messages for all operations
- **Accessibility**: ARIA labels and semantic HTML for accessibility

## Tech Stack

- **Framework**: ASP.NET Core 8.0 (Razor Pages)
- **ORM**: Entity Framework Core with SQL Server
- **Authentication**: ASP.NET Core Identity with Entity Framework
- **Security**: 
  - Google reCAPTCHA v3 API
  - Data Protection API (DPAPI)
  - Secure password hashing with PBKDF2
- **Email**: Mailtrap SMTP service
- **Database**: Microsoft SQL Server
- **Frontend**: Bootstrap 5, jQuery, Razor Pages

## Security Highlights

* ✅ **Defense in Depth**: Multiple layers of security (authentication, authorization, encryption, audit logging)
* ✅ **OWASP Compliance**: Protection against common web vulnerabilities
* ✅ **Credential Protection**: Secure password storage with ASP.NET Identity hashing
* ✅ **Session Security**: Secure cookie configuration with defense against session fixation
* ✅ **Data Integrity**: Audit logs for accountability and forensic analysis
* ✅ **Abuse Prevention**: reCAPTCHA + account lockout protection against brute force attacks
* ✅ **Data Encryption**: Sensitive data encrypted at rest using DPAPI
* ✅ **Compliance Ready**: Supports GDPR personal data handling and audit trails

## Project Structure

```
BookwormsOnline/
├── Pages/                          # Razor Page handlers and views
│   ├── Login.cshtml(.cs)          # Secure login with reCAPTCHA
│   ├── Register.cshtml(.cs)       # User registration
│   ├── TwoFactorAuthentication.cshtml(.cs)  # 2FA setup
│   ├── LoginWith2fa.cshtml(.cs)   # 2FA verification
│   ├── ChangePassword.cshtml(.cs) # Password change with expiry
│   ├── ForgotPassword.cshtml(.cs) # Password reset workflow
│   └── ...                         # Other pages
├── Models/                         # Data models
│   ├── ApplicationUser.cs          # Extended Identity user
│   ├── AuditLog.cs                # Audit log entries
│   └── PasswordHistory.cs          # Password history tracking
├── Services/                       # Business logic services
│   ├── ReCaptchaService.cs        # Google reCAPTCHA verification
│   ├── AuditLogService.cs         # Audit logging
│   ├── EmailSender.cs             # Email notifications
│   ├── EncryptionService.cs       # Data encryption/decryption
│   ├── PhotoUploadService.cs      # Secure file uploads
│   └── StandardAddressAttribute.cs # Address validation
├── Middleware/                     # Custom middleware
│   └── PasswordAgeMiddleware.cs   # 90-day password expiry enforcement
├── Data/                           # Database context
│   └── ApplicationDbContext.cs     # EF Core configuration
├── Migrations/                     # Database migrations
└── wwwroot/                        # Static files (CSS, JS, uploads)
```
