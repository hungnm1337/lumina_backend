# API TESTING SCRIPT - QUOTA & PAYMENT

## Prerequisite: Lấy JWT Token

```bash
# Login để lấy token
curl -X POST http://localhost:5000/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-email@example.com",
    "password": "your-password"
  }'

# Copy token từ response
# TOKEN=<your-jwt-token-here>
```

---

## Test 1: Check Quota - Reading (FREE user)

**Request:**

```bash
curl -X GET "http://localhost:5000/api/Quota/check/reading" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response (FREE user, chưa làm bài):**

```json
{
  "canAccess": true,
  "isPremium": false,
  "requiresUpgrade": false,
  "remainingAttempts": 20,
  "subscriptionType": "FREE",
  "message": "Có thể làm bài"
}
```

---

## Test 2: Check Quota - Speaking (FREE user)

**Request:**

```bash
curl -X GET "http://localhost:5000/api/Quota/check/speaking" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response:**

```json
{
  "canAccess": false,
  "isPremium": false,
  "requiresUpgrade": true,
  "remainingAttempts": 0,
  "subscriptionType": "FREE",
  "message": "Nâng cấp lên Premium để truy cập SPEAKING"
}
```

---

## Test 3: Increment Quota

**Request:**

```bash
curl -X POST "http://localhost:5000/api/Quota/increment/reading" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response:**

```json
{
  "message": "Quota incremented for reading"
}
```

**Verify:**

```bash
# Check quota again - should be 19 remaining
curl -X GET "http://localhost:5000/api/Quota/check/reading" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## Test 4: Create Payment Link

**Request:**

```bash
curl -X POST "http://localhost:5000/api/Payment/create-link" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "packageId": 1,
    "amount": 299000
  }'
```

**Expected Response:**

```json
{
  "checkoutUrl": "https://pay.payos.vn/web/...",
  "qrCode": "data:image/png;base64,...",
  "orderCode": "1-1-1731600000"
}
```

---

## Test 5: Get Subscription Status

**Request:**

```bash
curl -X GET "http://localhost:5000/api/Payment/subscription-status" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response (FREE user):**

```json
{
  "hasActiveSubscription": false,
  "subscriptionType": "FREE"
}
```

**Expected Response (PREMIUM user):**

```json
{
  "hasActiveSubscription": true,
  "subscriptionType": "PREMIUM",
  "startDate": "2025-11-15T10:00:00Z",
  "endDate": "2025-12-15T10:00:00Z",
  "packageId": 1
}
```

---

## Test 6: Simulate Premium User

**Setup:**

```sql
-- Tạo payment record
INSERT INTO Payments (UserId, PackageId, Amount, PaymentGatewayTransactionId, Status, CreatedAt)
VALUES (1, 1, 299000, 'TEST-TRANSACTION-123', 'Completed', GETDATE());

-- Lấy PaymentId vừa tạo
DECLARE @PaymentId INT = SCOPE_IDENTITY();

-- Tạo subscription
INSERT INTO Subscriptions (UserId, PackageId, PaymentId, StartTime, EndTime, Status)
VALUES (1, 1, @PaymentId, GETDATE(), DATEADD(day, 30, GETDATE()), 'Active');
```

**Test:**

```bash
# Check quota - should be unlimited now
curl -X GET "http://localhost:5000/api/Quota/check/speaking" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response:**

```json
{
  "canAccess": true,
  "isPremium": true,
  "requiresUpgrade": false,
  "remainingAttempts": -1,
  "subscriptionType": "PREMIUM",
  "message": "Có thể làm bài"
}
```

---

## Test 7: Simulate Quota Exhaustion

**Setup:**

```sql
UPDATE Users
SET MonthlyReadingAttempts = 20
WHERE UserId = 1;
```

**Test:**

```bash
curl -X GET "http://localhost:5000/api/Quota/check/reading" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected Response:**

```json
{
  "canAccess": false,
  "isPremium": false,
  "requiresUpgrade": false,
  "remainingAttempts": 0,
  "subscriptionType": "FREE",
  "message": "Đã hết lượt. Còn 0 lượt trong tháng này."
}
```

---

## Test 8: Admin - Reset All Quotas

**Request:**

```bash
curl -X POST "http://localhost:5000/api/Quota/reset-all" \
  -H "Authorization: Bearer ADMIN_TOKEN_HERE"
```

**Expected Response:**

```json
{
  "message": "All quotas reset successfully"
}
```

**Verify:**

```sql
SELECT UserId, MonthlyReadingAttempts, MonthlyListeningAttempts, LastQuotaReset
FROM Users;

-- All should be 0 and LastQuotaReset should be current date
```

---

## Error Cases

### Test: Unauthorized Access

```bash
curl -X GET "http://localhost:5000/api/Quota/check/reading"
# No Authorization header
```

**Expected:**

```json
{
  "message": "User not authenticated"
}
```

### Test: Invalid Package

```bash
curl -X POST "http://localhost:5000/api/Payment/create-link" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "packageId": 999,
    "amount": 299000
  }'
```

**Expected:**

```json
{
  "message": "Package not found"
}
```

### Test: Invalid Skill Type

```bash
curl -X GET "http://localhost:5000/api/Quota/check/invalid-skill" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected:**

```json
{
  "canAccess": false,
  "isPremium": false,
  "requiresUpgrade": false,
  "remainingAttempts": 0,
  "subscriptionType": "FREE",
  "message": "..."
}
```

---

## Postman Collection

Import this JSON into Postman:

```json
{
  "info": {
    "name": "Lumina Quota & Payment APIs",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Check Quota - Reading",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/Quota/check/reading",
          "host": ["{{baseUrl}}"],
          "path": ["api", "Quota", "check", "reading"]
        }
      }
    },
    {
      "name": "Create Payment Link",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"packageId\": 1,\n  \"amount\": 299000\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/Payment/create-link",
          "host": ["{{baseUrl}}"],
          "path": ["api", "Payment", "create-link"]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5000"
    },
    {
      "key": "token",
      "value": "YOUR_JWT_TOKEN_HERE"
    }
  ]
}
```

---

## Monitoring Queries

### Check Current Quota Usage

```sql
SELECT
    u.UserId,
    u.Email,
    u.MonthlyReadingAttempts,
    u.MonthlyListeningAttempts,
    u.LastQuotaReset,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM Subscriptions s
            WHERE s.UserId = u.UserId
              AND s.Status = 'Active'
              AND s.EndTime > GETDATE()
        ) THEN 'PREMIUM'
        ELSE 'FREE'
    END AS SubscriptionType
FROM Users u
ORDER BY u.UserId;
```

### Check All Active Subscriptions

```sql
SELECT
    s.SubscriptionId,
    u.Email,
    p.PackageName,
    s.StartTime,
    s.EndTime,
    s.Status,
    DATEDIFF(day, GETDATE(), s.EndTime) AS DaysRemaining
FROM Subscriptions s
JOIN Users u ON s.UserId = u.UserId
JOIN Packages p ON s.PackageId = p.PackageId
WHERE s.Status = 'Active'
  AND s.EndTime > GETDATE()
ORDER BY s.EndTime DESC;
```

### Check Payment History

```sql
SELECT
    p.PaymentId,
    u.Email,
    pkg.PackageName,
    p.Amount,
    p.Status,
    p.CreatedAt,
    p.PaymentGatewayTransactionId
FROM Payments p
JOIN Users u ON p.UserId = u.UserId
JOIN Packages pkg ON p.PackageId = pkg.PackageId
ORDER BY p.CreatedAt DESC;
```

---

## ✅ Test Results Checklist

- [ ] Login successful, token received
- [ ] Reading quota check returns correct remaining attempts
- [ ] Speaking blocked for FREE users
- [ ] Increment quota works correctly
- [ ] Payment link created successfully
- [ ] Subscription status correct for FREE user
- [ ] Premium user has unlimited access
- [ ] Quota exhaustion blocked correctly
- [ ] Admin reset works
- [ ] Error cases handled properly

---

**All tests passed?** → System is ready! 🚀
