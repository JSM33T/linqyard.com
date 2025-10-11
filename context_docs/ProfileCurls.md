# Profile API - cURL Examples

This document provides cURL examples for all ProfileController endpoints in the Linqyard API.

## Base URL
```
https://localhost:7001/profile
```

## Authentication
All profile endpoints require JWT authentication. Include the JWT token in the Authorization header:
```bash
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

---

## 📋 Profile Management

### Get Profile Details
Retrieve the current user's complete profile information.

```bash
curl -X GET "https://localhost:7001/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "id": "12345678-1234-1234-1234-123456789abc",
    "email": "user@example.com",
    "emailVerified": true,
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "bio": "Software developer passionate about clean code",
    "avatarUrl": "https://example.com/avatar.jpg",
    "timezone": "UTC",
    "locale": "en-US",
    "verifiedBadge": false,
    "createdAt": "2025-09-01T10:00:00Z",
    "updatedAt": "2025-09-20T15:30:00Z",
    "roles": ["user"]
  },
  "meta": null
}
```

---

### Update Profile
Update profile information (excluding password).

```bash
curl -X POST "https://localhost:7001/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "username": "johnsmith2024",
    "firstName": "John",
    "lastName": "Smith",
    "displayName": "Johnny",
    "bio": "Full-stack developer and tech enthusiast",
    "timezone": "America/New_York",
    "locale": "en-US"
  }'
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "message": "Profile updated successfully",
    "updatedAt": "2025-09-22T12:30:00Z",
    "profile": {
      "id": "12345678-1234-1234-1234-123456789abc",
      "email": "user@example.com",
      "emailVerified": true,
      "username": "johnsmith2024",
      "firstName": "John",
      "lastName": "Smith",
      "displayName": "Johnny",
      "bio": "Full-stack developer and tech enthusiast",
      "avatarUrl": "https://example.com/avatar.jpg",
      "timezone": "America/New_York",
      "locale": "en-US",
      "verifiedBadge": false,
      "createdAt": "2025-09-01T10:00:00Z",
      "updatedAt": "2025-09-22T12:30:00Z",
      "roles": ["user"]
    }
  },
  "meta": null
}
```

**Partial Update Examples:**
```bash
# Update only bio
curl -X POST "https://localhost:7001/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "bio": "Updated bio only"
  }'

# Update only username
curl -X POST "https://localhost:7001/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "username": "mynewusername"
  }'
```

---

### Change Password
Change the user's password (requires current password verification).

```bash
curl -X POST "https://localhost:7001/profile/password" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "currentPassword": "oldPassword123",
    "newPassword": "newSecurePassword456"
  }'
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "message": "Password changed successfully. Please log in again on other devices.",
    "changedAt": "2025-09-22T12:45:00Z"
  },
  "meta": null
}
```

**Note:** After password change, all existing refresh tokens are revoked, forcing re-login on other devices.

---

## � Username Validation Rules

When updating the username field, the following validation rules apply:

- **Minimum Length**: 3 characters
- **Maximum Length**: 30 characters
- **Allowed Characters**: Letters (a-Z), numbers (0-9), underscores (_), dots (.), and hyphens (-)
- **Uniqueness**: Username must be unique across all users (case-insensitive)

**Valid Username Examples:**
- `john_doe`
- `user123`
- `my.username`
- `user-name`

**Invalid Username Examples:**
- `ab` (too short)
- `user@domain.com` (contains @ symbol)
- `user name` (contains space)
- `existing_username` (already taken by another user)

---

## �🔐 Session Management

### Get All Sessions
Retrieve all active sessions for the current user.

```bash
curl -X GET "https://localhost:7001/profile/sessions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "sessions": [
      {
        "id": "87654321-4321-4321-4321-876543219abc",
        "authMethod": "EmailPassword",
        "ipAddress": "192.168.1.100",
        "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "createdAt": "2025-09-22T08:00:00Z",
        "lastSeenAt": "2025-09-22T12:00:00Z",
        "isCurrentSession": true
      },
      {
        "id": "11111111-2222-3333-4444-555555556666",
        "authMethod": "Google",
        "ipAddress": "10.0.0.50",
        "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)",
        "createdAt": "2025-09-20T14:30:00Z",
        "lastSeenAt": "2025-09-21T09:15:00Z",
        "isCurrentSession": false
      }
    ]
  },
  "meta": null
}
```

---

### Logout from Specific Session
Logout from a specific session by its ID.

```bash
curl -X POST "https://localhost:7001/profile/sessions/11111111-2222-3333-4444-555555556666/logout" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "message": "Session logged out successfully",
    "deletedAt": "2025-09-22T12:50:00Z"
  },
  "meta": null
}
```

---

### Logout from All Other Sessions
Logout from all sessions except the current one.

```bash
curl -X POST "https://localhost:7001/profile/sessions/logout-all" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)"
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "message": "Logged out from 3 other session(s) successfully",
    "deletedAt": "2025-09-22T12:55:00Z"
  },
  "meta": null
}
```

---

## ⚠️ Account Management

### Delete Account
Permanently delete the user account (requires password confirmation and exact confirmation text).

```bash
curl -X POST "https://localhost:7001/profile/delete" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $(uuidgen)" \
  -d '{
    "password": "currentPassword123",
    "confirmationText": "DELETE MY ACCOUNT"
  }'
```

**Expected Response (200 OK):**
```json
{
  "data": {
    "message": "Account deleted successfully",
    "deletedAt": "2025-09-22T13:00:00Z"
  },
  "meta": null
}
```

**Important Notes:**
- The `confirmationText` must be exactly: `DELETE MY ACCOUNT` (case-sensitive)
- This operation performs a soft delete, anonymizing user data while preserving audit trail
- All sessions and refresh tokens are immediately revoked
- The refresh token cookie is cleared

---

## 📝 Error Responses

### 400 Bad Request

**Field validation error:**
```json
{
  "type": "about:blank",
  "title": "Bad request",
  "status": 400,
  "detail": "Display name cannot exceed 50 characters",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

**Username validation errors:**
```json
{
  "type": "about:blank",
  "title": "Bad request",
  "status": 400,
  "detail": "Username must be at least 3 characters long",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

```json
{
  "type": "about:blank",
  "title": "Bad request",
  "status": 400,
  "detail": "Username can only contain letters, numbers, underscores, dots, and hyphens",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

```json
{
  "type": "about:blank",
  "title": "Bad request",
  "status": 400,
  "detail": "Username is already taken",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

### 401 Unauthorized
```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid user context",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

### 404 Not Found
```json
{
  "type": "about:blank",
  "title": "Resource not found",
  "status": 404,
  "detail": "User not found",
  "instance": "/profile",
  "correlationId": "abc123-def456-ghi789"
}
```

---

## 🔧 Testing Tips

### Generate UUID for Correlation ID (Linux/macOS)
```bash
uuidgen
```

### Generate UUID for Correlation ID (Windows PowerShell)
```powershell
[System.Guid]::NewGuid().ToString()
```

### Environment Variables
You can set environment variables to make testing easier:

```bash
export BASE_URL="https://localhost:7001"
export JWT_TOKEN="your_jwt_token_here"
export CORRELATION_ID=$(uuidgen)

# Then use in cURL commands:
curl -X GET "$BASE_URL/profile" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: $CORRELATION_ID"
```

### Windows PowerShell Variables
```powershell
$BASE_URL = "https://localhost:7001"
$JWT_TOKEN = "your_jwt_token_here"
$CORRELATION_ID = [System.Guid]::NewGuid().ToString()

# Then use in cURL commands:
curl -X GET "$BASE_URL/profile" `
  -H "Authorization: Bearer $JWT_TOKEN" `
  -H "Content-Type: application/json" `
  -H "X-Correlation-Id: $CORRELATION_ID"
```

---

##  Quick Test Sequence

Here's a typical testing sequence:

1. **Get profile details** to see current state
2. **Update profile** with new information
3. **Get sessions** to see active devices
4. **Logout from specific session** (if multiple exist)
5. **Change password** (optional)
6. **Delete account** (destructive - use with caution)

Remember to obtain a valid JWT token through the auth endpoints first!