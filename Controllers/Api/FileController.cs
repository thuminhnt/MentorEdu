using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorEdu.DTOs.Common;

namespace MentorEdu.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private const long MaxFileSize = 20 * 1024 * 1024; // 20MB
        private const int MaxFiles = 2;
        private readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };

        public FileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Upload certification files for mentor application (max 20MB, 2 files)
        /// </summary>
        [HttpPost("upload/certifications")]
        [AllowAnonymous] // Allow during registration
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB total limit
        public async Task<ActionResult<ApiResponse<UploadResult>>> UploadCertifications([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("No files provided"));

            if (files.Count > MaxFiles)
                return BadRequest(ApiResponse<UploadResult>.FailResponse($"Maximum {MaxFiles} files allowed"));

            var uploadedUrls = new List<string>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    errors.Add($"{file.FileName}: File size exceeds 20MB limit");
                    continue;
                }

                // Validate extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    errors.Add($"{file.FileName}: Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");
                    continue;
                }

                try
                {
                    // Create upload directory
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "certifications");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Generate URL
                    var fileUrl = $"/uploads/certifications/{uniqueFileName}";
                    uploadedUrls.Add(fileUrl);
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: Upload failed - {ex.Message}");
                }
            }

            if (uploadedUrls.Count == 0)
            {
                return BadRequest(ApiResponse<UploadResult>.FailResponse(
                    $"No files were uploaded. Errors: {string.Join("; ", errors)}"
                ));
            }

            var result = new UploadResult
            {
                FileUrls = uploadedUrls,
                Errors = errors
            };

            return Ok(ApiResponse<UploadResult>.SuccessResponse(result, 
                $"{uploadedUrls.Count} file(s) uploaded successfully"));
        }

        /// <summary>
        /// Upload avatar/profile picture (max 5MB)
        /// </summary>
        [HttpPost("upload/avatar")]
        [Authorize]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<UploadResult>>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("No file provided"));

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("File size exceeds 5MB limit"));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            
            if (!imageExtensions.Contains(extension))
                return BadRequest(ApiResponse<UploadResult>.FailResponse("Only image files are allowed"));

            try
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/avatars/{uniqueFileName}";

                var result = new UploadResult
                {
                    FileUrls = new List<string> { fileUrl }
                };

                return Ok(ApiResponse<UploadResult>.SuccessResponse(result, "Avatar uploaded successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UploadResult>.FailResponse($"Upload failed: {ex.Message}"));
            }
        }
    }

    public class UploadResult
    {
        public List<string> FileUrls { get; set; } = new();
        public List<string>? Errors { get; set; }
    }
}
