# 🚀 HƯỚNG DẪN TRIỂN KHAI HỆ THỐNG QUOTA & PAYMENT

## 📋 TÓM TẮT

Hệ thống quota và payment đã được triển khai với các tính năng:

### **Gói FREE**

- Reading: 20 bài/tháng
- Listening: 20 bài/tháng
- Speaking: Bị chặn (yêu cầu Premium)
- Writing: Bị chặn (yêu cầu Premium)

### **Gói PREMIUM**

- Unlimited tất cả 4 kĩ năng
- AI Scoring Speaking/Writing
- AI Generate từ vựng

---

## 🔧 SETUP BACKEND

### 1. Chạy Migration Database

```bash
cd lumina_backend\DataLayer
dotnet ef migrations add AddQuotaTracking --startup-project ..\lumina\lumina.csproj
dotnet ef database update --startup-project ..\lumina\lumina.csproj
```

### 2. Cấu hình PayOS

#### a. Đăng ký tài khoản PayOS

- Truy cập: https://payos.vn
- Đăng ký tài khoản merchant
- Lấy thông tin API từ Dashboard

#### b. Cập nhật `appsettings.json`

```json
"PayOS": {
  "ApiKey": "your-actual-api-key",
  "ChecksumKey": "your-actual-checksum-key",
  "ClientId": "your-actual-client-id",
  "ReturnUrl": "http://localhost:4200/payment/success",
  "CancelUrl": "http://localhost:4200/payment/cancel"
}
```

### 3. Tạo Package trong Database

Chạy SQL để tạo gói Premium:

```sql
INSERT INTO Packages (PackageName, Price, DurationInDays, IsActive)
VALUES ('Premium Monthly', 299000, 30, 1);

-- Lấy PackageId vừa tạo
SELECT * FROM Packages;
```

### 4. Test Backend APIs

```bash
# Check quota (phải login trước)
GET http://localhost:5000/api/Quota/check/speaking
Authorization: Bearer {your-jwt-token}

# Expected Response for FREE user:
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

## 🎨 SETUP FRONTEND

### 1. Cập nhật Package ID trong Upgrade Modal

File: `lumina_frontend/lumina/src/app/Views/User/upgrade-modal/upgrade-modal.component.ts`

```typescript
premiumPackage = {
  id: 1, // ✅ Thay bằng PackageId thực tế từ database
  name: "Premium Monthly",
  price: 299000,
  // ...
};
```

### 2. Apply Quota Guard vào Routes

File: `lumina_frontend/lumina/src/app/app.routes.ts`

```typescript
import { QuotaGuard } from "./guards/quota.guard";

export const routes: Routes = [
  // ... existing routes
  {
    path: "speaking-exam",
    component: SpeakingComponent,
    canActivate: [QuotaGuard],
    data: { skill: "speaking" }, // ⬅️ Quan trọng!
  },
  {
    path: "writing-exam",
    component: WritingComponent,
    canActivate: [QuotaGuard],
    data: { skill: "writing" },
  },
  {
    path: "reading-exam",
    component: ReadingComponent,
    canActivate: [QuotaGuard],
    data: { skill: "reading" },
  },
  {
    path: "listening-exam",
    component: ListeningComponent,
    canActivate: [QuotaGuard],
    data: { skill: "listening" },
  },
];
```

### 3. Thêm Upgrade Modal vào Layout

File: `app.component.ts` hoặc parent component

```typescript
import { UpgradeModalComponent } from './Views/User/upgrade-modal/upgrade-modal.component';

@Component({
  // ...
  imports: [UpgradeModalComponent, ...],
  template: `
    <router-outlet></router-outlet>
    <app-upgrade-modal
      [isVisible]="showUpgradeModal"
      [skill]="currentSkill"
      (close)="showUpgradeModal = false">
    </app-upgrade-modal>
  `
})
export class AppComponent {
  showUpgradeModal = false;
  currentSkill = '';
}
```

### 4. Tích hợp Quota Check vào Exam Start

File: `speaking.component.ts` (hoặc exam components)

```typescript
import { QuotaService } from "../../Services/Quota/quota.service";

export class SpeakingComponent implements OnInit {
  constructor(private quotaService: QuotaService) {}

  async ngOnInit() {
    // Check quota before starting exam
    this.quotaService.checkQuota("speaking").subscribe({
      next: (result) => {
        if (!result.canAccess) {
          // Show upgrade modal or redirect
          this.router.navigate(["/upgrade"]);
        }
      },
    });
  }

  async finishExam() {
    // ... existing code ...

    // Increment quota after completion
    await this.quotaService.incrementQuota("speaking").toPromise();
  }
}
```

---

## 🎯 TESTING FLOW

### Test Case 1: FREE User - Reading Exam

**Setup:**

```sql
-- Reset user quota
UPDATE Users SET MonthlyReadingAttempts = 0 WHERE UserId = 1;
```

**Test:**

1. Login as FREE user
2. Vào trang Reading exam lần 1-19: ✅ Pass
3. Vào trang Reading exam lần 20: ✅ Pass (last free attempt)
4. Vào trang Reading exam lần 21: ❌ Blocked + show quota exhausted

**Expected:**

- API `/Quota/check/reading` returns `canAccess: false`
- User không vào được exam page
- Hiển thị thông báo "Đã hết lượt"

### Test Case 2: FREE User - Speaking Exam

**Test:**

1. Login as FREE user
2. Click vào Speaking exam

**Expected:**

- QuotaGuard blocks navigation
- API returns `requiresUpgrade: true`
- Hiển thị Upgrade Modal
- Redirect to `/upgrade` page

### Test Case 3: Premium Payment Flow

**Test:**

1. Click "Nâng cấp Premium" button
2. API call: `POST /api/Payment/create-link`
3. Redirect to PayOS checkout page
4. Complete payment on PayOS
5. PayOS webhook calls backend
6. Subscription activated

**Verify:**

```sql
-- Check subscription status
SELECT * FROM Subscriptions WHERE UserId = 1 AND Status = 'Active';

-- Check payment record
SELECT * FROM Payments WHERE UserId = 1 ORDER BY PaymentDate DESC;
```

### Test Case 4: PREMIUM User - All Access

**Setup:**

```sql
-- Create active subscription
INSERT INTO Subscriptions (UserId, PackageId, PaymentId, StartTime, EndTime, Status)
VALUES (1, 1, 1, GETDATE(), DATEADD(day, 30, GETDATE()), 'Active');
```

**Test:**

1. Login as PREMIUM user
2. Access all 4 skills: Reading, Listening, Speaking, Writing

**Expected:**

- All `/Quota/check/{skill}` return `canAccess: true, isPremium: true`
- No restrictions
- Quota counters NOT incremented (unlimited)

---

## 🔄 MONTHLY QUOTA RESET

### Option 1: Hangfire Background Job (Recommended)

Install Hangfire:

```bash
dotnet add package Hangfire
dotnet add package Hangfire.SqlServer
```

Configure in `Program.cs`:

```csharp
using Hangfire;

// Add Hangfire services
builder.Services.AddHangfire(config => config
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Schedule monthly reset
RecurringJob.AddOrUpdate<IQuotaService>(
    "reset-monthly-quotas",
    service => service.ResetMonthlyQuotaAsync(),
    "0 0 1 * *" // Run at 00:00 on day 1 of every month
);
```

### Option 2: Manual SQL Script

Run this script on the 1st of each month:

```sql
UPDATE Users
SET MonthlyReadingAttempts = 0,
    MonthlyListeningAttempts = 0,
    LastQuotaReset = GETDATE();
```

---

## 🐛 TROUBLESHOOTING

### Error: "User not authenticated" in QuotaController

**Fix:** Check JWT token claims

```csharp
// In your JWT generation, ensure UserId is included:
var claims = new[]
{
    new Claim("UserId", user.UserId.ToString()), // ⬅️ Important
    new Claim(ClaimTypes.Email, user.Email),
    // ...
};
```

### Error: "PayOS API error: 401"

**Fix:** Verify PayOS credentials in `appsettings.json`

- Check ApiKey, ChecksumKey, ClientId are correct
- Test in PayOS sandbox environment first

### Error: Quota not incrementing

**Fix:** Ensure `incrementQuota` is called AFTER exam completion

```typescript
async finishExam() {
  await this.submitAnswers(); // ✅ Submit first
  await this.quotaService.incrementQuota('speaking').toPromise(); // ✅ Then increment
}
```

### Error: Upgrade Modal not showing

**Fix:** Check if component is imported

```typescript
// In app.component.ts or parent
imports: [
  UpgradeModalComponent, // ⬅️ Add this
  CommonModule,
  RouterOutlet,
];
```

---

## 📊 MONITORING

### Track Quota Usage

```sql
-- Top users by quota usage
SELECT
    UserId,
    FullName,
    MonthlyReadingAttempts,
    MonthlyListeningAttempts,
    LastQuotaReset
FROM Users
WHERE MonthlyReadingAttempts > 15
   OR MonthlyListeningAttempts > 15
ORDER BY (MonthlyReadingAttempts + MonthlyListeningAttempts) DESC;
```

### Track Payment Success Rate

```sql
-- Payment conversion rate
SELECT
    COUNT(*) as TotalPayments,
    SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) as SuccessfulPayments,
    CAST(SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as SuccessRate
FROM Payments
WHERE PaymentDate >= DATEADD(month, -1, GETDATE());
```

### Active Subscriptions

```sql
-- Current active premium users
SELECT COUNT(*) as ActivePremiumUsers
FROM Subscriptions
WHERE Status = 'Active'
  AND EndTime > GETDATE();
```

---

## ✅ DEPLOYMENT CHECKLIST

### Pre-Production

- [ ] Test all quota limits (0, 19, 20, 21 attempts)
- [ ] Test payment flow end-to-end
- [ ] Test webhook signature validation
- [ ] Verify database indexes are created
- [ ] Test monthly quota reset job

### Production

- [ ] Update PayOS credentials (production keys)
- [ ] Set production URLs in `appsettings.json`
- [ ] Configure PayOS webhook URL in dashboard
- [ ] Enable Hangfire dashboard authentication
- [ ] Monitor payment logs for 24h after launch
- [ ] Set up alerts for failed payments

### Security

- [ ] Never commit PayOS keys to git
- [ ] Use environment variables for secrets
- [ ] Validate webhook signatures
- [ ] Rate limit payment API endpoints
- [ ] Log all payment transactions

---

## 🎉 DONE!

Hệ thống quota và payment đã sẵn sàng triển khai!

**Next Steps:**

1. Chạy migration database
2. Đăng ký PayOS và lấy credentials
3. Test payment flow trong sandbox
4. Deploy và monitor

**Support:**

- PayOS Docs: https://payos.vn/docs
- Issues: Contact dev team
