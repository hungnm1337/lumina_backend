# 🐛 Debug Guide: Speaking 0 điểm & Không hiển thị kết quả

## 📋 Checklist Debug

### 1️⃣ **Kiểm tra attemptId có được truyền đúng không**

Mở DevTools Console, chạy lệnh sau **TRƯỚC KHI** nộp bài:

```javascript
// Kiểm tra localStorage
console.log("localStorage:", localStorage.getItem("currentExamAttempt"));

// Kiểm tra component state (nếu có access)
// Hoặc xem trong Console logs: [Speaking] ✅ Loaded attemptId: XXX
```

**Expected:**

```
localStorage: {"attemptID":156,"userID":4,"examID":4,...}
[Speaking] ✅ Loaded attemptId: 156
```

**Nếu thấy:**

- ❌ `localStorage: null` → Bug #12 chưa hoạt động
- ❌ `attemptId: 0` → Bị reset về 0 đâu đó

---

### 2️⃣ **Kiểm tra request Submit có đúng không**

Khi click "Nộp bài", mở **DevTools > Network**:

1. Filter: `speaking`
2. Tìm request `POST /api/Speaking/submit`
3. Click vào request → **Payload tab**

**Expected Payload:**

```json
{
  "questionId": 77,
  "audioFile": Blob,
  "attemptId": 156  // ← PHẢI LÀ SỐ DƯƠNG, KHÔNG PHẢI 0
}
```

**Nếu thấy:**

- ❌ `attemptId: 0` → Component truyền sai
- ❌ `attemptId: null` → Chưa được set

---

### 3️⃣ **Kiểm tra Backend Response**

Trong Network tab, click vào request → **Response tab**

**Expected Response (Success):**

```json
{
  "transcript": "Hello, my name is...",
  "overallScore": 75.3,
  "pronunciationScore": 80.5,
  "accuracyScore": 85.2,
  "fluencyScore": 70.8,
  "grammarScore": 78.5,
  "vocabularyScore": 72.1,
  "contentScore": 68.9,
  "savedAudioUrl": "https://..."
}
```

**Nếu thấy:**

- ❌ **403 Forbidden** → attemptId ownership validation failed
- ❌ **404 Not Found** → attemptId không tồn tại trong DB
- ❌ **500 Internal Server Error** → Backend lỗi (check logs)
- ❌ `overallScore: 0` → Azure/NLP service failed

---

### 4️⃣ **Kiểm tra Frontend nhận kết quả**

Mở Console, tìm logs:

**Expected Logs:**

```
[SpeakingAnswerBox] 🔍 DEBUG attemptId: {attemptId: 156, type: "number", ...}
[SpeakingAnswerBox] Submitting answer for question 77 with attemptId: 156
[SpeakingComponent] 📊 Received scoring result: {overallScore: 75.3, ...}
[SpeakingComponent] ✅ Updated results: {totalResults: 1, mapSize: 1}
[SpeakingComponent] 📈 Score calculated: {earnedScore: 7.53, roundedScore: 7.53, totalScore: 7.53}
```

**Nếu thấy:**

- ❌ Không có log `📊 Received scoring result` → Event không được emit/nhận
- ❌ `overallScore: undefined` → Backend không trả về đúng format
- ❌ `totalResults: 0` → Không lưu vào array

---

### 5️⃣ **Kiểm tra hiển thị kết quả chi tiết**

Sau khi nộp bài, kiểm tra:

```javascript
// Trong Console
console.log("speakingQuestionResults:", this.speakingQuestionResults);
console.log("speakingResults Map:", this.speakingResults);
```

**Hoặc** kiểm tra UI:

- Có hiển thị điểm tổng không? (ở góc trên)
- Có hiển thị icon ✅/❌ ở navigation dots không?

**Nếu không thấy:**

- ❌ Kiểm tra `speaking.component.html` có render `speakingQuestionResults` không
- ❌ Kiểm tra CSS có ẩn element không

---

## 🔧 **Các Fix Nhanh**

### Fix #1: attemptId = 0 hoặc null

**File:** `speaking.component.html` line 47

```html
<!-- ❌ SAI -->
[attemptId]="attemptId ?? 0"

<!-- ✅ ĐÚNG -->
[attemptId]="attemptId ?? null"
```

Sau đó thêm validation trong `speaking-answer-box.component.ts`:

```typescript
if (!this.attemptId || this.attemptId <= 0) {
  this.toastService.error("Lỗi: Không tìm thấy ID bài thi.");
  return;
}
```

---

### Fix #2: localStorage bị xóa giữa chừng

Kiểm tra có code nào gọi `localStorage.removeItem('currentExamAttempt')` **NGOÀI** cleanup method không:

```bash
# Search trong codebase
grep -r "removeItem.*currentExamAttempt" lumina_frontend/
```

**Chỉ nên xóa khi:**

- User click "Hoàn thành" exam
- User click "Thoát" exam
- KHÔNG nên xóa khi đang làm bài

---

### Fix #3: Backend trả về 0 điểm

Check Backend logs (Visual Studio Debug Console):

```
[Speaking] Transcript result: [Không nhận diện được giọng nói]
[Speaking] Azure transcription failed after retries
```

**Nguyên nhân:**

1. Audio quality quá kém → Azure không nhận diện được
2. Network timeout → Cloudinary upload failed
3. NLP service down

**Test:**

- Nói TO RÕ VÀO MIC
- Kiểm tra mic permission
- Kiểm tra file audio có upload lên Cloudinary không (check URL trong response)

---

## 📊 **Test Case Đầy Đủ**

### Test 1: Happy Path

1. ✅ Login → Chọn Speaking exam
2. ✅ localStorage có `currentExamAttempt`
3. ✅ Ghi âm câu 1 (10s, nói rõ)
4. ✅ Click "Nộp bài"
5. ✅ Thấy loading... (state = processing)
6. ✅ Sau 5-10s: Thấy điểm (ví dụ: 7.5/10)
7. ✅ Click "Câu tiếp theo" → Thấy icon ✅ ở câu 1
8. ✅ Làm hết 11 câu → Click "Hoàn thành"
9. ✅ Thấy trang summary với điểm chi tiết

### Test 2: Edge Cases

1. ❌ Ghi âm 0s → Click "Nộp bài" → Alert "Không có bản ghi âm"
2. ❌ Tắt mạng → Click "Nộp bài" → Alert "Mất kết nối mạng"
3. ❌ Spam click "Nộp bài" → Chỉ 1 request
4. ❌ Refresh page giữa chừng → attemptId vẫn còn
5. ❌ Navigate trực tiếp `/part/123` → Tự tạo attempt

---

## 🎯 **Root Cause Analysis**

Dựa vào screenshot của bạn:

```
[Speaking] ✅ Loaded attemptId: 156
localStorage.getItem('currentExamAttempt') → null
```

→ **LocalStorage bị XÓA SAU KHI component load!**

**Nghi ngờ:**

1. Có component khác xóa localStorage
2. Code cleanup được gọi sai chỗ
3. Browser auto-clear localStorage (ít khả năng)

**Debug:**

```javascript
// Thêm vào speaking.component.ts ngOnInit
window.addEventListener("storage", (e) => {
  if (e.key === "currentExamAttempt") {
    console.error("🚨 localStorage changed:", e);
    console.trace("Stack trace");
  }
});
```

Cái này sẽ log ra **AI XÓA** localStorage!

---

## 📞 **Next Steps**

1. **Chạy debug logs mới** (đã thêm ở trên)
2. **Ghi lại output** từ Console
3. **Chụp ảnh Network tab** (request/response)
4. **Báo lại kết quả** để tôi phân tích tiếp

Có thể vấn đề nằm ở:

- attemptId bị reset về 0
- Event binding bị lỗi
- Backend validation quá strict
- hoặc đơn giản là **scoreWeight = 0** trong database 😅
