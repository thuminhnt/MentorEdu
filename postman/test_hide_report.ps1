#!/usr/bin/env pwsh
# ============================================================
#  TEST SCRIPT: Report / Hide / Unhide Community Post
#  Chạy sau khi PostgreSQL và server đã up
#  Usage: .\test_hide_report.ps1
# ============================================================

$BASE = "https://localhost:7082/api"
$ProgressPreference = 'SilentlyContinue'
# Bỏ qua SSL tự ký khi dev
if ([System.Net.ServicePointManager]::SecurityProtocol) {
    add-type @"
using System.Net;using System.Security.Cryptography.X509Certificates;
public class TrustAll : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp,X509Certificate cert,WebRequest req,int problem){return true;}
}
"@ -ErrorAction SilentlyContinue
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll
}

function Print-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Print-OK($msg)   { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Print-FAIL($msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Print-Info($msg) { Write-Host "  $msg" -ForegroundColor Gray }

function Invoke-API($method, $url, $body, $token) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    try {
        $params = @{ Method = $method; Uri = $url; Headers = $headers; SkipCertificateCheck = $true }
        if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 10) }
        return Invoke-RestMethod @params
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errBody = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
        return [PSCustomObject]@{ success = $false; message = ($errBody.message ?? $_.Exception.Message); statusCode = $statusCode }
    }
}

# ==============================================================
# STEP 1: Login admin
# ==============================================================
Print-Step "STEP 1: Login Admin (admin@mentor-edu.local / Admin123!)"
$res = Invoke-API "POST" "$BASE/auth/login" @{ email = "admin@mentor-edu.local"; password = "Admin123!" }
if (-not $res.data.accessToken) {
    Print-FAIL "Login admin thất bại: $($res.message)"
    exit 1
}
$adminToken = $res.data.accessToken
Print-OK "Admin token: $($adminToken.Substring(0,30))..."

# ==============================================================
# STEP 2: Login student (để tạo report)
# ==============================================================
Print-Step "STEP 2: Login Student (student@mentor-edu.local / Student123!)"
$res = Invoke-API "POST" "$BASE/auth/login" @{ email = "student@mentor-edu.local"; password = "Student123!" }
if (-not $res.data.accessToken) {
    Print-FAIL "Login student thất bại: $($res.message)"
    exit 1
}
$studentToken = $res.data.accessToken
$studentId    = $res.data.userId
Print-OK "Student token OK, userId=$studentId"

# ==============================================================
# STEP 3: Admin lấy danh sách posts
# ==============================================================
Print-Step "STEP 3: Admin lấy danh sách posts"
$res = Invoke-API "GET" "$BASE/admin/posts?pageSize=5" $null $adminToken
if (-not $res.success) {
    Print-FAIL "Lấy posts thất bại: $($res.message)"
    exit 1
}
$posts = $res.data.items
Print-OK "Tổng post: $($res.data.totalCount)"

if ($posts.Count -eq 0) {
    Print-FAIL "Không có post nào trong DB để test. Hãy seed dữ liệu trước."
    exit 1
}

$testPost = $posts[0]
$postId   = $testPost.id
Print-Info "Dùng post id=$postId, title='$($testPost.title)', isHidden=$($testPost.isHidden)"

# ==============================================================
# STEP 4: Student tạo Report trên người dùng liên quan tới post
# (CreateReportDto cầu ReportedUserId, Reason)
# ==============================================================
Print-Step "STEP 4: Student báo cáo tác giả của post (authorId=$($testPost.authorId))"
$res = Invoke-API "POST" "$BASE/users/reports" @{
    reportedUserId = $testPost.authorId
    reason         = "Nội dung vi phạm cộng đồng - test script"
    details        = "Post id=$postId có nội dung không phù hợp"
} $studentToken

if ($res.success -eq $false -and $res.statusCode -ne 201) {
    Print-FAIL "Tạo report thất bại: $($res.message)"
} else {
    Print-OK "Tạo report thành công"
    if ($res.data) { Print-Info "Report id=$($res.data.id), status=$($res.data.status)" }
}

# ==============================================================
# STEP 5: Admin xem danh sách reports
# ==============================================================
Print-Step "STEP 5: Admin xem reports"
$res = Invoke-API "GET" "$BASE/admin/reports?pageSize=5" $null $adminToken
if ($res.success -eq $false) {
    Print-FAIL "Lấy reports thất bại: $($res.message)"
} else {
    Print-OK "Tổng report: $($res.data.totalCount)"
    if ($res.data.items.Count -gt 0) {
        $latestReport = $res.data.items[0]
        Print-Info "Report mới nhất: id=$($latestReport.id), status=$($latestReport.status), reason=$($latestReport.reason)"
        $reportId = $latestReport.id
    }
}

# ==============================================================
# STEP 6: Admin HIDE post
# ==============================================================
Print-Step "STEP 6: Admin HIDE post id=$postId"
$res = Invoke-API "POST" "$BASE/admin/posts/$postId/hide" @{
    reason = "Bài viết bị báo cáo nhiều lần, ẩn để kiểm tra"
} $adminToken

if (-not $res.success) {
    Print-FAIL "Hide thất bại: $($res.message)"
} else {
    Print-OK "Hide thành công: $($res.message)"
}

# ==============================================================
# STEP 7: Verify post đã bị ẩn
# ==============================================================
Print-Step "STEP 7: Verify post isHidden=true"
$res = Invoke-API "GET" "$BASE/admin/posts?isHidden=true&pageSize=10" $null $adminToken
if ($res.success -eq $false) {
    Print-FAIL "Không query được: $($res.message)"
} else {
    $hiddenPost = $res.data.items | Where-Object { $_.id -eq $postId }
    if ($hiddenPost) {
        Print-OK "Post id=$postId xuất hiện trong danh sách isHidden=true ✓"
        Print-Info "isHidden=$($hiddenPost.isHidden)"
    } else {
        Print-FAIL "Post id=$postId KHÔNG xuất hiện trong filter isHidden=true"
    }
}

# ==============================================================
# STEP 8: Admin UNHIDE post
# ==============================================================
Print-Step "STEP 8: Admin UNHIDE post id=$postId (nhầm, khôi phục)"
$res = Invoke-API "POST" "$BASE/admin/posts/$postId/unhide" $null $adminToken

if (-not $res.success) {
    Print-FAIL "Unhide thất bại: $($res.message)"
} else {
    Print-OK "Unhide thành công: $($res.message)"
}

# ==============================================================
# STEP 9: Verify post đã được khôi phục
# ==============================================================
Print-Step "STEP 9: Verify post isHidden=false (khôi phục)"
$res = Invoke-API "GET" "$BASE/admin/posts?isHidden=false&pageSize=10" $null $adminToken
if ($res.success -eq $false) {
    Print-FAIL "Không query được: $($res.message)"
} else {
    $visiblePost = $res.data.items | Where-Object { $_.id -eq $postId }
    if ($visiblePost) {
        Print-OK "Post id=$postId xuất hiện trong danh sách isHidden=false ✓"
    } else {
        Print-FAIL "Post id=$postId KHÔNG xuất hiện trong filter isHidden=false"
    }
}

# ==============================================================
# STEP 10: Admin xử lý report (resolve)
# ==============================================================
if ($reportId) {
    Print-Step "STEP 10: Admin xử lý report id=$reportId → resolved"
    $res = Invoke-API "POST" "$BASE/admin/reports/$reportId/handle" @{
        status  = "resolved"
        note    = "Đã kiểm tra và ẩn post, xử lý xong"
        banUser = $false
    } $adminToken

    if (-not $res.success) {
        Print-FAIL "Handle report thất bại: $($res.message)"
    } else {
        Print-OK "Handle report thành công: $($res.message)"
    }
}

Write-Host "`n============================================" -ForegroundColor Yellow
Write-Host "  TEST COMPLETE" -ForegroundColor Yellow
Write-Host "============================================`n" -ForegroundColor Yellow
