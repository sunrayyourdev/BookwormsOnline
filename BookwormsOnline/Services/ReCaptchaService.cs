using System.Text.Json;

namespace BookwormsOnline.Services;

public class ReCaptchaService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ReCaptchaService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<bool> VerifyAsync(string token)
    {
        var secretKey = _configuration["ReCaptcha:SecretKey"];
        var response = await _httpClient.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
            null);

        if (!response.IsSuccessStatusCode) return false;

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var success = jsonDoc.RootElement.GetProperty("success").GetBoolean();
        var score = jsonDoc.RootElement.GetProperty("score").GetDouble();

        // Threshold of 0.5 is common for v3
        return success && score >= 0.5;
    }
}
