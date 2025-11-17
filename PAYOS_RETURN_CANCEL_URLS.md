# 🔗 PAYOS RETURN & CANCEL URLS - GIẢI THÍCH

## 🎯 ReturnUrl vs CancelUrl

### ✅ `ReturnUrl` - URL Thành Công

**Khi nào dùng:** User hoàn tất thanh toán thành công trên PayOS

**Flow:**

```
User click "Nâng cấp Premium"
    ↓
Redirect đến PayOS checkout page
    ↓
User nhập thông tin thẻ/QR/banking
    ↓
Thanh toán THÀNH CÔNG ✅
    ↓
PayOS redirect về: ReturnUrl
    ↓
Hiển thị: Payment Success Page 🎉
```

**Config của bạn:**

```json
"ReturnUrl": "http://localhost:4200/payment/success"
```

**Component:** `PaymentSuccessComponent`

- Hiển thị thông báo thành công
- List premium features vừa mở khóa
- Button "Bắt đầu luyện tập"

---

### ❌ `CancelUrl` - URL Hủy

**Khi nào dùng:** User hủy thanh toán trên PayOS (click "Quay lại" hoặc đóng popup)

**Flow:**

```
User click "Nâng cấp Premium"
    ↓
Redirect đến PayOS checkout page
    ↓
User click "Hủy" hoặc đóng tab ❌
    ↓
PayOS redirect về: CancelUrl
    ↓
Hiển thị: Payment Cancel Page
```

**Config của bạn:**

```json
"CancelUrl": "http://localhost:4200/payment/cancel"
```

**Component:** `PaymentCancelComponent`

- Hiển thị "Đã hủy thanh toán"
- Button "Thử lại" → Quay về upgrade page
- Button "Quay về exams"

---

## 🔄 Complete Payment Flow

```
┌─────────────────────────────────────────────────────────┐
│  1. User Click "Nâng cấp Premium" Button                │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  2. Frontend gọi API: POST /api/Payment/create-link     │
│     Request: { packageId: 1, amount: 299000 }           │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  3. Backend tạo Payment Link với PayOS                  │
│     Response: { checkoutUrl: "https://pay.payos..." }   │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  4. Redirect user đến PayOS Checkout Page               │
│     window.location.href = checkoutUrl                  │
└─────────────────────────────────────────────────────────┘
                        ↓
         ┌──────────────┴──────────────┐
         │                              │
    ✅ SUCCESS                     ❌ CANCEL
         │                              │
         ↓                              ↓
┌────────────────────┐      ┌─────────────────────┐
│ PayOS redirects to │      │ PayOS redirects to  │
│ ReturnUrl          │      │ CancelUrl           │
│                    │      │                     │
│ /payment/success   │      │ /payment/cancel     │
└────────────────────┘      └─────────────────────┘
         │                              │
         ↓                              ↓
┌────────────────────┐      ┌─────────────────────┐
│ Show Success Page: │      │ Show Cancel Page:   │
│ - "Thành công!" 🎉 │      │ - "Đã hủy"          │
│ - Premium features │      │ - "Thử lại?"        │
│ - Start practicing │      │                     │
└────────────────────┘      └─────────────────────┘
         │
         ↓
┌─────────────────────────────────────────────────────────┐
│  5. PayOS Webhook gọi Backend (background)              │
│     POST /api/Payment/webhook                           │
│     Payload: { status: "PAID", orderCode: "..." }       │
└─────────────────────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────────────────────┐
│  6. Backend Auto-Activate Subscription                  │
│     - Create Payment record                             │
│     - Create Subscription record (Status = "Active")    │
└─────────────────────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────────────────────┐
│  7. User Now Has PREMIUM Access! 🎊                     │
│     - Unlimited all skills                              │
│     - AI Scoring enabled                                │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 Development vs Production URLs

### ✅ Development (Hiện tại của bạn)

```json
"ReturnUrl": "http://localhost:4200/payment/success",
"CancelUrl": "http://localhost:4200/payment/cancel"
```

- Dùng khi test trên máy local
- Port 4200 (Angular default)

### 🚀 Production (Khi deploy)

```json
"ReturnUrl": "https://lumina-toeic.com/payment/success",
"CancelUrl": "https://lumina-toeic.com/payment/cancel"
```

- Thay bằng domain thật của bạn
- Phải là HTTPS (bắt buộc)

---

## 🧪 Test URLs

### Test Success Flow

1. Vào upgrade page
2. Click "Nâng cấp Premium"
3. Trên PayOS, complete payment
4. Sẽ redirect về: `http://localhost:4200/payment/success`
5. Verify: Hiển thị success page đẹp ✅

### Test Cancel Flow

1. Vào upgrade page
2. Click "Nâng cấp Premium"
3. Trên PayOS, click "Hủy" hoặc đóng tab
4. Sẽ redirect về: `http://localhost:4200/payment/cancel`
5. Verify: Hiển thị cancel page ✅

---

## ⚙️ PayOS Dashboard Configuration

**Quan trọng:** Bạn cũng cần config URLs trong PayOS Dashboard!

1. Login vào: https://my.payos.vn
2. Vào **Settings** → **Webhook & Return URL**
3. Nhập:
   - **Return URL:** `http://localhost:4200/payment/success`
   - **Cancel URL:** `http://localhost:4200/payment/cancel`
   - **Webhook URL:** `https://your-backend-url.com/api/Payment/webhook`

**Lưu ý:**

- Webhook URL phải là public URL (không dùng localhost)
- Có thể dùng ngrok cho test: `https://abc123.ngrok.io/api/Payment/webhook`

---

## 🔒 Security Notes

### ReturnUrl

- ✅ **An toàn:** Chỉ để hiển thị UI thành công
- ⚠️ **Không tin tưởng:** User có thể tự navigate đến URL này
- ✅ **Giải pháp:** Subscription thật sự được activate qua **Webhook**, không phải ReturnUrl

### Webhook (Thật sự quan trọng!)

```
ReturnUrl        → Chỉ để UX (show success page)
Webhook          → Thật sự activate subscription ⭐
```

**Flow đúng:**

1. User thấy success page (ReturnUrl) → UI thôi
2. Webhook chạy background → Activate subscription thật
3. User reload page → Thấy Premium features

---

## 📊 Monitoring

### Check if URLs work

```sql
-- Sau khi test payment, check database
SELECT
    p.PaymentId,
    u.Email,
    p.Status,
    p.CreatedAt,
    s.Status as SubscriptionStatus
FROM Payments p
JOIN Users u ON p.UserId = u.UserId
LEFT JOIN Subscriptions s ON p.PaymentId = s.PaymentId
ORDER BY p.CreatedAt DESC;
```

**Expected khi thành công:**

- Payment.Status = "Completed"
- Subscription.Status = "Active"
- User redirect về `/payment/success` ✅

---

## ✅ Checklist

- [x] ReturnUrl configured: `http://localhost:4200/payment/success`
- [x] CancelUrl configured: `http://localhost:4200/payment/cancel`
- [x] PaymentSuccessComponent created
- [x] PaymentCancelComponent created
- [x] Routes added to app.routes.ts
- [ ] Test success flow
- [ ] Test cancel flow
- [ ] Configure URLs in PayOS Dashboard
- [ ] Update URLs for production deployment

---

## 🎉 Kết Luận

**Config của bạn đã ĐÚNG 100%!** ✅

Chỉ cần:

1. Test payment flow
2. Verify redirect về đúng pages
3. Khi deploy production → Đổi sang HTTPS URLs

**Ready to accept payments!** 💰
