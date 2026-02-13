# Quick Reference: Toast Notifications & Professional Theme

## Using Toast Notifications

### Basic Usage

```javascript
// Success notification (4 second auto-dismiss)
window.toast.success('Operation completed successfully!');

// Error notification (6 second auto-dismiss)
window.toast.danger('Something went wrong. Please try again.');

// Warning notification (5 second auto-dismiss)
window.toast.warning('Warning: Your session is about to expire.');

// Info notification (5 second auto-dismiss)
window.toast.info('New feature available. Check it out!');
```

### Security Events (No System Details)

```javascript
// reCAPTCHA failure
window.toast.captchaFailed('Security verification failed. Please try again.');

// Account locked
window.toast.accountLocked();  // Auto-generates generic message

// Login failed
window.toast.loginFailed('Login failed. Please check your credentials.');

// Validation error
window.toast.validationError('Please check your input and try again.');
```

### Generic Security Alert Handler

```javascript
window.showSecurityAlert('recaptcha_failed', {
    message: 'Security verification failed. Please try again.',
    duration: 5000
});
```

## In Razor Pages

### In Form Submission Script

```html
@section Scripts {
    <script>
        document.getElementById('myForm').addEventListener('submit', function(e) {
            // Validate
            if (!isValid()) {
                window.toast.warning('Please complete all required fields.');
                e.preventDefault();
                return;
            }
            
            // Success
            window.toast.success('Form submitted successfully!');
        });
    </script>
}
```

### In Async Operations

```javascript
try {
    const response = await fetch('/api/endpoint', {
        method: 'POST',
        body: JSON.stringify(data)
    });
    
    if (response.ok) {
        window.toast.success('Changes saved successfully!');
    } else {
        window.toast.danger('Failed to save changes. Please try again.');
    }
} catch (error) {
    window.toast.danger('An error occurred. Please try again.');
    console.error(error);
}
```

## Toast Notification Types

| Method | Color | Auto-Dismiss | Use Case |
|--------|-------|------------|----------|
| `success()` | Green | 4 sec | Successful operations |
| `danger()` | Red | 6 sec | Errors and failures |
| `warning()` | Yellow | 5 sec | Alerts and cautions |
| `info()` | Blue | 5 sec | Information |
| `securityAlert()` | Yellow | 5 sec | Security events (generic) |
| `accountLocked()` | Red | Manual | Account lockout |
| `captchaFailed()` | Yellow | 6 sec | reCAPTCHA failures |
| `loginFailed()` | Red | 5 sec | Login errors |

## Important: Security Message Guidelines

❌ **DO NOT** expose system details:
```javascript
// BAD
window.toast.danger('User not found with email: ' + email);
window.toast.danger('Invalid password. Remaining attempts: ' + attempts);
window.toast.danger('Database error: connection timeout');
```

✅ **DO** use generic, user-friendly messages:
```javascript
// GOOD
window.toast.danger('Login failed. Please check your credentials.');
window.toast.warning('Login failed. Your account is temporarily locked.');
window.toast.danger('Unable to process request. Please try again.');
```

## Styling Classes

### Navbar
- `.navbar-dark` - Dark theme
- `.sticky-top` - Sticky positioning
- `.dropdown-menu` - Account menu
- `.dropdown-item` - Menu items

### Cards
- `.card` - Card container
- `.card-header` - Header section
- `.card-body` - Content section
- `.card-title` - Title text

### Buttons
- `.btn-primary` - Primary action
- `.btn-secondary` - Secondary action
- `.btn-danger` - Destructive action
- `.btn-outline-*` - Outline variant

### Forms
- `.form-label` - Input label
- `.form-control` - Text input
- `.form-control-lg` - Large input
- `.invalid-feedback` - Error message

## Navigation

### For Authenticated Users
```html
<li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle" href="#" id="accountDropdown" 
       role="button" data-bs-toggle="dropdown">
        Account
    </a>
    <ul class="dropdown-menu dropdown-menu-end">
        <li><a class="dropdown-item" asp-page="/ChangePassword">Change Password</a></li>
        <li><a class="dropdown-item" asp-page="/TwoFactorAuthentication">2FA Setup</a></li>
    </ul>
</li>
```

### For Unauthenticated Users
```html
<li class="nav-item">
    <a class="nav-link btn btn-primary text-white btn-sm ms-2" asp-page="/Login">
        Login
    </a>
</li>
```

## Bootstrap Icons

Used throughout the application:

- `bi-book-fill` - Bookworms logo
- `bi-house-door` - Home link
- `bi-shield-check` - Privacy/Security
- `bi-person-circle` - Account menu
- `bi-key` - Change password
- `bi-shield-lock` - 2FA setup
- `bi-box-arrow-right` - Logout
- `bi-person-plus` - Register
- `bi-box-arrow-in-right` - Login
- `bi-envelope` - Email input
- `bi-lock` - Password input
- `bi-check-circle-fill` - Success icon
- `bi-exclamation-triangle-fill` - Warning icon
- `bi-exclamation-circle-fill` - Error icon
- `bi-info-circle-fill` - Info icon

## Responsive Breakpoints

```css
/* Mobile */
@media (max-width: 576px) {
    /* Hamburger menu, full-width buttons */
}

/* Tablet */
@media (min-width: 768px) {
    /* Expanded menu, 2-column layouts */
}

/* Desktop */
@media (min-width: 1200px) {
    /* Full navbar, multi-column layouts */
}
```

## Common Patterns

### Form with Toast Feedback
```html
<form id="myForm" method="post">
    <div class="mb-3">
        <label class="form-label">Email</label>
        <input type="email" class="form-control" name="email" required>
    </div>
    <button type="submit" class="btn btn-primary">Submit</button>
</form>

@section Scripts {
    <script>
        document.getElementById('myForm').addEventListener('submit', async (e) => {
            e.preventDefault();
            
            try {
                const formData = new FormData(this);
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData
                });
                
                if (response.ok) {
                    window.toast.success('Form submitted successfully!');
                    this.reset();
                } else {
                    window.toast.danger('Failed to submit. Please try again.');
                }
            } catch (error) {
                window.toast.danger('An error occurred. Please try again.');
            }
        });
    </script>
}
```

### Modal with Toast
```html
<div class="modal fade" id="confirmModal">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Action</h5>
            </div>
            <div class="modal-body">
                Are you sure?
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-danger" id="confirmBtn">Delete</button>
            </div>
        </div>
    </div>
</div>

<script>
    document.getElementById('confirmBtn').addEventListener('click', function() {
        // Perform action
        window.toast.success('Deleted successfully!');
        bootstrap.Modal.getInstance(document.getElementById('confirmModal')).hide();
    });
</script>
```

---

**For more detailed information, see BOOTSTRAP_THEME_TOAST_NOTIFICATIONS.md**

