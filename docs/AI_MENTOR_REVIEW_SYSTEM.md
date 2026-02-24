# 🤖 AI-Powered Mentor Application Review System

## 📋 Overview

Hệ thống tự động đánh giá đơn ứng tuyển mentor sử dụng Gemini AI của Google. AI sẽ phân tích đơn ứng tuyển và đưa ra quyết định tự động dựa trên confidence score, đồng thời admin vẫn có thể override quyết định của AI.

**Implemented:** February 2026  
**AI Model:** Google Gemini 1.5 Flash  
**Status:** ✅ Production Ready

---

## 🎯 Key Features

- ✅ **Tự động phân tích đơn ứng tuyển** bằng Gemini AI
- ✅ **Auto-approve** cho ứng viên xuất sắc (score ≥ 80%)
- ✅ **Auto-reject** cho ứng viên không đạt (score < 40%)
- ✅ **Manual review** cho các trường hợp trung bình (40-79%)
- ✅ **Admin override** - Admin có thể thay đổi quyết định AI
- ✅ **Detailed reasoning** - AI giải thích lý do đánh giá
- ✅ **Key points & red flags** - Highlight điểm mạnh và điểm yếu
- ✅ **Audit trail** - Lưu toàn bộ lịch sử và quyết định AI

---

## 📊 Evaluation Criteria

AI đánh giá đơn ứng tuyển theo **4 tiêu chí chính** (tổng 100 điểm):

### 1. **Experience Level** (0-25 điểm)
- Số năm kinh nghiệm (`ExperienceYears`)
- Mức độ liên quan với lĩnh vực chuyên môn
- **Điểm cao:** 3-5+ năm kinh nghiệm với chuyên môn rõ ràng
- **Điểm thấp:** < 1 năm hoặc không rõ ràng

### 2. **Introduction Quality** (0-25 điểm)
- Độ dài và chi tiết của phần giới thiệu
- Tính chuyên nghiệp, rõ ràng trong diễn đạt
- Thể hiện đam mê giảng dạy và hỗ trợ học viên
- **Điểm cao:** 150-300 từ, thể hiện achievements cụ thể
- **Điểm thấp:** 1-2 câu đơn giản, nhiều lỗi ngữ pháp

### 3. **Portfolio Quality** (0-25 điểm)
- Chất lượng portfolio link (GitHub/LinkedIn/Website)
- Tính liên quan của certification files
- **Điểm cao:** GitHub active (10+ repos), certifications chuyên nghiệp
- **Điểm thấp:** Link broken, portfolio trống, không có certifications

### 4. **Overall Fit** (0-25 điểm)
- Sự phù hợp tổng thể với vai trò mentor
- Tính nhất quán giữa các thông tin
- Categories/hashtags có match với expertise không
- **Điểm cao:** Thông tin nhất quán, categories chính xác
- **Điểm thấp:** Claim 10 years nhưng portfolio trống

---

## 🎚️ Score Ranges & Actions

| Score Range | Decision | Description | Auto Action |
|-------------|----------|-------------|-------------|
| **≥ 80%** | ✅ **Auto-Approve** | Ứng viên xuất sắc, đủ tiêu chuẩn | Tự động duyệt, gửi email thông báo |
| **40-79%** | ⏳ **Manual Review** | Cần admin xem xét thêm | Set status = Pending, chờ admin |
| **< 40%** | ❌ **Auto-Reject** | Không đạt yêu cầu tối thiểu | Tự động reject, gửi email thông báo |

---

## 🔄 Processing Flow

```
User submits application
         ↓
Upload certification files
         ↓
Call Gemini AI API
         ↓
AI analyzes application
    (4 criteria, 100 points)
         ↓
Calculate confidence score
         ↓
    ┌────┴────┐
    ↓         ↓         ↓
Score ≥80   40-79    <40
    ↓         ↓         ↓
Auto-     Manual   Auto-
Approve   Review   Reject
    ↓         ↓         ↓
Send      Wait     Send
Email     Admin    Email
```

### Detailed Steps:

1. **User submits**
   - Fill form: specialization, experience, introduction, portfolio
   - Upload certification files (PDF/JPG/PNG/DOC)
   - Select categories & hashtags

2. **Backend processing** (`MentorService.ApplyAsMentorAsync`)
   - Validate input data
   - Save files to storage
   - Call `GeminiAiService.AnalyzeMentorApplicationAsync`

3. **AI analysis** (`GeminiAiService`)
   - Build analysis prompt with all application data
   - Call Gemini API (`gemini-1.5-flash` model)
   - Parse JSON response: score, reasoning, keyPoints, redFlags
   - Determine recommended action based on thresholds

4. **Save result**
   - Create `MentorApplicationHistory` record with:
     - `Action` = 0 (Submitted)
     - `IsAiReviewed` = true
     - `AiConfidenceScore`, `AiReasoning`, `AiKeyPoints`, `AiRedFlags`
   - If score ≥ 80: Auto-approve (Action = 3)
   - If score < 40: Auto-reject (Action = 4)
   - If 40-79: Keep Pending

5. **Notification**
   - Send email to user about application status
   - Save notification to database

6. **Admin review** (if manual review required)
   - Admin views application with AI analysis
   - Can approve/reject with reason
   - AI data shown as reference, not binding

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE",
    "Enabled": true,
    "AutoApproveThreshold": 80,
    "AutoRejectThreshold": 40
  }
}
```

### User Secrets (Recommended for Production)

```bash
dotnet user-secrets set "GeminiAI:ApiKey" "YOUR_API_KEY"
```

### Environment Variables

```
GeminiAI__ApiKey=YOUR_API_KEY
GeminiAI__Enabled=true
GeminiAI__AutoApproveThreshold=80
GeminiAI__AutoRejectThreshold=40
```

### Configuration Options:

- **ApiKey**: Gemini API key (required)
- **Enabled**: Enable/disable AI review (default: false)
- **AutoApproveThreshold**: Min score for auto-approve (default: 80)
- **AutoRejectThreshold**: Max score for auto-reject (default: 40)

---

## 🏗️ Architecture

### Backend Components

#### 1. **GeminiAiService** (`Services/Implementations/GeminiAiService.cs`)
- Interface: `IGeminiAiService`
- Methods:
  - `IsConfigured()` - Check if API key is valid
  - `AnalyzeMentorApplicationAsync()` - Main analysis method
- Features:
  - Builds detailed prompt for AI
  - Calls Gemini API via HTTP
  - Parses JSON response
  - Handles errors gracefully

#### 2. **MentorService** (`Services/Implementations/MentorService.cs`)
- Updated `ApplyAsMentorAsync()` method
- Integrates AI analysis into application flow
- Auto-approve/reject logic
- Creates history records with AI data

#### 3. **AdminService** (`Services/Implementations/AdminService.cs`)
- `GetMentorApplicationDetailsAsync()` - Returns application with AI data
- Parses AI fields from `MentorApplicationHistory`

#### 4. **DTOs** (`DTOs/Admin/MentorApplicationDtos.cs`)
- `MentorApplicationDetailDto` with AI fields:
  - `IsAiReviewed`
  - `AiConfidenceScore`
  - `AiRecommendedAction`
  - `AiReasoning`
  - `AiKeyPoints`
  - `AiRedFlags`
  - `AiModelVersion`

### Frontend Components

#### 1. **ApplyMentorModal** (`my-app/src/components/mentor/ApplyMentorModal.jsx`)
- **New:** Collapsible "AI Evaluation Guidelines" section
- Shows:
  - Score ranges & outcomes
  - 4 evaluation criteria
  - Tips for high score (80%+)
  - Best file formats
  - Common red flags
- Enhanced form fields with real-time hints
- Visual feedback for good inputs

#### 2. **MentorApplicationsPage** (`my-app/src/pages/admin/MentorApplicationsPage.jsx`)
- Displays AI analysis in application details modal
- Shows:
  - 🤖 AI Analysis badge
  - Confidence score with progress bar
  - Recommended action badge
  - AI reasoning (purple highlight box)
  - ✅ Key strengths (green list)
  - ⚠️ Red flags (red list)
  - Model version

---

## 📝 Sample AI Prompt

```text
You are an AI assistant helping to review mentor applications for an educational mentoring platform.

Analyze the following mentor application and provide a JSON response with your assessment.

APPLICATION DETAILS:
- User ID: 123
- Specialization: Full-Stack Web Development
- Years of Experience: 7
- Introduction: I'm a passionate educator with 8 years of experience in web development...
- Portfolio URL: https://github.com/username
- Certification Files: /uploads/certifications/cert1.pdf, /uploads/certifications/cv.pdf
- Application Note: I've mentored 50+ junior developers at top tech companies.
- Category IDs: 1, 3, 5
- Hashtag IDs: 12, 15, 23

EVALUATION CRITERIA:
1. Experience Level (0-25 points): Consider years of experience and relevance
2. Introduction Quality (0-25 points): Clarity, professionalism, passion for teaching
3. Portfolio (0-25 points): Quality and relevance of portfolio/credentials
4. Overall Fit (0-25 points): Alignment with mentoring role requirements

RESPONSE FORMAT (JSON only, no markdown):
{
    "confidenceScore": <number 0-100>,
    "reasoning": "<detailed explanation of your assessment>",
    "keyPoints": ["<positive point 1>", "<positive point 2>", ...],
    "redFlags": ["<concern 1>", "<concern 2>", ...] or []
}

GUIDELINES:
- Score >= 80: Strong candidate, likely to be auto-approved
- Score 40-79: Needs manual review by admin
- Score < 40: Significant concerns, likely to be auto-rejected
- Be objective and fair
- List specific strengths in keyPoints
- List any concerns in redFlags (empty array if none)

Respond with ONLY the JSON object, no additional text.
```

---

## 📄 Sample AI Response

### ✅ Good Application (Score 85%)

```json
{
  "confidenceScore": 85,
  "reasoning": "Strong candidate with 7 years of professional experience in web development. The introduction clearly demonstrates teaching passion and quantified achievements (50+ mentored developers). Portfolio shows active GitHub presence with multiple projects. Categories align well with stated expertise.",
  "keyPoints": [
    "7 years of relevant experience in full-stack development",
    "Proven mentoring track record (50+ developers mentored)",
    "Professional portfolio with active GitHub contributions",
    "Clear, passionate teaching philosophy in introduction",
    "Appropriate category and skill selections"
  ],
  "redFlags": []
}
```

### ⚠️ Medium Application (Score 55%)

```json
{
  "confidenceScore": 55,
  "reasoning": "Candidate has 2 years of experience, which is below ideal threshold for mentoring. Introduction is adequate but lacks specific achievements or teaching philosophy. Portfolio link provided but limited content. Categories are appropriate.",
  "keyPoints": [
    "Relevant specialization in web development",
    "Basic portfolio presence",
    "Appropriate category selections"
  ],
  "redFlags": [
    "Only 2 years of experience (3+ recommended)",
    "Introduction lacks specific achievements or metrics",
    "Portfolio has limited content/projects",
    "No evidence of prior teaching or mentoring experience"
  ]
}
```

### ❌ Poor Application (Score 25%)

```json
{
  "confidenceScore": 25,
  "reasoning": "Application shows significant gaps. Less than 1 year experience is insufficient for mentoring role. Introduction is minimal and generic. Portfolio link is broken/invalid. No professional certifications provided.",
  "keyPoints": [
    "Clear specialization stated"
  ],
  "redFlags": [
    "Only 6 months of experience (insufficient for mentor role)",
    "Very brief introduction (only 1 sentence)",
    "Portfolio link is broken or returns 404 error",
    "No certifications or CV uploaded",
    "Introduction contains grammatical errors",
    "No evidence of professional experience or teaching ability"
  ]
}
```

---

## 🧪 Testing Guide

### 1. **Test Auto-Approve (Score ≥ 80)**

**Setup:**
- Experience: 5+ years
- Introduction: 200+ words, professional, with specific achievements
- Portfolio: Active GitHub (https://github.com/torvalds) or complete LinkedIn
- Files: Upload professional CV (PDF) + certifications

**Expected Result:**
- Status → Approved
- History action = 3 (Auto Approved)
- Email sent to user
- AI score ≥ 80 visible in admin panel

### 2. **Test Manual Review (Score 40-79)**

**Setup:**
- Experience: 2 years
- Introduction: 80 words, basic content
- Portfolio: Simple GitHub or LinkedIn
- Files: Basic CV only

**Expected Result:**
- Status → Pending
- History action = 0 (Submitted)
- AI score 40-79
- Admin sees "Manual Review Required" badge

### 3. **Test Auto-Reject (Score < 40)**

**Setup:**
- Experience: 0-6 months
- Introduction: 20 words, "I want to be mentor"
- Portfolio: Broken link or empty GitHub
- Files: Upload random image

**Expected Result:**
- Status → Rejected
- History action = 4 (Auto Rejected)
- Email sent to user
- AI score < 40 with red flags listed

### 4. **Test Admin Override**

**Setup:**
- Submit application that got auto-rejected (score 35%)

**Steps:**
1. Login as admin
2. View application details
3. See AI analysis (score 35%, auto-rejected)
4. Click "Approve" button (override AI decision)
5. Confirm

**Expected Result:**
- Status → Approved
- New history record with action = 1 (Approved by Admin)
- Original auto-reject record still visible in history

### 5. **Test AI Disabled**

**Setup:**
```json
"GeminiAI": {
  "Enabled": false
}
```

**Expected Result:**
- Application submitted → Pending (no auto decision)
- `IsAiReviewed` = false
- No AI fields populated
- Admin must manually review all applications

---

## 📊 Database Schema

### MentorApplicationHistory Table

```sql
CREATE TABLE MentorApplicationHistory (
    Id BIGINT PRIMARY KEY IDENTITY,
    UserId BIGINT NOT NULL,
    Action INT NOT NULL, -- 0=Submitted, 1=Approved, 2=Rejected, 3=AutoApproved, 4=AutoRejected
    Reason NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedById BIGINT NULL,
    
    -- AI Analysis Fields
    IsAiReviewed BIT NOT NULL DEFAULT 0,
    AiConfidenceScore INT NULL,
    AiRecommendedAction NVARCHAR(50) NULL, -- AUTO_APPROVE, AUTO_REJECT, MANUAL_REVIEW
    AiReasoning NVARCHAR(MAX) NULL,
    AiKeyPoints NVARCHAR(MAX) NULL, -- JSON array
    AiRedFlags NVARCHAR(MAX) NULL,  -- JSON array
    AiModelVersion NVARCHAR(100) NULL
);
```

---

## 🔧 Troubleshooting

### Issue: AI returns low scores for all applications

**Possible Causes:**
- Gemini API prompt may be too strict
- Test data không đủ chi tiết

**Solution:**
- Review prompt in `GeminiAiService.BuildAnalysisPrompt()`
- Ensure test applications have quality data
- Adjust thresholds in `appsettings.json`

---

### Issue: API calls failing

**Possible Causes:**
- Invalid API key
- API quota exceeded
- Network issues

**Solution:**
```csharp
// Check logs for error details
_logger.LogError("Gemini API error: {StatusCode} - {Response}", ...)

// Verify API key
dotnet user-secrets list

// Test API key manually
curl -X POST "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{"contents":[{"parts":[{"text":"test"}]}]}'
```

---

### Issue: AI analysis not showing in admin panel

**Possible Causes:**
- DTO mapping issue
- Frontend not parsing AI fields

**Solution:**
- Check `AdminService.GetMentorApplicationDetailsAsync()` DTO mapping
- Verify `selectedApp.isAiReviewed` in React component
- Check browser console for JS errors

---

### Issue: Files not being analyzed by AI

**Possible Causes:**
- AI cannot read file URLs (only metadata sent)
- Expected behavior: AI only sees file URLs, not content

**Note:**
- Current implementation sends file URLs to AI, not file content
- AI evaluates based on:
  - Presence of files (empty vs. uploaded)
  - File URLs/names (professional naming)
  - Other application data (experience, intro, portfolio)
- Future enhancement: OCR/text extraction from PDFs

---

## 🎨 UI/UX Guidelines

### User-facing Form (ApplyMentorModal)

**Design Principles:**
- ✅ **Transparent:** Show AI evaluation criteria upfront
- ✅ **Helpful:** Provide real-time hints and guidance
- ✅ **Educational:** Explain what makes a good application
- ✅ **Non-intrusive:** Collapsible guidelines, not overwhelming

**Key Elements:**
- 🤖 AI Guidelines banner (collapsible)
- Color-coded score ranges (green/yellow/red)
- Tips section with specific recommendations
- Real-time feedback (e.g., "3+ years recommended")
- File format recommendations

### Admin Panel (MentorApplicationsPage)

**Design Principles:**
- ✅ **Informative:** Show AI analysis prominently
- ✅ **Actionable:** Clear approve/reject buttons
- ✅ **Override-friendly:** Admin decision supersedes AI
- ✅ **Audit-ready:** All decisions logged with reasoning

**Key Elements:**
- 🤖 AI Analysis section with badge
- Progress bar for confidence score
- Recommended action badge (color-coded)
- AI reasoning in highlighted box
- Key points & red flags in lists
- Model version for traceability

---

## 📈 Metrics & Analytics

### Recommended Tracking

- **Auto-approval rate:** % of applications auto-approved
- **Auto-reject rate:** % of applications auto-rejected
- **Manual review rate:** % requiring admin intervention
- **Admin override rate:** % where admin disagrees with AI
- **Average confidence score:** Overall AI confidence level
- **Processing time:** Time from submission to decision

### SQL Queries

```sql
-- Auto-approval rate
SELECT 
    COUNT(CASE WHEN Action = 3 THEN 1 END) * 100.0 / COUNT(*) AS AutoApprovalRate
FROM MentorApplicationHistory
WHERE CreatedAt >= DATEADD(month, -1, GETDATE());

-- Average AI confidence score
SELECT AVG(AiConfidenceScore) AS AvgConfidence
FROM MentorApplicationHistory
WHERE IsAiReviewed = 1
  AND CreatedAt >= DATEADD(month, -1, GETDATE());

-- Admin override rate
SELECT 
    COUNT(*) AS OverrideCount
FROM MentorApplicationHistory h1
WHERE Action IN (1, 2) -- Manual approve/reject
  AND CreatedById IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM MentorApplicationHistory h2
      WHERE h2.UserId = h1.UserId
        AND h2.Action IN (3, 4) -- Auto decisions
        AND h2.CreatedAt < h1.CreatedAt
  );
```

---

## 🚀 Future Enhancements

### 1. **File Content Analysis**
- Extract text from PDF certifications using OCR
- Analyze CV content, education, work history
- Verify certificate authenticity

### 2. **Portfolio Deep Analysis**
- Scrape GitHub repos for code quality metrics
- Analyze LinkedIn profile completeness
- Check blog/website content quality

### 3. **Adaptive Thresholds**
- Machine learning to adjust thresholds based on outcomes
- Feedback loop: track mentor performance vs. AI score

### 4. **Multi-language Support**
- Support applications in Vietnamese
- Translate prompts and responses

### 5. **Explainability Dashboard**
- Visualize how each criterion contributed to score
- Show score breakdown (25% exp + 20% intro + ...)

### 6. **A/B Testing**
- Test different prompts and models
- Compare manual vs. AI approval outcomes

---

## 📞 Support & Maintainance

### API Key Management

**Rotation Schedule:** Every 90 days

**Process:**
1. Generate new API key in Google Cloud Console
2. Update User Secrets: `dotnet user-secrets set "GeminiAI:ApiKey" "NEW_KEY"`
3. Restart application
4. Monitor logs for successful API calls
5. Revoke old key after 24 hours

### Monitoring

**Critical Alerts:**
- API error rate > 5%
- Average confidence score < 50
- Auto-reject rate > 30%

**Log Locations:**
- Application logs: `logs/mentoredu-{date}.log`
- AI service logs: Filter by `GeminiAiService`
- API errors: Filter by `"Gemini API error"`

### Emergency Procedures

**If AI service fails:**
1. Set `GeminiAI:Enabled = false` in config
2. System falls back to 100% manual review
3. No existing applications affected
4. Fix AI service and re-enable

**If API quota exceeded:**
1. Check Google Cloud Console for quota limit
2. Request quota increase if needed
3. Temporarily disable AI review
4. Notify users of temporary manual review

---

## ✅ Checklist: Production Deployment

- [ ] API key stored in User Secrets (not in appsettings.json)
- [ ] `GeminiAI:Enabled = true` in production config
- [ ] Thresholds configured appropriately (80/40 or custom)
- [ ] Database migration applied (AI fields in MentorApplicationHistory)
- [ ] Frontend built and deployed with new UI
- [ ] Email templates updated for auto-approve/reject
- [ ] Monitoring alerts configured
- [ ] Admin users trained on new UI
- [ ] User documentation updated (AI guidelines visible in form)
- [ ] Test all 3 scenarios (auto-approve, manual, auto-reject)
- [ ] Backup & rollback plan in place

---

## 📚 References

- **Gemini API Docs:** https://ai.google.dev/docs
- **Model:** gemini-1.5-flash
- **Pricing:** https://ai.google.dev/pricing
- **Rate Limits:** 15 requests/minute (free tier)

---

## 📝 Changelog

### Version 1.0 - February 2026
- ✅ Initial implementation of AI review system
- ✅ Auto-approve/reject logic
- ✅ Admin UI with AI analysis display
- ✅ User-facing guidelines in application form
- ✅ Audit trail in database
- ✅ Email notifications for auto-decisions
- ✅ Configurable thresholds

---

**Document Version:** 1.0  
**Last Updated:** February 5, 2026  
**Author:** Development Team  
**Status:** Production Ready ✅
