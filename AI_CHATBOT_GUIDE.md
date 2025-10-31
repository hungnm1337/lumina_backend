# AI Chatbot Service - Hướng dẫn sử dụng

## Tổng quan
Service chatbot AI sử dụng Gemini 2.5 để trả lời câu hỏi về nội dung bài học TOEIC. Học sinh có thể hỏi bất kỳ câu hỏi nào liên quan đến bài học và nhận được câu trả lời chi tiết, dễ hiểu từ AI.

## Cấu hình

### appsettings.json
```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "ModelName": "gemini-2.0-flash-exp"
  }
}
```

### Đăng ký Service (Program.cs)
```csharp
builder.Services.AddScoped<IAIChatService, AIChatService>();
```

## API Endpoints

### 1. Ask Question - Hỏi câu hỏi đơn giản
**Endpoint:** `POST /api/AIChat/ask-question`

Sử dụng để hỏi câu hỏi về nội dung bài học.

**Request Body:**
```json
{
  "userQuestion": "Present Simple tense được sử dụng khi nào?",
  "lessonContent": "Present Simple tense is used for habits, general truths, and permanent situations. Examples: I work every day. The sun rises in the east.",
  "lessonTitle": "Present Simple Tense - Thì hiện tại đơn",
  "userId": 123,
  "articleId": 456
}
```

**Response:**
```json
{
  "answer": "Present Simple tense được sử dụng trong 3 trường hợp chính:\n\n1. **Thói quen (Habits)**: Những hành động xảy ra thường xuyên, lặp đi lặp lại\n   - Ví dụ: I work every day (Tôi làm việc mỗi ngày)\n\n2. **Chân lý, sự thật hiển nhiên (General truths)**: Những sự thật luôn đúng\n   - Ví dụ: The sun rises in the east (Mặt trời mọc ở phía đông)\n\n3. **Tình huống cố định (Permanent situations)**: Những tình huống lâu dài, ổn định\n   - Ví dụ: She lives in Hanoi (Cô ấy sống ở Hà Nội)\n\n**Cấu trúc:**\n- Khẳng định: S + V(s/es)\n- Phủ định: S + do/does + not + V\n- Nghi vấn: Do/Does + S + V?",
  "confidenceScore": 95,
  "suggestedQuestions": [
    "Làm thế nào để thêm 's' hoặc 'es' vào động từ?",
    "Khi nào dùng 'do' và khi nào dùng 'does'?",
    "Present Simple khác gì với Present Continuous?"
  ],
  "relatedTopics": [
    "grammar",
    "tense",
    "structure",
    "ngữ pháp",
    "thì"
  ],
  "timestamp": "2025-10-29T10:30:00Z",
  "success": true,
  "errorMessage": null
}
```

---

### 2. Continue Conversation - Tiếp tục cuộc trò chuyện
**Endpoint:** `POST /api/AIChat/continue-conversation`

Sử dụng khi muốn tiếp tục cuộc trò chuyện với context từ các câu hỏi trước.

**Request Body:**
```json
{
  "userQuestion": "Vậy khi nào thì thêm 'es' thay vì chỉ thêm 's'?",
  "lessonContent": "Present Simple tense is used for habits...",
  "lessonTitle": "Present Simple Tense",
  "conversationHistory": [
    {
      "role": "user",
      "content": "Present Simple tense được sử dụng khi nào?",
      "timestamp": "2025-10-29T10:30:00Z"
    },
    {
      "role": "assistant",
      "content": "Present Simple tense được sử dụng trong 3 trường hợp...",
      "timestamp": "2025-10-29T10:30:05Z"
    }
  ]
}
```

**Response:**
```json
{
  "currentResponse": {
    "answer": "Chúng ta thêm **'es'** thay vì chỉ 's' trong các trường hợp sau:\n\n1. **Động từ kết thúc bằng -o, -s, -ss, -x, -ch, -sh:**\n   - go → goes\n   - pass → passes\n   - watch → watches\n   - wash → washes\n\n2. **Động từ kết thúc bằng phụ âm + y:**\n   - study → studies (bỏ y, thêm ies)\n   - fly → flies\n   \n**Lưu ý:** Nếu động từ kết thúc bằng nguyên âm + y thì chỉ thêm 's':\n   - play → plays\n   - enjoy → enjoys",
    "confidenceScore": 90,
    "suggestedQuestions": [
      "Còn động từ bất quy tắc thì sao?",
      "Cho tôi thêm ví dụ về động từ có đuôi -ch"
    ],
    "success": true
  },
  "conversationHistory": [
    {
      "role": "user",
      "content": "Present Simple tense được sử dụng khi nào?",
      "timestamp": "2025-10-29T10:30:00Z"
    },
    {
      "role": "assistant",
      "content": "Present Simple tense được sử dụng trong 3 trường hợp...",
      "timestamp": "2025-10-29T10:30:05Z"
    },
    {
      "role": "user",
      "content": "Vậy khi nào thì thêm 'es' thay vì chỉ thêm 's'?",
      "timestamp": "2025-10-29T10:31:00Z"
    },
    {
      "role": "assistant",
      "content": "Chúng ta thêm 'es' thay vì chỉ 's' trong các trường hợp...",
      "timestamp": "2025-10-29T10:31:05Z"
    }
  ],
  "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

---

### 3. Generate Suggested Questions - Tạo câu hỏi gợi ý
**Endpoint:** `POST /api/AIChat/suggested-questions`

Tự động tạo các câu hỏi gợi ý dựa trên nội dung bài học.

**Request Body:**
```json
{
  "lessonContent": "Present Simple tense is used for habits, general truths, and permanent situations...",
  "lessonTitle": "Present Simple Tense"
}
```

**Response:**
```json
{
  "answer": "Đây là một số câu hỏi gợi ý dựa trên nội dung bài học:",
  "confidenceScore": 95,
  "suggestedQuestions": [
    "Present Simple tense có những cách sử dụng nào?",
    "Làm thế nào để chia động từ trong Present Simple?",
    "Sự khác biệt giữa Present Simple và Present Continuous là gì?",
    "Khi nào cần thêm 's' hoặc 'es' vào động từ?",
    "Present Simple có những trạng từ chỉ tần suất nào thường đi kèm?"
  ],
  "success": true
}
```

---

### 4. Explain Concept - Giải thích khái niệm
**Endpoint:** `POST /api/AIChat/explain-concept`

Giải thích chi tiết một khái niệm, thuật ngữ cụ thể.

**Request Body:**
```json
{
  "concept": "adverb of frequency",
  "lessonContext": "Present Simple often uses adverbs of frequency like always, usually, often, sometimes, rarely, never."
}
```

**Response:**
```json
{
  "answer": "**Adverb of Frequency (Trạng từ chỉ tần suất)**\n\n**1. Định nghĩa:**\nTrạng từ chỉ tần suất là những từ dùng để diễn tả mức độ thường xuyên của một hành động.\n\n**2. Các trạng từ phổ biến (từ cao đến thấp):**\n- Always (100%) - luôn luôn\n- Usually (80-90%) - thường thường\n- Often (60-70%) - thường xuyên\n- Sometimes (40-50%) - thỉnh thoảng\n- Rarely/Seldom (10-20%) - hiếm khi\n- Never (0%) - không bao giờ\n\n**3. Vị trí trong câu:**\n- Đứng TRƯỚC động từ thường: I always study English.\n- Đứng SAU động từ 'to be': She is usually happy.\n\n**4. Ví dụ:**\n- I often go to the gym. (Tôi thường xuyên đi phòng gym)\n- He rarely eats fast food. (Anh ấy hiếm khi ăn đồ ăn nhanh)\n\n**5. Lưu ý:**\n- Không dùng với Present Continuous\n- Có thể đặt ở đầu hoặc cuối câu để nhấn mạnh",
  "confidenceScore": 90,
  "relatedTopics": [
    "grammar",
    "structure",
    "ngữ pháp",
    "cấu trúc"
  ],
  "success": true
}
```

---

### 5. Quick Ask - Hỏi nhanh
**Endpoint:** `POST /api/AIChat/quick-ask`

Endpoint đơn giản hóa cho câu hỏi nhanh.

**Request Body:**
```json
{
  "question": "Cho tôi 3 ví dụ về Present Simple",
  "context": "Present Simple tense is used for habits...",
  "userId": 123
}
```

**Response:**
```json
{
  "answer": "Dưới đây là 3 ví dụ về Present Simple tense:\n\n1. **Thói quen:**\n   - I drink coffee every morning. (Tôi uống cà phê mỗi sáng)\n\n2. **Chân lý:**\n   - Water boils at 100 degrees Celsius. (Nước sôi ở 100 độ C)\n\n3. **Tình huống cố định:**\n   - My sister works in a hospital. (Chị tôi làm việc ở bệnh viện)",
  "suggestedQuestions": [
    "Làm thế nào để tạo câu phủ định trong Present Simple?",
    "Present Simple khác gì Past Simple?"
  ],
  "success": true
}
```

---

## Tính năng nổi bật

### 1. **Contextual Understanding**
- AI hiểu ngữ cảnh bài học và trả lời chính xác
- Sử dụng ví dụ từ nội dung bài học

### 2. **Conversation History**
- Ghi nhớ lịch sử cuộc trò chuyện (5 câu gần nhất)
- Trả lời có tính liên tục và nhất quán

### 3. **Vietnamese + English Mix**
- Giải thích bằng tiếng Việt dễ hiểu
- Giữ nguyên các thuật ngữ tiếng Anh quan trọng

### 4. **Follow-up Questions**
- Tự động gợi ý câu hỏi tiếp theo
- Giúp học sinh khám phá kiến thức sâu hơn

### 5. **Related Topics**
- Nhận diện các chủ đề liên quan
- Gợi ý hướng học tập mở rộng

---

## Sử dụng trong Code

### C# Client
```csharp
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

var request = new
{
    userQuestion = "Present Simple tense được sử dụng khi nào?",
    lessonContent = "Present Simple tense is used for habits...",
    lessonTitle = "Present Simple Tense",
    userId = 123
};

var response = await httpClient.PostAsJsonAsync("/api/AIChat/ask-question", request);
var result = await response.Content.ReadFromJsonAsync<ChatResponseDTO>();

Console.WriteLine(result.Answer);
```

### JavaScript/TypeScript
```typescript
const askQuestion = async (question: string, lessonContent: string) => {
  const response = await fetch('/api/AIChat/ask-question', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      userQuestion: question,
      lessonContent: lessonContent,
      lessonTitle: "Present Simple Tense"
    })
  });
  
  const data = await response.json();
  return data;
};

// Sử dụng
const result = await askQuestion(
  "Present Simple tense được sử dụng khi nào?",
  "Present Simple tense is used for habits..."
);

console.log(result.answer);
console.log("Suggested questions:", result.suggestedQuestions);
```

### React Hook Example
```typescript
import { useState } from 'react';

const useAIChat = () => {
  const [loading, setLoading] = useState(false);
  const [conversation, setConversation] = useState([]);

  const askQuestion = async (question: string, lessonContent: string) => {
    setLoading(true);
    try {
      const response = await fetch('/api/AIChat/continue-conversation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userQuestion: question,
          lessonContent: lessonContent,
          conversationHistory: conversation
        })
      });
      
      const data = await response.json();
      setConversation(data.conversationHistory);
      return data.currentResponse;
    } finally {
      setLoading(false);
    }
  };

  return { askQuestion, loading, conversation };
};
```

---

## Error Handling

### 400 Bad Request
```json
{
  "message": "User question cannot be empty."
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "errorMessage": "Error: Gemini API timeout",
  "answer": "Xin lỗi, tôi gặp lỗi khi xử lý câu hỏi của bạn. Vui lòng thử lại sau."
}
```

---

## Best Practices

### 1. **Cung cấp Context đầy đủ**
```json
{
  "userQuestion": "Giải thích cho tôi",  // ❌ Không rõ ràng
  "userQuestion": "Giải thích cách sử dụng Present Simple"  // ✅ Rõ ràng
}
```

### 2. **Sử dụng Conversation History**
Để có câu trả lời chính xác hơn, luôn gửi kèm lịch sử cuộc trò chuyện khi tiếp tục hỏi.

### 3. **Limit Context Length**
Nội dung bài học quá dài (>5000 từ) có thể gây timeout. Nên chia nhỏ hoặc tóm tắt.

### 4. **Handle Timeouts**
Gemini API có thể mất 5-10 giây để phản hồi. Nên có loading indicator.

---

## Architecture

```
User Input
    ↓
AIChatController
    ↓
AIChatService
    ↓
Gemini 2.5 API
    ↓
Response Processing
    ↓
Structured ChatResponseDTO
    ↓
Return to Client
```

---

## Performance Tips

1. **Cache Suggested Questions**: Cache các câu hỏi gợi ý cho mỗi bài học
2. **Limit History**: Chỉ gửi 5 tin nhắn gần nhất để giảm token usage
3. **Async Operations**: Sử dụng async/await để không block UI
4. **Timeout Handling**: Set timeout 30s cho Gemini API calls

---

## Future Enhancements

- [ ] Lưu lịch sử chat vào database
- [ ] Hỗ trợ đa ngôn ngữ (English only mode)
- [ ] Voice input/output
- [ ] Personalized responses dựa trên level của học sinh
- [ ] Quiz generation từ conversation
- [ ] Export conversation as study notes

---

## Support

Nếu gặp vấn đề:
1. Kiểm tra Gemini API key trong appsettings.json
2. Xem logs trong ILogger để debug
3. Kiểm tra network connectivity
4. Verify model name là "gemini-2.0-flash-exp" hoặc model khả dụng

---

**Version:** 1.0.0  
**Last Updated:** October 29, 2025  
**License:** Internal Use Only
