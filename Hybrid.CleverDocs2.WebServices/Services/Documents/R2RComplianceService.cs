using System.Security.Cryptography;
using System.Text;
using Hybrid.CleverDocs2.WebServices.Models.Documents;

namespace Hybrid.CleverDocs2.WebServices.Services.Documents
{
    /// <summary>
    /// Service for R2R compliance utilities (filename convention, checksum, etc.)
    /// </summary>
    public interface IR2RComplianceService
    {
        /// <summary>
        /// Generates R2R compliant filename with timestamp
        /// </summary>
        string GenerateR2RCompliantFilename(string originalFileName, Guid documentId);
        
        /// <summary>
        /// Computes MD5 checksum for file integrity
        /// </summary>
        Task<string> ComputeChecksumAsync(string filePath);
        
        /// <summary>
        /// Computes MD5 checksum for byte array
        /// </summary>
        string ComputeChecksum(byte[] data);
        
        /// <summary>
        /// Categorizes error based on message and exception type
        /// </summary>
        ErrorCategory CategorizeError(string errorMessage, Exception? exception = null);
        
        /// <summary>
        /// Validates file for R2R compliance
        /// </summary>
        Task<(bool IsValid, List<string> Errors)> ValidateFileForR2RAsync(string filePath, string contentType);
    }

    public class R2RComplianceService : IR2RComplianceService
    {
        private readonly ILogger<R2RComplianceService> _logger;
        
        // R2R supported file types
        private static readonly HashSet<string> SupportedContentTypes = new()
        {
            "application/pdf",
            "text/plain",
            "text/markdown",
            "text/html",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "text/csv",
            "image/png",
            "image/jpeg",
            "image/bmp",
            "image/tiff",
            "audio/mpeg"
        };

        public R2RComplianceService(ILogger<R2RComplianceService> logger)
        {
            _logger = logger;
        }

        public string GenerateR2RCompliantFilename(string originalFileName, Guid documentId)
        {
            try
            {
                var extension = Path.GetExtension(originalFileName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                
                // ✅ R2R Best Practice: timestamp_documentId_originalName.ext
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var shortDocId = documentId.ToString("N")[..8]; // First 8 chars for brevity
                
                // Sanitize original name
                var sanitizedName = SanitizeFileName(nameWithoutExtension);
                
                return $"{timestamp}_{shortDocId}_{sanitizedName}{extension}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating R2R compliant filename for {OriginalFileName}", originalFileName);
                return originalFileName; // Fallback to original
            }
        }

        public async Task<string> ComputeChecksumAsync(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = await Task.Run(() => md5.ComputeHash(stream));
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing checksum for file {FilePath}", filePath);
                throw;
            }
        }

        public string ComputeChecksum(byte[] data)
        {
            try
            {
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(data);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing checksum for byte array");
                throw;
            }
        }

        public ErrorCategory CategorizeError(string errorMessage, Exception? exception = null)
        {
            var message = errorMessage?.ToLowerInvariant() ?? "";
            
            // Rate limiting errors
            if (message.Contains("rate limit") || message.Contains("too many requests") || 
                message.Contains("429") || exception is HttpRequestException { Data: var data } && 
                data.Contains("429"))
            {
                return ErrorCategory.RateLimit;
            }
            
            // Transient errors
            if (message.Contains("timeout") || message.Contains("connection") || 
                message.Contains("network") || message.Contains("502") || 
                message.Contains("503") || message.Contains("504") ||
                exception is TaskCanceledException or TimeoutException or HttpRequestException)
            {
                return ErrorCategory.Transient;
            }
            
            // Authentication errors
            if (message.Contains("unauthorized") || message.Contains("forbidden") || 
                message.Contains("401") || message.Contains("403"))
            {
                return ErrorCategory.Authentication;
            }
            
            // Validation errors
            if (message.Contains("validation") || message.Contains("invalid") || 
                message.Contains("bad request") || message.Contains("400"))
            {
                return ErrorCategory.Validation;
            }
            
            // File format errors
            if (message.Contains("unsupported") || message.Contains("format") || 
                message.Contains("file type"))
            {
                return ErrorCategory.FileFormat;
            }
            
            // File size errors
            if (message.Contains("too large") || message.Contains("size") || 
                message.Contains("413"))
            {
                return ErrorCategory.FileSize;
            }
            
            // Default to permanent for unknown errors
            return ErrorCategory.Permanent;
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateFileForR2RAsync(string filePath, string contentType)
        {
            var errors = new List<string>();
            
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    errors.Add("File does not exist");
                    return (false, errors);
                }
                
                // Check file size (R2R has limits)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    errors.Add("File is empty");
                }
                else if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit
                {
                    errors.Add("File is too large (max 100MB for optimal processing)");
                }
                
                // Check content type
                if (!SupportedContentTypes.Contains(contentType.ToLowerInvariant()))
                {
                    errors.Add($"Unsupported content type: {contentType}");
                }
                
                // Additional validation for specific file types
                if (contentType.StartsWith("image/"))
                {
                    await ValidateImageFileAsync(filePath, errors);
                }
                else if (contentType == "application/pdf")
                {
                    await ValidatePdfFileAsync(filePath, errors);
                }
                
                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file {FilePath}", filePath);
                errors.Add($"Validation error: {ex.Message}");
                return (false, errors);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters and limit length
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');
            
            // Limit length to 50 characters
            if (sanitized.Length > 50)
            {
                sanitized = sanitized[..50];
            }
            
            return sanitized;
        }

        private async Task ValidateImageFileAsync(string filePath, List<string> errors)
        {
            try
            {
                // Basic image validation - check if file starts with valid image headers
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, 8);
                
                // Check for common image signatures
                var isValidImage = buffer.Take(4).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }) || // PNG
                                  buffer.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }) ||        // JPEG
                                  buffer.Take(2).SequenceEqual(new byte[] { 0x42, 0x4D });               // BMP
                
                if (!isValidImage)
                {
                    errors.Add("File does not appear to be a valid image");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not validate image file {FilePath}", filePath);
                errors.Add("Could not validate image format");
            }
        }

        private async Task ValidatePdfFileAsync(string filePath, List<string> errors)
        {
            try
            {
                // Basic PDF validation - check for PDF header
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[5];
                await stream.ReadAsync(buffer, 0, 5);
                
                var pdfHeader = Encoding.ASCII.GetString(buffer);
                if (!pdfHeader.StartsWith("%PDF-"))
                {
                    errors.Add("File does not appear to be a valid PDF");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not validate PDF file {FilePath}", filePath);
                errors.Add("Could not validate PDF format");
            }
        }
    }
}
