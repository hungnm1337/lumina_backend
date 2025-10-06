# 📊 TOEIC Speaking Scoring System - Hướng dẫn Chấm điểm

## 🎯 Tổng quan

Hệ thống chấm điểm TOEIC Speaking của Lumina được thiết kế dựa trên **tiêu chuẩn chấm điểm chính thức của IIG (International Institute of Globalization)** và áp dụng công nghệ AI hiện đại.

---

## 📚 Cấu trúc TOEIC Speaking Test

| Task                        | Questions | Thời gian                  | Điểm/câu | Mô tả                     |
| --------------------------- | --------- | -------------------------- | -------- | ------------------------- |
| **1. Read Aloud**           | Q1-2      | 45s                        | 0-3      | Đọc to đoạn văn           |
| **2. Describe Picture**     | Q3        | 30s chuẩn bị + 45s nói     | 0-3      | Miêu tả hình ảnh          |
| **3. Respond to Questions** | Q4-6      | 3s chuẩn bị + 15s/30s nói  | 0-3      | Trả lời câu hỏi ngắn      |
| **4. Respond using Info**   | Q7-9      | 30s chuẩn bị + 15s/30s nói | 0-3      | Trả lời dựa vào thông tin |
| **5. Express an Opinion**   | Q10-11    | 30s chuẩn bị + 60s nói     | 0-5      | Diễn đạt quan điểm        |

**Tổng điểm: 0-200** (quy đổi từ điểm thô sang 8 levels)

---

## 🔬 Hệ thống AI Chấm điểm

### **1. Azure AI Speech Service** (Pronunciation Assessment)

Đánh giá khía cạnh **phát âm và phát biểu**:

- ✅ **Pronunciation Score** (0-100): Độ chính xác phát âm từng phoneme
- ✅ **Accuracy Score** (0-100): Độ chính xác từng từ so với reference
- ✅ **Fluency Score** (0-100): Độ trôi chảy, tốc độ nói, pause patterns
- ⚠️ **Completeness Score** (0-100): % từ được nói so với reference (CHỈ THAM KHẢO - không dùng tính điểm overall)

**Cấu hình:**

- Language model: `en-GB` (tối ưu cho Vietnamese-accented English)
- Audio format: MP3 16kHz (Cloudinary transformation)
- Pronunciation Assessment Granularity: Word level

---

### **2. Python NLP Service** (Grammar, Vocabulary, Content)

Đánh giá khía cạnh **ngữ pháp, từ vựng, nội dung**:

#### **A. Grammar Score** (0-100)

```python
# Sử dụng LanguageTool (rule-based grammar checker)
grammar_score = max(0, 100 - (number_of_errors * 5))
```

- Phát hiện lỗi ngữ pháp: subject-verb agreement, tenses, articles, etc.
- Mỗi lỗi trừ 5 điểm

#### **B. Content Score** (0-100)

```python
# Sử dụng Sentence Transformers (semantic similarity)
cosine_similarity = util.cos_sim(transcript_embedding, sample_answer_embedding)
content_score = cosine_similarity * 100
```

- Đo độ tương đồng nghĩa giữa câu trả lời và sample answer
- Model: `all-MiniLM-L6-v2` (384-dimensional embeddings)

#### **C. Vocabulary Score** (0-100) - **CẢI TIẾN MỚI** ✨

```python
# Kết hợp 3 yếu tố:
# 1. Word Length (30%): Độ dài từ trung bình
length_score = min(100, (average_word_length / 5.5) * 100)

# 2. Word Diversity (40%): Tỷ lệ từ unique / tổng từ
diversity_score = (unique_words / total_words) * 100

# 3. Word Complexity (30%): Tỷ lệ từ phức tạp (>6 chars)
complexity_score = min(100, (complex_words_ratio) * 200)

# Final score
vocabulary_score = length_score * 0.3 + diversity_score * 0.4 + complexity_score * 0.3
```

---

## ⚖️ Trọng số Chấm điểm Theo Task Type

### **🎤 Task 1: READ_ALOUD** (Q1-2)

**Trọng tâm: Pronunciation, Fluency, Accuracy**

| Tiêu chí      | Trọng số | Lý do                                      |
| ------------- | -------- | ------------------------------------------ |
| Pronunciation | **40%**  | Quan trọng nhất - đọc đúng âm              |
| Accuracy      | **25%**  | Đọc đúng từ                                |
| Fluency       | **20%**  | Đọc trôi chảy, không ngắt quãng            |
| Grammar       | 5%       | Không cần đánh giá nhiều (văn bản cho sẵn) |
| Vocabulary    | 5%       | Không cần đánh giá nhiều                   |
| Content       | 5%       | Không cần đánh giá nhiều                   |

**Ví dụ tính điểm:**

```
Pronunciation: 85, Accuracy: 90, Fluency: 80
Grammar: 70, Vocabulary: 70, Content: 70

Overall = 85*0.4 + 90*0.25 + 80*0.2 + 70*0.05 + 70*0.05 + 70*0.05
        = 34 + 22.5 + 16 + 3.5 + 3.5 + 3.5
        = 83.0
```

---

### **🖼️ Task 2: DESCRIBE_PICTURE** (Q3)

**Trọng tâm: Vocabulary, Grammar, Content**

| Tiêu chí      | Trọng số | Lý do                          |
| ------------- | -------- | ------------------------------ |
| Vocabulary    | **20%**  | Cần từ vựng đa dạng để miêu tả |
| Grammar       | **20%**  | Cấu trúc câu chính xác         |
| Content       | **20%**  | Miêu tả đúng nội dung hình     |
| Fluency       | **15%**  | Nói tự nhiên                   |
| Pronunciation | **15%**  | Phát âm rõ ràng                |
| Accuracy      | **10%**  | Phát âm đúng từ                |

---

### **💬 Task 3: RESPOND_QUESTIONS** (Q4-6)

**Trọng tâm: Fluency, Content, Accuracy**

| Tiêu chí      | Trọng số | Lý do                   |
| ------------- | -------- | ----------------------- |
| Fluency       | **25%**  | Trả lời nhanh, tự nhiên |
| Content       | **20%**  | Trả lời đúng câu hỏi    |
| Pronunciation | **15%**  | Phát âm rõ              |
| Accuracy      | **15%**  | Chính xác               |
| Grammar       | **15%**  | Ngữ pháp đúng           |
| Vocabulary    | **10%**  | Từ vựng phù hợp         |

---

### **📋 Task 4: RESPOND_WITH_INFO** (Q7-9)

**Trọng tâm: Content, Grammar, Vocabulary**

| Tiêu chí      | Trọng số | Lý do                             |
| ------------- | -------- | --------------------------------- |
| Content       | **25%**  | Trả lời dựa vào thông tin cho sẵn |
| Grammar       | **20%**  | Cấu trúc câu phức tạp             |
| Vocabulary    | **20%**  | Diễn đạt thông tin                |
| Fluency       | **15%**  | Nói trôi chảy                     |
| Pronunciation | **10%**  | Phát âm                           |
| Accuracy      | **10%**  | Chính xác từ                      |

---

### **💭 Task 5: EXPRESS_OPINION** (Q10-11)

**Trọng tâm: Tất cả yếu tố, đặc biệt Content, Grammar, Vocabulary**

| Tiêu chí      | Trọng số | Lý do                             |
| ------------- | -------- | --------------------------------- |
| Content       | **25%**  | Quan điểm rõ ràng, lập luận logic |
| Grammar       | **20%**  | Cấu trúc câu phức tạp, đa dạng    |
| Vocabulary    | **20%**  | Từ vựng phong phú, chính xác      |
| Fluency       | **15%**  | Nói tự nhiên, mạch lạc            |
| Pronunciation | **10%**  | Phát âm rõ ràng                   |
| Accuracy      | **10%**  | Chính xác từng từ                 |

---

## 🔄 Luồng Chấm điểm

```
1. User ghi âm → Upload Cloudinary
                ↓
2. Cloudinary transform → MP3 16kHz
                ↓
3. Azure Speech Service → Pronunciation, Accuracy, Fluency, Completeness
                ↓
4. Python NLP Service → Grammar, Vocabulary, Content
                ↓
5. Backend Scoring Logic:
   - Xác định Task Type (READ_ALOUD, DESCRIBE_PICTURE, etc.)
   - Áp dụng Weights tương ứng
   - Tính Overall Score (0-100)
                ↓
6. Lưu vào Database:
   - UserAnswer: transcript, audio_url, overall_score
   - SpeakingResult: 7 scores chi tiết
                ↓
7. Frontend hiển thị:
   - Per-question: "Đã nộp" + Audio player (ẨN ĐIỂM)
   - Summary: TOEIC Score (0-200) + Chi tiết tất cả câu
```

---

## 📈 Quy đổi TOEIC Score (0-200)

Frontend tính điểm TOEIC dựa trên **trung bình Overall Score** của tất cả câu:

```typescript
avgScore = sum(overallScores) / numberOfQuestions; // 0-100

toeicScore = Math.round((avgScore / 100) * 200); // 0-200
```

### **8 Levels TOEIC Speaking:**

| Score   | Level                     | Mô tả                                           |
| ------- | ------------------------- | ----------------------------------------------- |
| 160-200 | **8 - Advanced High**     | Nói rất lưu loát, ngữ pháp và từ vựng phong phú |
| 130-150 | **7 - Advanced Low**      | Nói tốt, có thể diễn đạt ý phức tạp             |
| 110-120 | **6 - Intermediate High** | Nói tương đối tốt, vẫn có lỗi nhỏ               |
| 80-100  | **5 - Intermediate Mid**  | Nói được nhưng còn nhiều lỗi                    |
| 60-70   | **4 - Intermediate Low**  | Nói cơ bản, lỗi khá nhiều                       |
| 40-50   | **3 - Novice High**       | Nói hạn chế                                     |
| 20-30   | **2 - Novice Mid**        | Nói rất hạn chế                                 |
| 0-10    | **1 - Novice Low**        | Gần như không nói được                          |

---

## 🛠️ Cải tiến So với Phiên bản Cũ

### **❌ Cũ (Không hợp lý):**

- Tất cả task type đều dùng weights giống nhau
- Vocabulary chỉ dựa vào độ dài từ trung bình
- Completeness Score ảnh hưởng 20% vào Overall Score

### **✅ Mới (Chuẩn TOEIC):**

- ✅ Weights khác nhau cho từng task type
- ✅ Vocabulary đánh giá 3 yếu tố: length, diversity, complexity
- ✅ Completeness chỉ để tham khảo, không ảnh hưởng Overall Score
- ✅ Logging chi tiết weights và scores để debug
- ✅ Validation transcript sau retry để tránh lỗi NLP service

---

## 📝 Lưu ý Kỹ thuật

1. **Azure Speech Region:** `southeastasia` (tối ưu latency cho VN)
2. **Language Model:** `en-GB` (tốt hơn `en-US` cho Vietnamese accent)
3. **Cloudinary Audio Transform:** `f_mp3,ar_16000` (16kHz sample rate)
4. **NLP Service:** Chạy local `http://127.0.0.1:8000`
5. **Error Handling:** Retry 1 lần nếu Azure trả về null transcript

---

## 🔍 Debug & Monitoring

Backend logs quan trọng:

```
[Speaking] MP3 URL for Azure: https://...
[Speaking] Using language model: en-GB
[Speaking] Transcript result: "..."
[Scoring] Task: READ_ALOUD, Weights: P=40%, A=25%, F=20%, G=5%, V=5%, C=5%, Final=83.5
```

---

## 📚 Tài liệu Tham khảo

- [TOEIC Speaking Score Descriptors - IIBC](https://www.iibc-global.org/english/toeic/test/sw/guide05/guide05_01/score_descriptor.html)
- [TOEIC Speaking Test Format](https://vn.elsaspeak.com/review-cau-truc-de-thi-toeic-speaking-test-va-cach-cham-diem/)
- [AI Super TOEIC - Hệ thống chấm AI tại VN](https://www.anhngumshoa.com/tin-tuc/ra-mat-ai-super-toeic-website-ai-cham-speaking-writing-chinh-xac-theo-format-chuan-de-thi-toeic-38793.html)

---

**Cập nhật lần cuối:** 2025-01-06  
**Version:** 2.0 (TOEIC-aligned scoring)
