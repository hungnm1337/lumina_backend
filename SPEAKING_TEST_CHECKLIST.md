# 🧪 Speaking Feature - Manual Test Checklist

## 🆕 Bug #12: LocalStorage currentExamAttempt = null khi bắt đầu thi

### Test Steps:

1. **Test Case 1: Flow bình thường (qua ExamPartComponent)**

   - Login → Chọn exam → Chọn Speaking part → Bắt đầu
   - Mở DevTools Console: `localStorage.getItem('currentExamAttempt')`

2. **Test Case 2: Navigate trực tiếp (URL trực tiếp)**

   - Clear localStorage: `localStorage.clear()`
   - Navigate trực tiếp: `/homepage/user-dashboard/part/123`
   - Kiểm tra Console logs

3. **Test Case 3: localStorage bị corrupt**
   - `localStorage.setItem('currentExamAttempt', 'invalid json')`
   - Reload page

### Expected Result:

- ✅ Case 1: Có attemptId ngay từ đầu (từ ExamPartComponent)
- ✅ Case 2: Console log `[Speaking] 🆕 Creating new exam attempt...`
- ✅ Case 2: Sau vài giây, `localStorage.getItem('currentExamAttempt')` có data
- ✅ Case 3: Tự động tạo attempt mới
- ✅ Tất cả cases: `this.attemptId` là số dương

### Bug Result (trước khi fix):

- ❌ Case 2: `attemptId = null` → Không submit được bài
- ❌ Case 3: Error → Component bị crash
- ❌ Không có auto-recovery

---

## ✅ Bug #1: Race Condition khi Submit Answer liên tục

### Test Steps:

1. Vào bài thi Speaking
2. Ghi âm câu 1 (10-15s)
3. Click nút "Nộp bài" **5 lần liên tục nhanh**
4. Mở DevTools Network tab, filter "speaking"

### Expected Result:

- ✅ Chỉ thấy **1 request POST** `/api/Speaking/submit`
- ✅ Console log: "Already processing/submitted"
- ✅ Button bị disable sau lần click đầu
- ✅ Không có duplicate scoring results

### Bug Result (trước khi fix):

- ❌ Thấy 5 requests POST cùng lúc
- ❌ Backend tạo 5 bản ghi `UserAnswerSpeaking`
- ❌ Điểm bị duplicate/overwrite

---

## ✅ Bug #2: attemptId Null/Undefined Handling

### Test Steps:

1. **Test Case 1: Normal Flow**
   - Login → Start exam Speaking
   - Check `localStorage.getItem('currentExamAttempt')`
   - Nộp bài câu 1
2. **Test Case 2: Missing localStorage**
   - Start exam
   - Mở DevTools Console: `localStorage.removeItem('currentExamAttempt')`
   - Thử nộp bài
3. **Test Case 3: Invalid attemptId = 0**
   - Start exam
   - DevTools: `localStorage.setItem('currentExamAttempt', JSON.stringify({attemptID: 0}))`
   - Thử nộp bài

### Expected Result:

- ✅ Case 1: attemptId = số dương (ví dụ: 123)
- ✅ Case 2: Alert "Lỗi hệ thống: Không tìm thấy ID bài thi"
- ✅ Case 3: Alert "Invalid attemptId"
- ✅ Console log rõ ràng: `[Speaking] ❌ Invalid attemptId: 0`

### Bug Result (trước khi fix):

- ❌ attemptId = 0 → Backend vẫn accept
- ❌ Không có alert, user bị stuck

---

## ✅ Bug #3: Backend Không Validate attemptId Ownership

### Test Steps:

1. User A login → Start exam → Lấy `attemptId = 123`
2. User B login
3. User B mở DevTools Console:
   ```javascript
   // Inject attemptId của User A
   localStorage.setItem(
     "currentExamAttempt",
     JSON.stringify({
       attemptID: 123, // attemptId của User A
       examId: 1,
     })
   );
   ```
4. User B start exam → Nộp bài

### Expected Result:

- ✅ Backend trả về: `403 Forbidden`
- ✅ Message: "You don't have permission to submit answers to this attempt."
- ✅ Frontend hiển thị error
- ✅ Database: Answer KHÔNG được lưu vào attempt của User A

### Bug Result (trước khi fix):

- ❌ Backend accept → Lưu answer vào attempt của User A
- ❌ User B có thể cheat điểm cho User A

---

## ✅ Bug #4: Azure Speech Recognition Retry Logic Yếu

### Test Steps:

1. **Setup**: Chặn Cloudinary upload tạm thời
   - DevTools Network → Throttle: Slow 3G
2. Ghi âm câu 1 → Nộp bài
3. Quan sát Console logs

### Expected Result:

- ✅ Thấy log: `[Speaking] Azure retry 1/3, waiting 500ms`
- ✅ Thấy log: `[Speaking] Azure retry 2/3, waiting 1000ms`
- ✅ Retry với exponential backoff: 500ms → 1000ms → 2000ms
- ✅ Sau 3 retries → Vẫn trả về result (có thể empty)

### Bug Result (trước khi fix):

- ❌ Chỉ retry 1 lần với fixed delay 800ms
- ❌ Không có log chi tiết
- ❌ Fail ngay nếu Cloudinary chậm

---

## ✅ Bug #5: Memory Leak - Audio URL Không Revoke

### Test Steps:

1. Start exam Speaking (11 câu)
2. Mỗi câu: Ghi âm → Nghe lại → Next
3. Mở DevTools Memory:
   - Performance → Record
   - Chuyển hết 11 câu
   - Take Heap Snapshot
4. Tìm "blob:" trong heap

### Expected Result:

- ✅ Heap snapshot: **0 Blob URLs** còn tồn tại
- ✅ Memory usage ổn định (~20-30MB)
- ✅ Console log: `URL.revokeObjectURL()` được gọi khi chuyển câu

### Bug Result (trước khi fix):

- ❌ Heap: 11 Blob URLs không được revoke
- ❌ Memory leak: +5MB mỗi câu → Tổng +55MB sau 11 câu
- ❌ Browser có thể crash sau nhiều câu

---

## ✅ Bug #6: NLP API Call Không Có Timeout

### Test Steps:

1. **Setup Mock**: Chặn NLP service
   - Backend: Comment out NLP service URL hoặc đổi thành URL invalid
2. Ghi âm → Nộp bài
3. Đợi và quan sát

### Expected Result:

- ✅ Request timeout sau **30 giây**
- ✅ Console log: `[Speaking] NLP API timeout`
- ✅ Frontend nhận error → Hiển thị message
- ✅ Không treo vô hạn

### Bug Result (trước khi fix):

- ❌ Request treo vô thời hạn
- ❌ User phải refresh page
- ❌ State stuck ở "processing"

---

## ✅ Bug #7: Frontend Không Handle Offline Mode

### Test Steps:

1. Start exam → Ghi âm câu 1
2. **Tắt mạng**: DevTools Network → Offline
3. Click "Nộp bài"

### Expected Result:

- ✅ Alert: "Mất kết nối mạng. Vui lòng kiểm tra và thử lại."
- ✅ State quay về "error"
- ✅ Button "Nộp bài" vẫn enabled để retry
- ✅ Audio vẫn được giữ (không mất)

### Bug Result (trước khi fix):

- ❌ Generic error không rõ ràng
- ❌ State stuck, không retry được
- ❌ Audio có thể bị mất

---

## ✅ Bug #8: Score Calculation Rounding Inconsistency

### Test Steps:

1. Nộp bài Speaking Part 5 (câu 11)
2. Kiểm tra Console logs:
   - Backend log: `[Scoring] Final=XX.X`
   - Frontend log: `earnedScore`
3. Kiểm tra kết quả hiển thị:
   - Summary page: `overallScore`
   - Individual scores: grammar, vocab, pronunciation

### Expected Result:

- ✅ Backend: Tất cả scores được round về **1 chữ số thập phân** (83.4)
- ✅ Frontend earnedScore: Round về **2 chữ số** (8.34)
- ✅ UI hiển thị: Luôn dùng `toFixed(1)` → "83.4"
- ✅ Không có số lẻ quá nhiều chữ số: ❌ 83.3400001

### Bug Result (trước khi fix):

- ❌ Điểm lẻ: 83.139999 hoặc 83.3400001
- ❌ Part 5 không round sau khi scale 1.67x

---

## ✅ Bug #9: Timer Không Pause Khi Chuyển Tab

### Test Steps:

1. Start exam → Ghi âm câu 1
2. Quan sát timer: `recordingTime = 5s`
3. **Minimize browser** hoặc chuyển sang tab khác
4. Đợi **20 giây**
5. Quay lại tab

### Expected Result:

- ✅ Timer vẫn hiển thị đúng thời gian đã ghi (5s + thời gian visible)
- ✅ Console log: `[SpeakingAnswerBox] ⚠️ Page hidden, pausing timer`
- ✅ Console log: `[SpeakingAnswerBox] ✅ Page visible, resuming timer`
- ✅ Recording không bị auto-stop sai thời điểm

### Bug Result (trước khi fix):

- ❌ Timer jump: 5s → 25s khi quay lại
- ❌ Hoặc timer chạy chậm do browser throttle

---

## ✅ Bug #10: LocalStorage Không Clear Sau Khi Hoàn Thành Exam

### Test Steps:

1. Làm xong 11 câu → Click "Hoàn thành"
2. Kiểm tra localStorage:
   ```javascript
   localStorage.getItem("currentExamAttempt");
   ```
3. Kiểm tra service state:
   ```javascript
   // Open console in speaking-question-state.service
   console.log(this.questionStates.size);
   ```

### Expected Result:

- ✅ `localStorage.getItem('currentExamAttempt')` = `null`
- ✅ Service state cleared: `questionStates.size = 0`
- ✅ Console log: `[Speaking] 🧹 Cleaning up session...`
- ✅ Console log: `[Speaking] ✅ Cleanup completed`

### Bug Result (trước khi fix):

- ❌ localStorage vẫn còn data
- ❌ Service giữ 11 câu trong Map
- ❌ User thi lại → thấy data cũ

---

## ✅ Bug #11: Race Condition Submit Multiple Questions

### Test Steps:

1. Start exam
2. **Ghi âm 3 câu liên tục** NHƯNG KHÔNG nộp
3. Click "Nộp bài" **cả 3 câu cùng lúc** (spam click nhanh)
4. Kiểm tra Network tab

### Expected Result:

- ✅ Thấy **3 requests** POST (mỗi câu 1 request) - OK
- ✅ Nếu spam 1 câu nhiều lần → Chỉ 1 request
- ✅ Console log: `⚠️ Question X already submitting, returning existing promise`
- ✅ State không bị stuck ở "scoring" nếu request fail

### Bug Result (trước khi fix):

- ❌ Duplicate requests cho cùng 1 câu
- ❌ State stuck nếu fail
- ❌ Không có timeout → request treo vô hạn

---

## 📊 **Test Coverage Summary**

| Bug | Test Type                 | Priority | Estimated Time |
| --- | ------------------------- | -------- | -------------- |
| #1  | Manual + Network          | High     | 5 min          |
| #2  | Manual + Console          | High     | 10 min         |
| #3  | Manual + 2 Users          | Critical | 15 min         |
| #4  | Manual + Network Throttle | Medium   | 10 min         |
| #5  | Manual + Memory Profiler  | Medium   | 15 min         |
| #6  | Manual + Mock             | Medium   | 10 min         |
| #7  | Manual + Offline          | High     | 5 min          |
| #8  | Manual + Console          | Low      | 10 min         |
| #9  | Manual + Tab Switch       | Medium   | 10 min         |
| #10 | Manual + Console          | High     | 5 min          |
| #11 | Manual + Network          | High     | 10 min         |

**Total Test Time:** ~1.5 - 2 hours

---

## 🎯 **Quick Smoke Test (15 phút)**

Nếu không có thời gian test hết, test tối thiểu:

1. ✅ **Bug #1**: Spam click "Nộp bài" → Chỉ 1 request
2. ✅ **Bug #3**: Thử inject attemptId người khác → 403 Forbidden
3. ✅ **Bug #7**: Tắt mạng → Error rõ ràng
4. ✅ **Bug #10**: Finish exam → localStorage cleared
5. ✅ **Bug #11**: Spam submit nhiều câu → Không duplicate

---

## 📝 **Reporting Template**

Khi test xong, ghi lại kết quả:

```markdown
## Test Results - [Date]

### Bug #1: Race Condition

- Status: ✅ PASS / ❌ FAIL
- Notes: [Ghi chú nếu có]
- Screenshot: [Link]

### Bug #2: attemptId Null Handling

- Status: ✅ PASS / ❌ FAIL
- Notes: ...

...
```
