# ⚡ QUICK START - QUOTA & PAYMENT SYSTEM

## 🚀 TL;DR - Để Chạy Ngay

### Step 1: Database Migration (5 phút)

```bash
cd lumina_backend\DataLayer
dotnet ef database update --startup-project ..\lumina\lumina.csproj
```

### Step 2: Tạo Premium Package (2 phút)

```sql
INSERT INTO Packages (PackageName, Price, DurationInDays, IsActive)
VALUES ('Premium Monthly', 299000, 30, 1);
```

### Step 3: PayOS Setup (10 phút)

1. Đăng ký: https://payos.vn
2. Lấy credentials từ Dashboard
3. Update `appsettings.json`:

```json
"PayOS": {
  "ApiKey": "your-key",
  "ChecksumKey": "your-checksum",
  "ClientId": "your-client-id"
}
```

### Step 4: Frontend Routes (5 phút)

```typescript
// app.routes.ts
import { QuotaGuard } from './guards/quota.guard';

{
  path: 'speaking-exam',
  canActivate: [QuotaGuard],
  data: { skill: 'speaking' }
}
```

### Step 5: Test

```bash
# Start backend
cd lumina_backend\lumina
dotnet run

# Start frontend
cd lumina_frontend\lumina
npm start
```

✅ **DONE!** Hệ thống quota & payment hoạt động!

---

## 🎯 Cách Sử Dụng Nhanh

### Check Quota (Backend)

```csharp
var result = await _quotaService.CheckQuotaAsync(userId, "speaking");
if (!result.CanAccess) {
    // Block access
}
```

### Check Quota (Frontend)

```typescript
this.quotaService.checkQuota("speaking").subscribe((result) => {
  if (result.requiresUpgrade) {
    this.showUpgradeModal = true;
  }
});
```

### Increment Quota After Exam

```typescript
await this.quotaService.incrementQuota("speaking").toPromise();
```

### Create Payment Link

```typescript
this.paymentService
  .createPaymentLink(packageId, amount)
  .subscribe((response) => {
    window.location.href = response.checkoutUrl;
  });
```

---

## 🔍 Test Scenarios

### FREE User - Reading (Có Quota)

1. Login FREE user
2. Vào Reading exam ≤ 20 lần: ✅ OK
3. Lần 21: ❌ Blocked

### FREE User - Speaking (Không Quota)

1. Login FREE user
2. Click Speaking: ❌ Instant block + upgrade modal

### PREMIUM User

1. Login PREMIUM user
2. Access ALL skills: ✅ Unlimited

---

## 📊 Monitoring Queries

### Current Quota Usage

```sql
SELECT UserId, MonthlyReadingAttempts, MonthlyListeningAttempts
FROM Users
WHERE MonthlyReadingAttempts > 15;
```

### Active Subscriptions

```sql
SELECT COUNT(*) FROM Subscriptions
WHERE Status = 'Active' AND EndTime > GETDATE();
```

### Recent Payments

```sql
SELECT TOP 10 * FROM Payments
ORDER BY PaymentDate DESC;
```

---

## 🐛 Troubleshooting

| Problem            | Quick Fix                          |
| ------------------ | ---------------------------------- |
| Quota not updating | Check `incrementQuota()` is called |
| Payment fails      | Verify PayOS credentials           |
| Guard not working  | Check route `data: { skill }`      |
| Modal not showing  | Import `UpgradeModalComponent`     |

---

## 📚 Detailed Docs

- **Full Setup Guide:** `QUOTA_PAYMENT_SETUP_GUIDE.md`
- **Files List:** `QUOTA_PAYMENT_FILES.md`

---

**Questions?** Check the detailed guides above!
