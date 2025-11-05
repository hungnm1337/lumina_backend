# 🔍 DEBUG: Tại sao Transcript trống?

## Bước 1: Kiểm tra Backend Console Logs

Khi bạn nộp bài Speaking, backend sẽ in ra các log sau:

```
[AzureSpeech] ResultReason: RecognizedSpeech   <-- ✅ PHẢI LÀ RecognizedSpeech
[AzureSpeech] Detailed JSON: {...}
[Speaking] Transcript result: <transcript>    <-- ❌ Nếu rỗng hoặc "." = THẤT BẠI
```

### ❌ Nếu thấy:
```
[AzureSpeech] ResultReason: NoMatch
[AzureSpeech] Cancellation: Canceled - Error - ...
```
→ **Nguyên nhân: Azure không nhận diện được giọng nói**

---

## Bước 2: Nguyên nhân phổ biến

### 🎤 **A. File audio không đúng định dạng**

**Triệu chứng:**
- Backend log: `ResultReason: NoMatch`
- Hoặc: `ErrorCode: BadRequest`

**Giải pháp:**
1. Mở DevTools Console (F12)
2. Kiểm tra `audioBlob` size:
   ```javascript
   // Trong Console khi recording
   [SpeakingAnswerBox] Saving recording to state service, size: 45678
   ```
3. **Nếu size < 1000 bytes** → Audio quá ngắn/rỗng

**Fix:** Nói lâu hơn (> 1 giây)

---

### 🌐 **B. Cloudinary chưa kịp transform MP3**

**Triệu chứng:**
- Backend log đầu tiên: `ResultReason: NoMatch`
- Backend log SAU RETRY: `ResultReason: RecognizedSpeech` ✅

**Giải pháp:** Đã có retry logic (line 96-111 trong SpeakingScoringService.cs)

---

### 🎙️ **C. Giọng nói không rõ ràng / Tiếng ồn**

**Triệu chứng:**
- Backend log: `ResultReason: RecognizedSpeech`
- Nhưng: `Transcript = "."` hoặc transcript rất ngắn

**Nguyên nhân:**
- Mic bị nhiễu
- Nói quá nhỏ
- Background noise
- Giọng Việt quá nặng

**Giải pháp:**
1. Đảm bảo mic hoạt động tốt
2. Nói to, rõ ràng
3. Tắt tiếng ồn xung quanh
4. Thử giọng British English (backend đã dùng `en-GB`)

---

### 🔑 **D. Azure API Key hết hạn / Vượt quota**

**Triệu chứng:**
```
[AzureSpeech] Cancellation: Error
ErrorCode: Forbidden / Unauthorized
```

**Kiểm tra:**
```bash
# File: appsettings.json
"AzureSpeechSettings": {
  "SubscriptionKey": "YOUR_KEY",  <-- Kiểm tra key còn hiệu lực
  "Region": "southeastasia"       <-- Kiểm tra region đúng
}
```

**Giải pháp:** 
- Kiểm tra Azure Portal → Speech Service
- Xem usage quota
- Renew key nếu cần

---

## Bước 3: Test Transcript ngay từ Browser

Mở DevTools Console và chạy:

```javascript
// 1. Ghi âm test
const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
const recorder = new MediaRecorder(stream);
const chunks = [];

recorder.ondataavailable = (e) => chunks.push(e.data);
recorder.onstop = async () => {
  const blob = new Blob(chunks, { type: 'audio/webm' });
  console.log('Blob size:', blob.size);
  
  // 2. Submit thử
  const formData = new FormData();
  formData.append('audio', blob, 'test.webm');
  formData.append('questionId', '1'); // Thay số câu hỏi thực tế
  formData.append('attemptId', '163'); // Thay attemptId thực tế
  
  const token = localStorage.getItem('lumina_token');
  const response = await fetch('https://your-api/api/Speaking/submit-answer', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  });
  
  const result = await response.json();
  console.log('Result:', result);
};

recorder.start();
setTimeout(() => {
  recorder.stop();
  stream.getTracks().forEach(t => t.stop());
}, 3000); // Ghi 3 giây

// 3. Nói rõ ràng vào mic
```

---

## Bước 4: Workaround tạm thời

Nếu vẫn không nhận diện được, thêm fallback text:

**File:** `SpeakingScoringService.cs` (line ~119)

```csharp
// TRƯỚC:
if (string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".")
{
    Console.WriteLine("[Speaking] Azure transcription failed, using fallback");
    azureResult.Transcript = "."; // ← Transcript rỗng
}

// SAU:
if (string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".")
{
    Console.WriteLine("[Speaking] Azure transcription failed, using fallback");
    azureResult.Transcript = "[Audio submitted but not recognized]"; // ← User-friendly message
}
```

---

## Bước 5: Kiểm tra Network Request

1. Mở DevTools → Network tab
2. Filter: `submit-answer`
3. Xem Response:
   ```json
   {
     "transcript": ".",  // ← ❌ Thất bại
     "overallScore": 0,
     "pronunciationScore": null
   }
   ```

**Nếu thấy transcript = ".":**
- Xem backend console log
- Kiểm tra Azure Speech Service status

---

## ✅ Giải pháp cuối cùng

Thêm enhanced logging để debug:

**File:** `SpeakingScoringService.cs`

Thêm vào đầu method `ProcessAndScoreAnswerAsync`:

```csharp
Console.WriteLine($"[Speaking] === BEGIN ProcessAndScoreAnswerAsync ===");
Console.WriteLine($"[Speaking] QuestionId: {questionId}, AttemptId: {attemptId}");
Console.WriteLine($"[Speaking] Audio file size: {audioFile.Length} bytes");
Console.WriteLine($"[Speaking] Audio content type: {audioFile.ContentType}");
```

Thêm sau khi gọi Azure:

```csharp
Console.WriteLine($"[Speaking] === AZURE RESULT ===");
Console.WriteLine($"[Speaking] Transcript: '{azureResult.Transcript}'");
Console.WriteLine($"[Speaking] ErrorMessage: '{azureResult.ErrorMessage}'");
Console.WriteLine($"[Speaking] PronScore: {azureResult.PronunciationScore}");
```

---

## 📊 Kết quả mong đợi

Sau khi fix, backend log phải như này:

```
[Speaking] === BEGIN ProcessAndScoreAnswerAsync ===
[Speaking] QuestionId: 123, AttemptId: 163
[Speaking] Audio file size: 45678 bytes
[Speaking] Audio content type: audio/webm
[Speaking] MP3 URL for Azure: https://res.cloudinary.com/.../file.mp3
[Speaking] Using language model: en-GB
[AzureSpeech] ResultReason: RecognizedSpeech ✅
[Speaking] Transcript result: Hello, my name is John ✅
[Speaking] === AZURE RESULT ===
[Speaking] Transcript: 'Hello, my name is John'
[Speaking] PronScore: 85.5
```

---

## 🆘 Nếu vẫn không được

Gửi cho tôi:
1. Backend console log FULL (từ khi submit đến khi return)
2. DevTools Network tab screenshot của request `submit-answer`
3. Audio file size (bytes)
4. Nội dung bạn đã nói

Tôi sẽ debug chi tiết hơn!
