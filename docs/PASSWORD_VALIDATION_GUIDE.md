# Password Validation - Frontend & Backend

## Frontend Validation (UsersPage.jsx)

### 1. Thêm state cho validation errors

```javascript
const [passwordError, setPasswordError] = useState('');

// Validation function
const validatePassword = (password) => {
  if (!password || password.trim() === '') {
    return ''; // Empty is OK (means no change)
  }
  
  if (password.length < 6) {
    return 'Password must be at least 6 characters';
  }
  
  if (password.length > 100) {
    return 'Password must be less than 100 characters';
  }
  
  if (!/[A-Z]/.test(password)) {
    return 'Password must contain at least one uppercase letter';
  }
  
  if (!/[a-z]/.test(password)) {
    return 'Password must contain at least one lowercase letter';
  }
  
  if (!/[0-9]/.test(password)) {
    return 'Password must contain at least one number';
  }
  
  return ''; // Valid
};
```

### 2. Validate khi user nh?p password

```javascript
const handlePasswordChange = (e) => {
  const newPassword = e.target.value;
  setFormData({ ...formData, password: newPassword });
  
  // Real-time validation
  const error = validatePassword(newPassword);
  setPasswordError(error);
};
```

### 3. Hi?n th? l?i trong form

```jsx
<div>
  <label className="block text-sm font-medium text-neutral-700 dark:text-neutral-300 mb-1">
    Password
  </label>
  <input
    type="password"
    value={formData.password}
    onChange={handlePasswordChange}
    className={`w-full px-4 py-2 bg-neutral-50 dark:bg-neutral-800 border 
      ${passwordError ? 'border-red-500' : 'border-neutral-200 dark:border-neutral-700'} 
      rounded-lg text-neutral-900 dark:text-white focus:outline-none 
      ${passwordError ? 'focus:border-red-500' : 'focus:border-blue-500'}`}
    placeholder="Leave blank to keep current password"
  />
  {passwordError && (
    <p className="text-xs text-red-500 mt-1">{passwordError}</p>
  )}
  <p className="text-xs text-neutral-500 mt-1">
    Minimum 6 characters with uppercase, lowercase, and number
  </p>
</div>
```

### 4. Block submit n?u password invalid

```javascript
const submitUpdate = async () => {
  // Validate before submit
  if (formData.password && formData.password.trim() !== '') {
    const error = validatePassword(formData.password);
    if (error) {
      setPasswordError(error);
      showToast(error, 'error');
      return; // Don't submit
    }
  }
  
  try {
    const updateData = { fullName: formData.fullName };
    
    if (formData.password && formData.password.trim() !== '') {
      updateData.password = formData.password;
    }
    
    await adminApi.updateUser(editingUser.id, updateData);
    showToast(`User "${formData.fullName}" has been updated successfully`, 'success');
    fetchUsers();
    setIsUpdateModalOpen(false);
  } catch (err) {
    console.error('Failed to update user', err);
    
    // Show backend validation errors
    const errorMsg = err.response?.data?.message || 
                     err.response?.data?.errors?.[0] || 
                     'Failed to update user';
    showToast(errorMsg, 'error');
  }
};
```

---

## Backend Validation (AdminService.cs)

Backend hi?n t?i **ch?a h? tr? ??i password** qua `AdminUpdateUserDto`. C?n b? sung:

### Option 1: Thêm password vào AdminUpdateUserDto

```csharp
public class AdminUpdateUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    // ? Add this
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public string? Password { get; set; } // Nullable - only update if provided

    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    // ...existing fields...
}
```

### Option 2: T?o endpoint riêng cho change password

```csharp
// DTO
public class AdminChangeUserPasswordDto
{
    [Required]
    public long UserId { get; set; }
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Password must contain uppercase, lowercase, and number")]
    public string NewPassword { get; set; } = string.Empty;
}

// Controller
[HttpPut("users/{userId}/password")]
public async Task<ActionResult<ApiResponse>> ChangeUserPassword(
    long userId, 
    [FromBody] AdminChangeUserPasswordDto request)
{
    if (!ModelState.IsValid)
        return BadRequest(ApiResponse.FailResponse("Invalid password format"));

    var result = await _adminService.AdminChangeUserPasswordAsync(userId, request.NewPassword);

    if (!result.Success)
        return StatusCode(result.StatusCode, ApiResponse.FailResponse(result.Message ?? ""));

    return Ok(ApiResponse.SuccessResponse(result.Message));
}

// Service
public async Task<ServiceResult> AdminChangeUserPasswordAsync(long userId, string newPassword)
{
    var user = await _unitOfWork.Users.GetByIdAsync(userId);
    
    if (user == null)
        return ServiceResult.NotFoundResult("User not found");

    if (user.Role == UserRole.Admin)
        return ServiceResult.FailResult("Cannot change admin password");

    user.PasswordHash = HashPassword(newPassword);
    user.UpdatedAt = DateTime.UtcNow;

    _unitOfWork.Users.Update(user);
    await _unitOfWork.SaveChangesAsync();

    return ServiceResult.SuccessResult("Password changed successfully");
}
```

---

## Tóm t?t

### Frontend (UsersPage.jsx)
? Real-time validation khi user nh?p  
? Hi?n th? l?i ngay d??i ô input (màu ??)  
? Block submit n?u password invalid  
? Show backend error message chi ti?t  

### Backend (C#)
? Validate format password (min length, uppercase, lowercase, number)  
? Return 400 v?i message rõ ràng n?u invalid  
? Hash password tr??c khi l?u DB  

### Password Rules (Recommended)
- **Minimum**: 6 characters
- **Must have**: 1 uppercase, 1 lowercase, 1 number
- **Optional**: 1 special character (@, #, $, %, etc.)
- **Max**: 100 characters

---

## Test Cases

```javascript
// ? Valid passwords
"Password1"
"MyPass123"
"Admin2024"

// ? Invalid passwords
"pass" // Too short (< 6)
"password" // No uppercase or number
"PASSWORD" // No lowercase or number
"Password" // No number
"12345678" // No letters
```

---

## Next Steps

1. **Implement frontend validation** - Add code above to UsersPage.jsx
2. **Add backend support** - Choose Option 1 or 2 above
3. **Test** - Try updating password with valid/invalid values
4. **Document** - Update API docs v?i password rules

B?n mu?n tôi implement code c? th? vào UsersPage.jsx và backend không?
