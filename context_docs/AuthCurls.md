# Linqyard API - Authentication Endpoints (JWT)

## Setup
Set your host variable: 
```bash
dotnet=http://localhost:5000  # or dotnet=https://localhost:5001
```

## JWT Authentication
This API now uses JWT tokens for authentication. Login and refresh endpoints return:
- `accessToken`: JWT token for API authentication (15 min expiry)
- `refreshToken`: Long-lived token to get new access tokens (7-14 days)
- `expiresAt`: When the access token expires

Use the JWT token in Authorization header: `Authorization: Bearer <accessToken>`

## 1. User Registration
curl -X POST "{{dotnet}}/auth/register" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: reg-$(date +%s)" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePass123!",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe"
  }'

## 2. Email Verification
# Note: Replace the token with the actual verification token (normally sent via email)
curl -X POST "{{dotnet}}/auth/verify-email" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: verify-$(date +%s)" \
  -d '{
    "email": "john.doe@example.com",
    "token": "ABC12345"
  }'

## 3. User Login
curl -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: login-$(date +%s)" \
  -d '{
    "emailOrUsername": "john.doe@example.com",
    "password": "SecurePass123!",
    "deviceName": "My Laptop",
    "deviceType": "Desktop",
    "rememberMe": false
  }'

# Alternative: Login with username instead of email
curl -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: login-username-$(date +%s)" \
  -d '{
    "emailOrUsername": "johndoe",
    "password": "SecurePass123!",
    "deviceName": "My Laptop",
    "deviceType": "Desktop",
    "rememberMe": false
  }'

## 4. Refresh Access Token
# Note: Replace REFRESH_TOKEN with actual refresh token from login response
curl -X POST "{{dotnet}}/auth/refresh" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: refresh-$(date +%s)" \
  -d '{
    "refreshToken": "REFRESH_TOKEN_HERE"
  }'

## 5. User Logout
# Note: Replace REFRESH_TOKEN with actual refresh token
curl -X POST "{{dotnet}}/auth/logout" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: logout-$(date +%s)" \
  -d '{
    "refreshToken": "REFRESH_TOKEN_HERE"
  }'

## 6. Home/Health Endpoints
# Root endpoint - Server info
curl -X GET "{{dotnet}}/" \
  -H "X-Correlation-Id: info-$(date +%s)"

# Health check
curl -X GET "{{dotnet}}/health" \
  -H "X-Correlation-Id: health-$(date +%s)"

## Example Complete Flow:

### Step 1: Register new user
REGISTRATION_RESPONSE=$(curl -s -X POST "{{dotnet}}/auth/register" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: flow-reg-$(date +%s)" \
  -d '{
    "email": "testuser@example.com",
    "password": "TestPassword123!",
    "username": "testuser",
    "firstName": "Test",
    "lastName": "User"
  }')

echo "Registration Response:"
echo $REGISTRATION_RESPONSE | jq .

### Step 2: Verify email (normally you'd get token from email)
# For testing, you'd need to check the database or logs for the actual verification token
curl -X POST "{{dotnet}}/auth/verify-email" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: flow-verify-$(date +%s)" \
  -d '{
    "email": "testuser@example.com",
    "token": "TOKEN_FROM_DATABASE_OR_EMAIL"
  }'

### Step 3: Login after verification
LOGIN_RESPONSE=$(curl -s -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: flow-login-$(date +%s)" \
  -d '{
    "emailOrUsername": "testuser@example.com",
    "password": "TestPassword123!",
    "deviceName": "Test Device",
    "deviceType": "API Client",
    "rememberMe": false
  }')

echo "Login Response:"
echo $LOGIN_RESPONSE | jq .

# Extract tokens
ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')
REFRESH_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.refreshToken')

### Step 4: Use refresh token
REFRESH_RESPONSE=$(curl -s -X POST "{{dotnet}}/auth/refresh" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: flow-refresh-$(date +%s)" \
  -d "{
    \"refreshToken\": \"$REFRESH_TOKEN\"
  }")

echo "Refresh Response:"
echo $REFRESH_RESPONSE | jq .

### Step 5: Logout
LOGOUT_RESPONSE=$(curl -s -X POST "{{dotnet}}/auth/logout" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: flow-logout-$(date +%s)" \
  -d "{
    \"refreshToken\": \"$REFRESH_TOKEN\"
  }")

echo "Logout Response:"
echo $LOGOUT_RESPONSE | jq .

## Using JWT Tokens for Protected Endpoints
```bash
# Extract access token from login response
ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')

# Example: Use JWT token to access a protected endpoint (when implemented)
curl -X GET "{{dotnet}}/api/protected-resource" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "X-Correlation-Id: protected-$(date +%s)"

# When token expires, use refresh token to get a new one
REFRESH_RESPONSE=$(curl -s -X POST "{{dotnet}}/auth/refresh" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: auto-refresh-$(date +%s)" \
  -d "{\"refreshToken\": \"$REFRESH_TOKEN\"}")

# Extract new access token
NEW_ACCESS_TOKEN=$(echo $REFRESH_RESPONSE | jq -r '.data.accessToken')
```

## Expected Response Formats

### Login Response (JWT)
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123def456...",
    "expiresAt": "2024-01-01T12:15:00Z",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "testuser@example.com",
      "emailVerified": true,
      "username": "testuser",
      "firstName": "Test",
      "lastName": "User",
      "avatarUrl": null,
      "createdAt": "2024-01-01T12:00:00Z",
      "roles": ["user"]
    }
  },
  "success": true,
  "message": null,
  "errors": null
}
```

## Testing Error Cases:

### Try login with unverified email
curl -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-unverified-$(date +%s)" \
  -d '{
    "emailOrUsername": "unverified@example.com",
    "password": "password123"
  }'

### Try login with wrong password (using email)
curl -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-wrongpass-$(date +%s)" \
  -d '{
    "emailOrUsername": "testuser@example.com",
    "password": "wrongpassword"
  }'

### Try login with username and wrong password
curl -X POST "{{dotnet}}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-wrongpass-username-$(date +%s)" \
  -d '{
    "emailOrUsername": "testuser",
    "password": "wrongpassword"
  }'

### Try registration with invalid username
curl -X POST "{{dotnet}}/auth/register" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-invalidusername-$(date +%s)" \
  -d '{
    "email": "newuser@example.com",
    "password": "ValidPassword123!",
    "username": "ab"
  }'

### Try duplicate registration
curl -X POST "{{dotnet}}/auth/register" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-duplicate-$(date +%s)" \
  -d '{
    "email": "testuser@example.com",
    "password": "AnotherPassword123!",
    "username": "testuser2"
  }'

### Try invalid refresh token
curl -X POST "{{dotnet}}/auth/refresh" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: error-invalidtoken-$(date +%s)" \
  -d '{
    "refreshToken": "invalid_token_here"
  }'

## 7. Resend Email Verification
curl -X POST "{{dotnet}}/auth/resend-verification" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: resend-$(date +%s)" \
  -d '{
    "email": "john.doe@example.com"
  }'

## 8. Forgot Password (Send Reset Code)
# Note: Works for both verified and unverified emails (in case verification email failed)
curl -X POST "{{dotnet}}/auth/forgot-password" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: forgot-$(date +%s)" \
  -d '{
    "email": "john.doe@example.com"
  }'

## 9. Reset Password
# Note: Replace TOKEN_FROM_EMAIL with actual reset token from email
# This will automatically verify the email if it wasn't verified before
curl -X POST "{{dotnet}}/auth/reset-password" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: reset-$(date +%s)" \
  -d '{
    "email": "john.doe@example.com",
    "token": "TOKEN_FROM_EMAIL",
    "newPassword": "NewSecurePass123!"
  }'

## PowerShell Variables (for Windows):
# $dotnet = "http://localhost:5000"
# $dotnet = "https://localhost:5001"

## Bash Variables (for Linux/Mac):
# export dotnet="http://localhost:5000"
# export dotnet="https://localhost:5001"

## Usage with variables:
# Replace {{dotnet}} with $dotnet in the commands above
# Example: curl -X POST "$dotnet/auth/register" ...