using System.Text.Json;

namespace BookwormsOnline.Services;

public class ReCaptchaService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReCaptchaService> _logger;

    public ReCaptchaService(IConfiguration configuration, HttpClient httpClient, ILogger<ReCaptchaService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("reCAPTCHA token is null or empty");
            return false;
        }

        var secretKey = Environment.GetEnvironmentVariable("RECAPTCHA_SECRETKEY");
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogError("RECAPTCHA_SECRETKEY environment variable not configured");
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"reCAPTCHA API returned status code: {response.StatusCode}");
                return false;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"reCAPTCHA response: {jsonString}");

            using var jsonDoc = JsonDocument.Parse(jsonString);
            
            // Check if "success" property exists before accessing it
            if (!jsonDoc.RootElement.TryGetProperty("success", out var successElement))
            {
                _logger.LogWarning("reCAPTCHA response missing 'success' property");
                return false;
            }

            var success = successElement.GetBoolean();
            
            // Check if "score" property exists before accessing it
            if (!jsonDoc.RootElement.TryGetProperty("score", out var scoreElement))
            {
                _logger.LogWarning("reCAPTCHA response missing 'score' property");
                return false;
            }

            var score = scoreElement.GetDouble();

            _logger.LogInformation($"reCAPTCHA verification - Success: {success}, Score: {score}");

            // Threshold of 0.3 allows for retries while still filtering bots
            // (0.5 was too strict for legitimate retry attempts)
            const double threshold = 0.5;
            var isValid = success && score >= threshold;
            
            if (!isValid)
            {
                _logger.LogWarning($"reCAPTCHA verification failed - Score {score} below threshold {threshold}");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception during reCAPTCHA verification: {ex.Message}");
            return false;
        }
    }
}
