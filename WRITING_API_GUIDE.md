# Writing API Guide

## Overview
API endpoints để lưu câu trả lời Writing và nhận feedback từ AI cho bài thi TOEIC Writing.

## Endpoints

### 1. Save Writing Answer
**Endpoint:** `POST /api/Writing/save-answer`

Lưu câu trả lời Writing vào database.

**Request Body:**
```json
{
  "userAnswerWritingId": 0,
  "attemptID": 1,
  "questionId": 5,
  "userAnswerContent": "The man is wearing a suit and tie.",
  "feedbackFromAI": "{\"TotalScore\":4,\"GrammarFeedback\":\"Good grammar\"}"
}
```

**Response:**
```json
{
  "message": "Writing answer saved successfully.",
  "success": true
}
```

### 2. Get AI Feedback
**Endpoint:** `POST /api/Writing/get-feedback`

Nhận feedback từ AI cho câu trả lời Writing.

**Request Body:**
```json
{
  "pictureCaption": "A man in business attire",
  "vocabularyRequest": "suit, tie, professional",
  "userAnswer": "The man is wearing a suit and tie."
}
```

**Response:**
```json
{
  "totalScore": 4,
  "grammarFeedback": "Your grammar is generally correct. The sentence structure is simple but effective.",
  "vocabularyFeedback": "Good use of required vocabulary: 'suit' and 'tie'. Consider adding 'professional' to enhance your description.",
  "requiredWordsCheck": "You successfully used 2 out of 3 required words: suit, tie. Missing: professional",
  "contentAccuracyFeedback": "Your answer accurately describes the picture caption. The description is clear and relevant.",
  "correededAnswerProposal": "The man is wearing a professional suit and tie, presenting a business-like appearance."
}
```

### 3. Submit and Evaluate (Combined)
**Endpoint:** `POST /api/Writing/submit-and-evaluate`

Lưu câu trả lời và nhận feedback từ AI trong một lần gọi.

**Request Body:**
```json
{
  "attemptID": 1,
  "questionId": 5,
  "userAnswerContent": "The man is wearing a suit and tie.",
  "pictureCaption": "A man in business attire",
  "vocabularyRequest": "suit, tie, professional"
}
```

**Response:**
```json
{
  "feedback": {
    "totalScore": 4,
    "grammarFeedback": "Your grammar is generally correct...",
    "vocabularyFeedback": "Good use of required vocabulary...",
    "requiredWordsCheck": "You successfully used 2 out of 3 required words...",
    "contentAccuracyFeedback": "Your answer accurately describes...",
    "correededAnswerProposal": "The man is wearing a professional suit..."
  },
  "saveSuccess": true,
  "message": "Answer saved and evaluated successfully."
}
```

## Error Responses

### 400 Bad Request
```json
{
  "message": "Invalid AttemptID."
}
```

### 500 Internal Server Error
```json
{
  "message": "An unexpected error occurred while saving writing answer.",
  "success": false
}
```

## Implementation Details

### Database Table: UserAnswerWriting
- **UserAnswerWritingId**: Primary key (auto-generated)
- **AttemptID**: Foreign key to ExamAttempt
- **QuestionId**: Foreign key to Question
- **UserAnswerContent**: Student's answer text
- **FeedbackFromAI**: AI feedback stored as JSON string

### Architecture
```
Controller (WritingController.cs)
    ↓
Service (WritingService.cs)
    ↓
Repository (WrittingRepository.cs)
    ↓
Database (UserAnswerWriting table)
```

### Dependencies Registered in Program.cs
```csharp
builder.Services.AddScoped<IWrittingRepository, WrittingRepository>();
builder.Services.AddScoped<IWritingService, WritingService>();
```

## Usage Example

### C# Client
```csharp
var client = new HttpClient();
client.BaseAddress = new Uri("https://localhost:5001");

var request = new
{
    attemptID = 1,
    questionId = 5,
    userAnswerContent = "The man is wearing a suit and tie.",
    pictureCaption = "A man in business attire",
    vocabularyRequest = "suit, tie, professional"
};

var response = await client.PostAsJsonAsync("/api/Writing/submit-and-evaluate", request);
var result = await response.Content.ReadFromJsonAsync<dynamic>();
```

### JavaScript/TypeScript
```javascript
const response = await fetch('/api/Writing/submit-and-evaluate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_JWT_TOKEN'
  },
  body: JSON.stringify({
    attemptID: 1,
    questionId: 5,
    userAnswerContent: "The man is wearing a suit and tie.",
    pictureCaption: "A man in business attire",
    vocabularyRequest: "suit, tie, professional"
  })
});

const result = await response.json();
console.log(result.feedback);
```

## AI Scoring Criteria

### TotalScore (0-5)
- **5**: Excellent - Perfect grammar, vocabulary, and content
- **4**: Good - Minor errors, good vocabulary usage
- **3**: Satisfactory - Some errors, adequate vocabulary
- **2**: Needs Improvement - Multiple errors, limited vocabulary
- **1**: Poor - Many errors, inadequate vocabulary
- **0**: No answer or completely incorrect

### Evaluation Components
1. **Grammar**: Sentence structure, verb tenses, punctuation
2. **Vocabulary**: Word choice, required words usage
3. **Content**: Relevance to picture caption, completeness
4. **Required Words**: Usage of specified vocabulary

## Notes
- Gemini API key phải được cấu hình trong `appsettings.json`
- Endpoint `/submit-and-evaluate` được khuyến nghị sử dụng vì kết hợp cả hai chức năng
- Feedback từ AI được lưu dưới dạng JSON string trong database
- Nếu answer đã tồn tại (cùng AttemptID và QuestionId), nó sẽ được cập nhật thay vì tạo mới
