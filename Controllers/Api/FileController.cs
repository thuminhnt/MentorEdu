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
        private const long MaxChatImageSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] ChatAllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx" };
        private const long MaxChatFileSize = 20 * 1024 * 1024; // 20MB
        private const int MaxChatFiles = 5;

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

        /// <summary>
        /// Upload chat image (max 10MB)
        /// </summary>
        [HttpPost("upload/chat-image")]
        [Authorize]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<UploadResult>>> UploadChatImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("No file provided"));

            if (file.Length > MaxChatImageSize)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("File size exceeds 10MB limit"));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!imageExtensions.Contains(extension))
                return BadRequest(ApiResponse<UploadResult>.FailResponse("Only image files are allowed"));

            try
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "chat-images");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/chat-images/{uniqueFileName}";

                var result = new UploadResult
                {
                    FileUrls = new List<string> { fileUrl }
                };

                return Ok(ApiResponse<UploadResult>.SuccessResponse(result, "Chat image uploaded successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UploadResult>.FailResponse($"Upload failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Upload chat files (images and documents) in a single request (max 20MB each, 5 files)
        /// </summary>
        [HttpPost("upload/chat-files")]
        [Authorize]
        [RequestSizeLimit(120 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<UploadResult>>> UploadChatFiles([FromForm] List<IFormFile> files)
        {
            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx",
                    ".ppt", ".pptx", ".txt", ".zip", ".rar", ".7z",
                    ".png", ".jpg", ".jpeg", ".gif", ".webp"
                };

            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
                    return BadRequest(ApiResponse<object>.FailResponse($"File type not allowed: {ext}"));
            }
            if (files == null || files.Count == 0)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("No files provided"));

            if (files.Count > MaxChatFiles)
                return BadRequest(ApiResponse<UploadResult>.FailResponse($"Maximum {MaxChatFiles} files allowed"));

            var uploadedUrls = new List<string>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length > MaxChatFileSize)
                {
                    errors.Add($"{file.FileName}: File size exceeds 20MB limit");
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!ChatAllowedExtensions.Contains(extension))
                {
                    errors.Add($"{file.FileName}: Invalid file type. Allowed: {string.Join(", ", ChatAllowedExtensions)}");
                    continue;
                }

                try
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "chat-files");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileUrl = $"/uploads/chat-files/{uniqueFileName}";
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
        /// Upload files for community posts (max 10MB each, 2 files)
        /// </summary>
        [HttpPost("upload/community-post")]
        [Authorize]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<UploadResult>>> UploadCommunityPostFiles([FromForm] List<IFormFile> files)
        {
            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx",
                ".png", ".jpg", ".jpeg", ".gif", ".webp"
            };

            if (files == null || files.Count == 0)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("No files provided"));

            if (files.Count > 2)
                return BadRequest(ApiResponse<UploadResult>.FailResponse("Maximum 2 files allowed"));

            var uploadedUrls = new List<string>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                // Validate size (10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    errors.Add($"{file.FileName}: File size exceeds 10MB limit");
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(extension))
                {
                    errors.Add($"{file.FileName}: Invalid file type. Allowed: pdf, doc, docx, png, jpg, jpeg, gif, webp");
                    continue;
                }

                try
                {
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "community-posts");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    // Keep original filename but add GUID prefix for uniqueness
                    var safeOriginalName = Path.GetFileNameWithoutExtension(file.FileName)
                        .Replace(" ", "_")
                        .Replace("/", "_")
                        .Replace("\\", "_");
                    var uniqueFileName = $"{Guid.NewGuid().ToString().Substring(0, 8)}_{safeOriginalName}{extension}";
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileUrl = $"/uploads/community-posts/{uniqueFileName}";
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

    }

    public class UploadResult
    {
        public List<string> FileUrls { get; set; } = new();
        public List<string>? Errors { get; set; }
    }
}
