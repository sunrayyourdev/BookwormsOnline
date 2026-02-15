// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function checkPasswordStrength() {
    const password = document.getElementById('passwordInput').value;
    const strengthMeterFill = document.getElementById('password-strength-meter-fill');
    const strengthText = document.getElementById('password-strength-text');

    const checks = {
        length: password.length >= 12,
        upper: /[A-Z]/.test(password),
        lower: /[a-z]/.test(password),
        number: /[0-9]/.test(password),
        special: /[@$!%*?&]/.test(password)
    };

    updateChecklist('check-length', checks.length);
    updateChecklist('check-upper', checks.upper);
    updateChecklist('check-lower', checks.lower);
    updateChecklist('check-number', checks.number);
    updateChecklist('check-special', checks.special);

    let score = 0;
    if (password.length > 0) {
        if (checks.length) score++;
        if (checks.upper) score++;
        if (checks.lower) score++;
        if (checks.number) score++;
        if (checks.special) score++;
    }

    let strength = "";
    let colorClass = "";
    let width = "0%";

    switch (score) {
        case 0:
            strength = "";
            colorClass = "";
            width = "0%";
            break;
        case 1:
        case 2:
            strength = "Weak";
            colorClass = "strength-weak";
            width = "25%";
            break;
        case 3:
            strength = "Medium";
            colorClass = "strength-medium";
            width = "50%";
            break;
        case 4:
            strength = "Strong";
            colorClass = "strength-strong";
            width = "75%";
            break;
        case 5:
            strength = "Very Strong";
            colorClass = "strength-very-strong";
            width = "100%";
            break;
    }

    strengthMeterFill.className = colorClass;
    strengthMeterFill.style.width = width;
    strengthText.innerText = strength;
    strengthText.className = "form-text " + (colorClass ? "text-" + colorClass.split('-')[1] : "");
}

function updateChecklist(id, isValid) {
    const element = document.getElementById(id);
    const icon = element.querySelector('i');

    if (isValid) {
        element.classList.remove('invalid');
        element.classList.add('valid');
        icon.className = 'bi bi-check-circle-fill';
    } else {
        element.classList.remove('valid');
        element.classList.add('invalid');
        icon.className = 'bi bi-circle';
    }
}
