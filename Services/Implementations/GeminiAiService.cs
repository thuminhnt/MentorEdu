using MentorEdu.DTOs.User;
using MentorEdu.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace MentorEdu.Services.Implementations
{
    public class GeminiAiService : IGeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAiService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _apiKey;
        private readonly int _autoApproveThreshold;
        private readonly int _autoRejectThreshold;
        private readonly bool _isEnabled;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
        private const string ModelName = "gemini-2.0-flash";
        private const string ChatModelName = "gemini-2.0-flash"; // Same model, quota saved via token limits

        // Supported file types for multimodal analysis
        private static readonly Dictionary<string, string> SupportedMimeTypes = new()
        {
            { ".pdf", "application/pdf" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" }
        };

        public GeminiAiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiAiService> logger,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? "";
            _autoApproveThreshold = _configuration.GetValue<int>("GeminiAI:AutoApproveThreshold", 80);
            _autoRejectThreshold = _configuration.GetValue<int>("GeminiAI:AutoRejectThreshold", 40);
            _isEnabled = _configuration.GetValue<bool>("GeminiAI:Enabled", false);
        }

        public bool IsConfigured()
        {
            return _isEnabled && !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_GEMINI_API_KEY_HERE";
        }

        public async Task<GeminiAnalysisResult> AnalyzeMentorApplicationAsync(
            long userId,
            ApplyAsMentorRequest request,
            string certificationFileUrls)
        {
            // Return default review result if not configured
            if (!IsConfigured())
            {
                _logger.LogWarning("Gemini AI is not configured. Returning default review result.");
                return CreateDefaultReviewResult("AI service not configured");
            }

            try
            {
                // Build prompt and load files for multimodal analysis
                var prompt = BuildAnalysisPrompt(userId, request, certificationFileUrls);
                var fileDataList = await LoadCertificationFilesAsync(certificationFileUrls);
                
                _logger.LogInformation("Loaded {FileCount} certification files for AI analysis", fileDataList.Count);
                
                var response = await CallGeminiMultimodalApiAsync(prompt, fileDataList);
                var result = ParseGeminiResponse(response);
                
                // Determine recommended action based on thresholds
                result.RecommendedAction = DetermineAction(result.ConfidenceScore);
                result.ModelVersion = ModelName;

                _logger.LogInformation(
                    "AI Analysis for user {UserId}: Score={Score}, Action={Action}",
                    userId, result.ConfidenceScore, result.RecommendedAction);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing mentor application for user {UserId}", userId);
                return CreateDefaultReviewResult($"AI analysis failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Load certification files from wwwroot and convert to base64 for multimodal analysis
        /// </summary>
        private async Task<List<(string MimeType, string Base64Data, string FileName)>> LoadCertificationFilesAsync(string certificationUrls)
        {
            var fileDataList = new List<(string MimeType, string Base64Data, string FileName)>();
            
            if (string.IsNullOrWhiteSpace(certificationUrls))
                return fileDataList;

            var urls = certificationUrls.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(u => u.Trim())
                                        .ToList();

            foreach (var url in urls)
            {
                try
                {
                    // Convert URL to file path (e.g., /uploads/certifications/file.pdf -> wwwroot/uploads/certifications/file.pdf)
                    var relativePath = url.TrimStart('/');
                    var filePath = Path.Combine(_environment.WebRootPath, relativePath);

                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("Certification file not found: {FilePath}", filePath);
                        continue;
                    }

                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (!SupportedMimeTypes.TryGetValue(extension, out var mimeType))
                    {
                        _logger.LogWarning("Unsupported file type for AI analysis: {Extension}", extension);
                        continue;
                    }

                    // Read file and convert to base64
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var base64Data = Convert.ToBase64String(fileBytes);
                    var fileName = Path.GetFileName(filePath);

                    fileDataList.Add((mimeType, base64Data, fileName));
                    _logger.LogInformation("Loaded file for AI analysis: {FileName} ({MimeType}, {Size} bytes)", 
                        fileName, mimeType, fileBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading certification file: {Url}", url);
                }
            }

            return fileDataList;
        }

        private string BuildAnalysisPrompt(long userId, ApplyAsMentorRequest request, string certificationUrls)
        {
            var categoryIdsList = request.CategoryIds != null ? string.Join(", ", request.CategoryIds) : "None";
            var hashtagIdsList = request.HashtagIds != null ? string.Join(", ", request.HashtagIds) : "None";

            return $@"You are an AI assistant helping to review mentor applications for an educational mentoring platform.

Analyze the following mentor application and provide a JSON response with your assessment.

APPLICATION DETAILS:
- User ID: {userId}
- Specialization: {request.Specialization}
- Years of Experience: {request.ExperienceYears}
- Introduction: {request.Introduction}
- Portfolio URL: {request.PortfolioUrl}
- Application Note: {request.ApplicationNote ?? "None provided"}
- Category IDs: {categoryIdsList}
- Hashtag IDs: {hashtagIdsList}

ATTACHED FILES:
The applicant has uploaded certification/CV files which are attached to this request. Please analyze the content of these files carefully:
- Check if documents are relevant (CV, certifications, degrees, etc.)
- Verify consistency with stated experience and specialization
- Look for professional qualifications, education, work history
- Note any red flags (fake documents, irrelevant content, low quality)

EVALUATION CRITERIA:
1. Experience Level (0-25 points): Consider years of experience, relevance, and consistency with uploaded documents
2. Introduction Quality (0-25 points): Clarity, professionalism, passion for teaching
3. Portfolio & Documents (0-25 points): Quality and relevance of portfolio/credentials AND uploaded CV/certifications
4. Overall Fit (0-25 points): Alignment with mentoring role requirements

RESPONSE FORMAT (JSON only, no markdown):
{{
    ""confidenceScore"": <number 0-100>,
    ""reasoning"": ""<detailed explanation of your assessment including analysis of uploaded documents>"",
    ""keyPoints"": [""<positive point 1>"", ""<positive point 2>"", ...],
    ""redFlags"": [""<concern 1>"", ""<concern 2>"", ...] or []
}}

GUIDELINES:
- Score >= 80: Strong candidate with verified credentials, likely to be auto-approved
- Score 40-79: Needs manual review by admin
- Score < 40: Significant concerns (fake/irrelevant documents, mismatched info), likely to be auto-rejected
- Be objective and fair
- List specific strengths in keyPoints (include findings from documents)
- List any concerns in redFlags (empty array if none)
- If documents appear fake, irrelevant, or low quality, reflect this in the score and red flags

Respond with ONLY the JSON object, no additional text.";
        }

        /// <summary>
        /// Call Gemini API with multimodal content (text + files)
        /// </summary>
        private async Task<string> CallGeminiMultimodalApiAsync(string prompt, List<(string MimeType, string Base64Data, string FileName)> files)
        {
            var requestUrl = $"{BaseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

            // Build parts array with text prompt first, then files
            var parts = new List<object>
            {
                new { text = prompt }
            };

            // Add file data as inline_data parts
            foreach (var file in files)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = file.MimeType,
                        data = file.Base64Data
                    }
                });
                _logger.LogInformation("Adding file to AI request: {FileName} ({MimeType})", file.FileName, file.MimeType);
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts.ToArray()
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 2048 // Increased for detailed document analysis
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending multimodal request to Gemini API with {FileCount} files", files.Count);

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }

            return responseContent;
        }

        // Keep old method for backward compatibility (text-only)
        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var requestUrl = $"{BaseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 1024
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }

            return responseContent;
        }

        private GeminiAnalysisResult ParseGeminiResponse(string response)
        {
            try
            {
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                // Navigate to the text content in Gemini response
                var candidates = root.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                var textPart = parts[0].GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(textPart))
                {
                    throw new Exception("Empty response from Gemini");
                }

                // Clean up the response (remove markdown code blocks if present)
                textPart = textPart.Trim();
                if (textPart.StartsWith("```json"))
                {
                    textPart = textPart.Substring(7);
                }
                if (textPart.StartsWith("```"))
                {
                    textPart = textPart.Substring(3);
                }
                if (textPart.EndsWith("```"))
                {
                    textPart = textPart.Substring(0, textPart.Length - 3);
                }
                textPart = textPart.Trim();

                // Parse the JSON response from Gemini
                using var analysisDoc = JsonDocument.Parse(textPart);
                var analysisRoot = analysisDoc.RootElement;

                var result = new GeminiAnalysisResult
                {
                    ConfidenceScore = analysisRoot.GetProperty("confidenceScore").GetDecimal(),
                    Reasoning = analysisRoot.GetProperty("reasoning").GetString() ?? "",
                    KeyPoints = new List<string>(),
                    RedFlags = new List<string>()
                };

                if (analysisRoot.TryGetProperty("keyPoints", out var keyPointsElement))
                {
                    foreach (var point in keyPointsElement.EnumerateArray())
                    {
                        var value = point.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            result.KeyPoints.Add(value);
                        }
                    }
                }

                if (analysisRoot.TryGetProperty("redFlags", out var redFlagsElement))
                {
                    foreach (var flag in redFlagsElement.EnumerateArray())
                    {
                        var value = flag.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            result.RedFlags.Add(value);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini response: {Response}", response);
                throw new Exception("Failed to parse AI response", ex);
            }
        }

        private string DetermineAction(decimal score)
        {
            if (score >= _autoApproveThreshold)
                return "AUTO_APPROVE";
            if (score < _autoRejectThreshold)
                return "AUTO_REJECT";
            return "MANUAL_REVIEW";
        }

        private GeminiAnalysisResult CreateDefaultReviewResult(string reason)
        {
            return new GeminiAnalysisResult
            {
                ConfidenceScore = 50,
                RecommendedAction = "MANUAL_REVIEW",
                Reasoning = reason,
                KeyPoints = new List<string>(),
                RedFlags = new List<string>(),
                ModelVersion = ModelName
            };
        }

        /// <summary>
        /// General AI chat method for chatbot functionality
        /// Uses the cheapest model (flash-lite) with limited tokens to minimize quota usage
        /// </summary>
        public async Task<string> GeneralAIChatAsync(string userMessage, List<ChatMessage>? conversationHistory = null)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("Gemini AI is not configured");
            }

            try
            {
                // Use the cheapest model for chatbot
                var requestUrl = $"{BaseUrl}/models/{ChatModelName}:generateContent?key={_apiKey}";

                // Truncate user input to save tokens
                const int maxInputLength = 500;
                if (userMessage.Length > maxInputLength)
                {
                    userMessage = userMessage.Substring(0, maxInputLength) + "...";
                }

                // Build contents array with conversation history
                var contents = new List<object>();

                // Short system prompt to save tokens
                var systemPrompt = @"You are an AI assistant for MentorEdu, an educational mentoring platform. Help users with finding mentors, booking sessions, posting requests, community features, and profile management. Reply in the user's language. Be concise.";

                // Add conversation history (truncate each message to save tokens)
                const int maxHistoryContentLength = 200;
                if (conversationHistory != null && conversationHistory.Count > 0)
                {
                    foreach (var msg in conversationHistory)
                    {
                        var text = msg.Content.Length > maxHistoryContentLength
                            ? msg.Content.Substring(0, maxHistoryContentLength) + "..."
                            : msg.Content;
                        contents.Add(new
                        {
                            role = msg.Role == "assistant" ? "model" : "user",
                            parts = new[] { new { text } }
                        });
                    }
                }

                // Add current user message
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = contents.ToArray(),
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 512
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);

                // Single request - NO retries to preserve free-tier quota
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse response
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    var candidates = root.GetProperty("candidates");
                    var firstCandidate = candidates[0];
                    var contentObj = firstCandidate.GetProperty("content");
                    var parts = contentObj.GetProperty("parts");
                    var textPart = parts[0].GetProperty("text").GetString();

                    return textPart ?? "Sorry, I could not generate a response. Please try again.";
                }

                _logger.LogError("Gemini API error: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new Exception("RATE_LIMITED: AI service is temporarily overloaded. Please try again in a few moments.");
                }

                throw new Exception($"Gemini API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GeneralAIChatAsync");
                throw;
            }
        }
    }
}
