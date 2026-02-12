using Microsoft.AspNetCore.Http;

namespace BookwormsOnline.Services;

/// <summary>
/// Service for secure photo file uploads with validation.
/// Validates file extension, MIME type, magic number signature, and file size.
/// </summary>
public interface IPhotoUploadService
{
    /// <summary>
    /// Validates and saves an uploaded photo file.
    /// </summary>
    /// <param name="file">The uploaded file from the form</param>
    /// <param name="uploadsFolder">The physical path to the uploads folder</param>
    /// <returns>Result containing success status and error message or file path</returns>
    Task<PhotoUploadResult> ValidateAndSavePhotoAsync(IFormFile file, string uploadsFolder);
}

/// <summary>
/// Result of photo upload validation and save operation
/// </summary>
public class PhotoUploadResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? FilePath { get; set; }
}

/// <summary>
/// Implementation of secure photo upload service
/// </summary>
public class PhotoUploadService : IPhotoUploadService
{
    private const long MAX_FILE_SIZE = 2 * 1024 * 1024; // 2MB in bytes
    private const string ALLOWED_EXTENSION = ".jpg";
    private const string ALLOWED_MIME_TYPE = "image/jpeg";
    
    // JPEG Magic Number (File Signature): FF D8 FF
    private static readonly byte[] JPEG_MAGIC_NUMBER = { 0xFF, 0xD8, 0xFF };
    
    private readonly ILogger<PhotoUploadService> _logger;

    public PhotoUploadService(ILogger<PhotoUploadService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates and saves a photo file with multiple security checks
    /// </summary>
    public async Task<PhotoUploadResult> ValidateAndSavePhotoAsync(IFormFile file, string uploadsFolder)
    {
        // Null check
        if (file == null)
        {
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = "No file was uploaded." 
            };
        }

        // 1. Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (extension != ALLOWED_EXTENSION)
        {
            _logger.LogWarning($"Photo upload rejected: Invalid extension '{extension}' for file '{file.FileName}'");
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = "Only .JPG files are allowed." 
            };
        }

        // 2. Validate file size
        if (file.Length > MAX_FILE_SIZE)
        {
            var maxSizeMB = MAX_FILE_SIZE / (1024 * 1024);
            _logger.LogWarning($"Photo upload rejected: File size {file.Length} bytes exceeds {maxSizeMB}MB limit");
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = $"File size cannot exceed {maxSizeMB}MB. Uploaded file is {Math.Round(file.Length / (1024.0 * 1024.0), 2)}MB." 
            };
        }

        // 3. Validate MIME type (basic check)
        if (file.ContentType.ToLower() != ALLOWED_MIME_TYPE)
        {
            _logger.LogWarning($"Photo upload rejected: Invalid MIME type '{file.ContentType}' for file '{file.FileName}'");
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = "Invalid file type. Only JPG images are allowed." 
            };
        }

        // 4. Validate magic number (file signature)
        var magicNumberValidation = await ValidateMagicNumberAsync(file);
        if (!magicNumberValidation.IsSuccess)
        {
            _logger.LogWarning($"Photo upload rejected: Invalid magic number for file '{file.FileName}'");
            return magicNumberValidation;
        }

        // 5. Create uploads folder if it doesn't exist
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            _logger.LogInformation($"Created uploads folder: {uploadsFolder}");
        }

        // 6. Generate secure filename using GUID to prevent path traversal and overwriting
        var uniqueFileName = GenerateSecureFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        try
        {
            // 7. Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Photo successfully uploaded: {uniqueFileName}");
            
            return new PhotoUploadResult 
            { 
                IsSuccess = true, 
                Message = "Photo uploaded successfully.",
                FilePath = "/uploads/" + uniqueFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving photo file: {filePath}");
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = "An error occurred while saving the photo. Please try again." 
            };
        }
    }

    /// <summary>
    /// Validates the file's magic number (first 3 bytes for JPEG: FF D8 FF)
    /// </summary>
    private async Task<PhotoUploadResult> ValidateMagicNumberAsync(IFormFile file)
    {
        try
        {
            // Read first 3 bytes of the file
            byte[] buffer = new byte[3];
            using (var stream = file.OpenReadStream())
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, 3);
                
                if (bytesRead < 3)
                {
                    return new PhotoUploadResult 
                    { 
                        IsSuccess = false, 
                        Message = "File is too small to be a valid JPEG image." 
                    };
                }

                // Compare with JPEG magic number
                if (!buffer.SequenceEqual(JPEG_MAGIC_NUMBER))
                {
                    return new PhotoUploadResult 
                    { 
                        IsSuccess = false, 
                        Message = "File signature does not match a valid JPEG file. The file may be corrupted or not a genuine JPG image." 
                    };
                }
            }

            return new PhotoUploadResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file magic number");
            return new PhotoUploadResult 
            { 
                IsSuccess = false, 
                Message = "An error occurred while validating the file." 
            };
        }
    }

    /// <summary>
    /// Generates a secure filename using GUID to prevent path traversal and file overwriting
    /// </summary>
    private string GenerateSecureFileName(string originalFileName)
    {
        // Extract only the extension (already validated as .jpg)
        var extension = Path.GetExtension(originalFileName).ToLower();
        
        // Generate GUID-based filename: {GUID}.jpg
        // This prevents:
        // - Path traversal attacks (no directory separators in filename)
        // - File overwriting (GUID is unique)
        // - Information disclosure (original filename not visible)
        var secureFileName = $"{Guid.NewGuid()}{extension}";
        
        return secureFileName;
    }
}

