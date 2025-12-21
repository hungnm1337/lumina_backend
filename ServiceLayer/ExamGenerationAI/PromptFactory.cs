using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using DataLayer.DTOs.AIGeneratedExam;
using System.Text.RegularExpressions;

namespace ServiceLayer.AI.Prompt
{
    public static class PromptFactory
    {
        //  CẤU HÌNH TẬP TRUNG
        private static readonly Dictionary<int, PartConfiguration> PartConfigs = new()
        {
            // LISTENING
            { 1, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 6 } },
            { 2, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 25 } },
            { 3, new PartConfiguration { QuestionsPerPrompt = 3, DefaultPromptCount = 5 } },
            { 4, new PartConfiguration { QuestionsPerPrompt = 3, DefaultPromptCount = 5 } },
            
            // READING
            { 5, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 30 } },
            { 6, new PartConfiguration { QuestionsPerPrompt = 4, DefaultPromptCount = 4 } },
            { 7, new PartConfiguration { QuestionsPerPrompt = 3, DefaultPromptCount = 5 } },
            
            // SPEAKING
            { 8, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 2 } },  
            { 9, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 2 } },  
            { 10, new PartConfiguration { QuestionsPerPrompt = 3, DefaultPromptCount = 1 } }, 
            { 11, new PartConfiguration { QuestionsPerPrompt = 3, DefaultPromptCount = 1 } }, 
            { 12, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 1 } }, 
            
            // WRITING
            { 13, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 5 } },
            { 14, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 2 } },
            { 15, new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 1 } }
        };

        //  CLASS CONFIGURATION
        public class PartConfiguration
        {
            public int QuestionsPerPrompt { get; set; }
            public int DefaultPromptCount { get; set; }
        }

        // Lấy config theo partNumber
        public static PartConfiguration GetPartConfiguration(int partNumber)
        {
            return PartConfigs.TryGetValue(partNumber, out var config)
                ? config
                : new PartConfiguration { QuestionsPerPrompt = 1, DefaultPromptCount = 1 };
        }
        
        public static string CreateParsingPrompt(string userRequest)
        {
            // Không cần xử lý Regex trước ở đây nữa. Gemini sẽ làm việc này.
            string jsonStringUserRequest = JsonConvert.SerializeObject(userRequest); // Vẫn serialize để đảm bảo an toàn

            // Định nghĩa số lượng mặc định cho từng Part (Số lượng Prompts/Items cần tạo)
            var defaultQuantities = new Dictionary<int, int>
        {
            { 1, 6 },   // Listening Part 1: 6 prompts (ảnh)
            { 2, 25 },  // Listening Part 2: 25 prompts (câu hỏi)
            { 3, 5 },  // Listening Part 3: 5 prompts (mỗi prompt 3 câu hỏi)
            { 4, 5 },  // Listening Part 4: 5 prompts (mỗi prompt 3 câu hỏi)
            { 5, 30 },  // Reading Part 5: 30 prompts (câu hỏi)
            { 6, 4 },   // Reading Part 6: 4 prompts (đoạn văn)
            { 7, 5 },  // Reading Part 7: Tổng cộng ~15 cụm (10 single, 2 double, 3 triple)
            { 8, 2 },   // Speaking Part 1 (Q1-2): 2 prompts (đoạn văn đọc)
            { 9, 2 },   // Speaking Part 2 (Q3): 2 prompt (ảnh miêu tả)
            { 10, 1 },  // Speaking Part 3 (Q4-6): 1 prompt (3 câu hỏi)
            { 11, 1 },  // Speaking Part 4 (Q7-9): 1 prompt (3 câu hỏi + info)
            { 12, 1 },  // Speaking Part 5 (Q10-11): 1 prompt (express opinion)
            { 13, 5 },  // Writing Q1-5: 5 prompts (ảnh + từ)
            { 14, 2 },  // Writing Q6-7: 2 prompts (email)
            { 15, 1 }   // Writing Q8: 1 prompt (đề luận)
        };
            string defaultQuantitiesJson = JsonConvert.SerializeObject(defaultQuantities, Formatting.Indented);

            // SỬA LỖI: Bắt đầu chuỗi với $$$ và dùng {{{...}}} cho các biến nội suy.
            return $$$"""
            Bạn là một trợ lý phân tích yêu cầu tạo đề thi TOEIC cực kỳ thông minh. Nhiệm vụ của bạn là đọc kỹ yêu cầu của người dùng và trích xuất chính xác các thông tin sau đây, trả về dưới dạng một đối tượng JSON duy nhất (không có markdown hay giải thích gì thêm).

            **Thông tin cần trích xuất:**

            1.  **`partNumber` (Integer):** Số thứ tự của phần thi TOEIC. Hãy xác định dựa trên các từ khóa như "Part 1", "Part 5", "Reading Part 7", "Speaking task 3", "Writing question 8", "miêu tả tranh", "bài luận", v.v.
                * **Mapping:**
                    * Listening Part 1 -> 1
                    * Listening Part 2 -> 2
                    * Listening Part 3 -> 3
                    * Listening Part 4 -> 4
                    
                    **LƯU Ý QUAN TRỌNG VỀ LISTENING:**
                    - "Listening Part 1" hoặc "miêu tả ảnh/tranh" → partNumber = 1
                    - "Listening Part 2" hoặc "hỏi đáp ngắn" → partNumber = 2
                    - "Listening Part 3" hoặc "hội thoại" → partNumber = 3
                    - "Listening Part 4" hoặc "monologue/độc thoại" → partNumber = 4
                    
                    * Reading Part 5 -> 5
                    * Reading Part 6 -> 6
                    * Reading Part 7 -> 7
                    
                    **LƯU Ý QUAN TRỌNG VỀ READING:**
                    - "Reading Part 5" hoặc "incomplete sentences/hoàn thành câu" → partNumber = 5
                    - "Reading Part 6" hoặc "text completion/hoàn thành đoạn văn" → partNumber = 6
                    - "Reading Part 7" hoặc "reading comprehension/đọc hiểu/single/double/triple passage" → partNumber = 7
                    
                    * Speaking Part 1 (Questions 1-2: Read Aloud) -> 8
                    * Speaking Part 2 (Question 3: Describe Picture) -> 9
                    * Speaking Part 3 (Questions 4-6: Respond to Questions) -> 10
                    * Speaking Part 4 (Questions 7-9: Respond Using Information) -> 11
                    * Speaking Part 5 (Questions 10-11: Express Opinion) -> 12
                    
                    **LƯU Ý QUAN TRỌNG VỀ SPEAKING:**
                    - "Speaking Part 1" → partNumber = 8 (KHÔNG PHẢI 1!)
                    - "Speaking Part 2" → partNumber = 9 (KHÔNG PHẢI 2!)
                    - "Speaking Part 3" → partNumber = 10 (KHÔNG PHẢI 3!)
                    - "Speaking Part 4" → partNumber = 11 (KHÔNG PHẢI 4!)
                    - "Speaking Part 5" → partNumber = 12 (KHÔNG PHẢI 5!)
                    - "Speaking question 5" hoặc "Speaking Q5" → thuộc Part 3 → partNumber = 10
                    
                    * Writing Part1 (Sentence from Picture) -> 13
                    * Writing Part2 (Respond Email) -> 14
                    * Writing Part3 (Opinion Essay) -> 15
                    
                    **LƯU Ý QUAN TRỌNG VỀ WRITING:**
                    - "Writing Part 1" hoặc "Writing Q1-5" → partNumber = 13, quantity = 5 (từ bảng mặc định)
                    - "Writing Part 2" hoặc "Writing Q6-7" → partNumber = 14, quantity = 2 (từ bảng mặc định)
                    - "Writing Part 3" hoặc "Writing Q8" → partNumber = 15, quantity = 1 (từ bảng mặc định)
                    
                * Nếu không thể xác định `partNumber`, trả về 0.

            2.  **`quantity` (Integer):** Số lượng **cụm đề bài (prompts)** hoặc **câu hỏi đơn lẻ** cần tạo.
                * **QUY TẮC ƯU TIÊN TUYỆT ĐỐI:**
                    1. **HIGHEST PRIORITY**: Nếu user CHỈ ĐỊNH BẤT KỲ CON SỐ NÀO, kể cả số 1 (ví dụ: "tạo 1 câu", "1 bài", "5 câu", "10 bài", "2 đoạn"), BẮT BUỘC phải lấy CHÍNH XÁC con số đó. 
                    2. **CRITICAL**: Số "1" cũng là một con số hợp lệ! "tạo 1 câu" = quantity 1, KHÔNG PHẢI lấy default!
                    3. Chỉ khi HOÀN TOÀN KHÔNG CÓ CON SỐ NÀO trong request (ví dụ: "tạo đề Part X", "cho tôi bài Part Y"), mới lấy giá trị từ bảng `defaultQuantities` bên dưới.
                * **⚠️ CỰC KỲ QUAN TRỌNG - TRÁNH NHẦM LẪN:**
                    - Bước 1: Xác định `partNumber` CHÍNH XÁC theo mapping ở trên
                    - Bước 2: Dùng `partNumber` ĐÃ MAPPING để tìm quantity trong bảng defaultQuantities
                    - ❌ SAI: "Speaking Part 2" → tìm key 2 trong bảng → 25 (WRONG!)
                    - ✅ ĐÚNG: "Speaking Part 2" → partNumber = 9 → tìm key 9 trong bảng → 2 (CORRECT!)
                * **Ví dụ phân biệt (CHÚ Ý ĐẶC BIỆT SỐ 1):**
                    - "tạo 1 câu part 2" → quantity = 1 ✅ (user nói "1", KHÔNG lấy default 25!)
                    - "tạo 2 câu part 2" → quantity = 2 ✅ (user nói "2")
                    - "tạo 5 câu part 2" → quantity = 5 ✅ (user nói "5")
                    - "tạo đề part 2" → quantity = 25 ✅ (không có số → lấy default)
                    - "cho 1 bài part 3" → quantity = 1 ✅ (user nói "1", KHÔNG lấy default 5!)
                    - "tạo đề speaking part 2" → partNumber = 9 → quantity = 2 ✅ (lấy default của key 9, KHÔNG PHẢI key 2!)
                * **Bảng số lượng mặc định (`defaultQuantities`) - CHỈ DÙNG KHI KHÔNG CÓ SỐ:**
                    ```json
                    {{{defaultQuantitiesJson}}}
                    ```
                    *(Giải thích bảng: Key là partNumber SAU KHI ĐÃ MAPPING, Value là số lượng prompts/items mặc định)*
                * Nếu không xác định được `partNumber`, `quantity` có thể là 1.

            3.  **`topic` (String | null):** Chủ đề cụ thể, điểm ngữ pháp, hoặc loại tình huống mà người dùng yêu cầu (ví dụ: "thì hiện tại hoàn thành", "email xin nghỉ phép", "họp trực tuyến", "chủ đề môi trường"). Nếu không có chủ đề nào được đề cập, trả về `null`. Cố gắng trích xuất chủ đề chính xác nhất có thể.

            **Yêu cầu đầu vào:**
            Yêu cầu của người dùng (dưới dạng JSON string): {{{jsonStringUserRequest}}}

            **Ví dụ phân tích:**

                * Input: `"tạo đề speaking part 2"`
                    Output: `{{ "partNumber": 9, "quantity": 2, "topic": null }}` ⚠️ CỰC KỲ QUAN TRỌNG: Speaking Part 2 = partNumber 9 → lấy quantity từ key 9 = 2, KHÔNG PHẢI key 2 = 25!
                * Input: `"tạo đề writing part 2"`
                    Output: `{{ "partNumber": 14, "quantity": 2, "topic": null }}` ⚠️ Writing Part 2 = partNumber 14 → lấy quantity từ key 14 = 2!
                * Input: `"tạo 1 câu listening part 2"`
                    Output: `{{ "partNumber": 2, "quantity": 1, "topic": null }}` ⚠️ User nói "1 câu" → quantity = 1, KHÔNG PHẢI 25!
                * Input: `"cho tôi 1 bài part 3"`
                    Output: `{{ "partNumber": 3, "quantity": 1, "topic": null }}` ⚠️ User nói "1 bài" → quantity = 1, KHÔNG PHẢI 5!
                * Input: `"tạo 2 câu part 2"`
                    Output: `{{ "partNumber": 2, "quantity": 2, "topic": null }}` (User nói "2 câu" → quantity = 2)
                * Input: `"tạo 5 câu Reading Part 5 về giới từ"`
                    Output: `{{ "partNumber": 5, "quantity": 5, "topic": "giới từ" }}`
                * Input: `"Cho tôi bài Listening Part 1"` (Không có số lượng)
                    Output: `{{ "partNumber": 1, "quantity": 6, "topic": null }}` (Lấy quantity=6 từ bảng mặc định)
                * Input: `"Tạo 1 đoạn hội thoại Listening Part 3 chủ đề đặt phòng khách sạn"`
                    Output: `{{ "partNumber": 3, "quantity": 1, "topic": "đặt phòng khách sạn" }}` (User nói "1 đoạn" → quantity = 1)
                * Input: `"tạo đề Listening Part 2"` (Không có số lượng)
                    Output: `{{ "partNumber": 2, "quantity": 25, "topic": null }}` (Lấy quantity=25 từ bảng)
                * Input: `"Reading Part 6 chủ đề môi trường"` (Không có số lượng)
                    Output: `{{ "partNumber": 6, "quantity": 4, "topic": "môi trường" }}` (Lấy quantity=4 từ bảng)
                * Input: `"Reading Part 7 double passage về review sản phẩm"`
                    Output: `{{ "partNumber": 7, "quantity": 5, "topic": "double passage review sản phẩm" }}` (Lấy quantity=5 từ bảng mặc định)
                * Input: `"Cho tôi đề Speaking Part 3"` (Không có số lượng)
                    Output: `{{ "partNumber": 10, "quantity": 1, "topic": null }}` (Speaking Part 3 → partNumber=10, lấy quantity=1)
                * Input: `"tạo đề Speaking Part 5 chủ đề công việc từ xa"`
                    Output: `{{ "partNumber": 12, "quantity": 1, "topic": "công việc từ xa" }}` (Speaking Part 5 → partNumber=12)
                * Input: `"làm giúp bài part 4 listening"` (Không có số lượng)
                    Output: `{{ "partNumber": 4, "quantity": 5, "topic": null }}` (Lấy quantity=5 từ bảng mặc định)
                * Input: `"tạo đề Writing Part 2"` (Không có số lượng)
                    Output: `{{ "partNumber": 14, "quantity": 2, "topic": null }}` (Writing Part 2 → partNumber=14, lấy quantity=2)
                * Input: `"cho tôi Writing Part 1"` (Không có số lượng)
                    Output: `{{ "partNumber": 13, "quantity": 5, "topic": null }}` (Writing Part 1 → partNumber=13, lấy quantity=5)
                    Output: `{{ "partNumber": 14, "quantity": 2, "topic": null }}` (Part 2 = Q6-7, lấy quantity=2 từ bảng)
                * Input: `"cho tôi Writing Part 1"` (Không có số lượng)
                    Output: `{{ "partNumber": 13, "quantity": 5, "topic": null }}` (Part 1 = Q1-5, lấy quantity=5 từ bảng)
                * Input: `"tạo đề thi"` (Không rõ part)
                    Output: `{{ "partNumber": 0, "quantity": 1, "topic": null }}`

            **Quan trọng:** Chỉ trả về đối tượng JSON đã phân tích, không thêm bất kỳ văn bản, giải thích hay markdown nào khác.
            """;
        }


        public static string GetGenerationPrompt(int partNumber, int quantity, string? topic)
        {
            string safeTopic = topic ?? "chủ đề kinh doanh và công sở tổng quát"; // Chủ đề mặc định

            switch (partNumber)
            {
                // Listening
                case 1: return CreateListeningPart1Prompt(quantity);
                case 2: return CreateListeningPart2Prompt(quantity);
                case 3: return CreateListeningPart3Prompt(quantity, safeTopic);
                case 4: return CreateListeningPart4Prompt(quantity, safeTopic);

                // Reading
                case 5: return CreateReadingPart5Prompt(quantity, topic);
                case 6: return CreateReadingPart6Prompt(quantity, safeTopic);
                case 7:
                    return CreateReadingPart7Prompt(quantity, safeTopic);

                // Speaking 
                case 8: return CreateSpeakingPart1Prompt(quantity);
                case 9: return CreateSpeakingPart2Prompt(quantity);
                case 10: return CreateSpeakingPart3Prompt(quantity, safeTopic);
                case 11: return CreateSpeakingPart4Prompt(quantity, safeTopic);
                case 12: return CreateSpeakingPart5Prompt(quantity, safeTopic);

                // Writing (Mapping giả định: 19=Q1-5, 20=Q6-7, 21=Q8)
                case 13: return CreateWritingPart1Prompt(quantity);
                case 14: return CreateWritingPart2Prompt(quantity, safeTopic);
                case 15: return CreateWritingPart3Prompt(quantity, safeTopic);

                default:
                    Console.WriteLine($"Warning: No generation prompt defined for Part {partNumber}.");
                    return "{\n  \"error\": \"Invalid part number requested.\"\n}";
            }
        }


        // --- Specific Prompt Generation Methods (Updated Examples) ---

        private static string CreateListeningPart1Prompt(int quantity)
        {
            // Ví dụ mẫu huấn luyện
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated TOEIC Listening Part 1 - Photographs",
                Skill = "Listening",
                PartLabel = "Part 1",
                Prompts = new List<AIGeneratedPromptDTO>
        {
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Photograph Description",
                Description = "You will see a picture and hear four statements about it. Choose the statement that best describes what you see.",
                ReferenceImageUrl = "A woman is pointing at a computer screen in an office. There is a man sitting next to her taking notes.", // Mô tả ảnh chi tiết để dùng tạo ảnh
                ReferenceAudioUrl = "A woman is pointing at a computer screen in an office. There is a man sitting next to her taking notes.", // Dạng text audio
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 1,
                        QuestionType = "Listening_Photograph",
                        StemText = "Choose the statement that best describes the picture.",
                        Explanation = "Lựa chọn (B) mô tả đúng nhất hành động trong ảnh: 'The woman is pointing at the screen'. Các lựa chọn khác không khớp với bối cảnh.",
                        ScoreWeight = 1,
                        Time = 60,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "The man is talking on the phone.", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "The woman is pointing at the screen.", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "The people are standing near the window.", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "The woman is typing on the keyboard.", IsCorrect = false }
                        }
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
            Bạn là một chuyên gia tạo đề thi **TOEIC Listening Part 1 – Photographs**.

            ---

            ### 🧩 Mô tả phần thi:
            - Ở phần này, thí sinh **xem một bức tranh** và **nghe bốn câu mô tả ngắn** (được phát một lần).
            - Nhiệm vụ của thí sinh là **chọn câu mô tả phù hợp nhất với hình ảnh**.
            - Có tổng cộng **6 câu (6 bức ảnh)** trong phần thi thật, mỗi câu gồm 1 ảnh và 4 lựa chọn.

            ---

            ### 🎯 Nhiệm vụ:
            Tạo ra **{quantity} bộ đề Part 1**, mỗi bộ tương ứng **1 bức ảnh**.  
            Với mỗi bộ đề (`Prompt`), cần bao gồm:

            1. **Mô tả ảnh (`ReferenceImageUrl`)**  
               - Viết **một mô tả chi tiết bằng tiếng Anh** cho bức ảnh (ví dụ: “A man is repairing a bicycle in front of a shop”).  
               - Mô tả này sẽ được dùng để **tạo ảnh minh họa bằng AI** sau này.  

            2. **Câu nói mô tả ảnh (`ReferenceAudioUrl`)**  
               - Mô tả chi tiết hình ảnh với 4 câu bằng tiếng Anh để người học hiểu rõ hơn về hình ảnh, đặt trong `ReferenceAudioUrl`.  
               - Các câu này là **âm thanh** mà thí sinh sẽ nghe.  

            3. **Tạo câu hỏi (`Questions`)**  
               - Mỗi `Question` đại diện cho **một bức ảnh**.  
               - Thêm giải thích (`Explanation`) ngắn gọn **bằng tiếng Việt**, nói rõ vì sao đáp án đúng.

            4. **Các trường bắt buộc khác:**  
               - `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `ScoreWeight`, `Time`.
           
            ---

            ### 🧠 Ví dụ cấu trúc JSON (1 ảnh mẫu):
            ```json
            {jsonExample}
            ```

            ---

            ### ⚠️ Lưu ý:
            - Trả về **một đối tượng JSON duy nhất** theo cấu trúc `AIGeneratedExamDTO`.
            - Không thêm bất kỳ markdown, text mô tả hay lời giải thích bên ngoài JSON.
            - Đảm bảo tất cả các chuỗi đều là tiếng Anh chuẩn, tự nhiên và dễ hiểu.

            Hãy bắt đầu tạo **{quantity} bộ đề Part 1 (Photographs)** ngay bây giờ.
            """;
        }


        private static string CreateListeningPart2Prompt(int quantity)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Listening Part 2",
                Skill = "Listening",
                PartLabel = "Part 2",
                Prompts = new List<AIGeneratedPromptDTO> 
                {
                    new AIGeneratedPromptDTO
                    {
                        ExamTitle = "Question-Response",
                        Description = "You will hear a question or statement and three responses...",
                        ReferenceAudioUrl = "Where is the marketing report?",
                        Questions = new List<AIGeneratedQuestionDTO> 
                        {
                            new AIGeneratedQuestionDTO
                            {
                                PartId = 2,
                                QuestionType = "Listening",
                                StemText = "Listen and choose the most appropriate answer?",
                                Explanation = "Câu hỏi 'Where' hỏi về địa điểm. Lựa chọn (B) là hợp lý nhất.",
                                ScoreWeight = 1, 
                                Time = 30,
                                Options = new List<AIGeneratedOptionDTO>
                                {
                                    new AIGeneratedOptionDTO { Label = "A", Content = "Yes, it was reported.", IsCorrect = false },
                                    new AIGeneratedOptionDTO { Label = "B", Content = "It's on your desk.", IsCorrect = true },
                                    new AIGeneratedOptionDTO { Label = "C", Content = "At 2:00 PM.", IsCorrect = false }
                                }
                            }
                        }
                    }
                }
            };
            
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            
            return $"""
    You are an expert TOEIC Listening Part 2 question generator.

    **CRITICAL REQUIREMENT:**
    - You MUST generate EXACTLY {quantity} prompts. NO MORE, NO LESS.
    - The Prompts array MUST contain precisely {quantity} items.
    - Count carefully before returning the JSON.

    **Structure (for EACH of the {quantity} prompts):**
    - 1 question/statement in ReferenceAudioUrl
    - 1 Question object with:
      - StemText (same as ReferenceAudioUrl)
      - 3 Options (A/B/C), only 1 correct
      - Vietnamese Explanation

    **Example (1 prompt):**
    ```json
    {jsonExample}
    ```

    **Validation before response:**
    - Check: Prompts.length === {quantity} ✓
    - Check: Each Prompt has 1 Question ✓
    - Check: Each Question has 3 Options ✓

    **Output format:**
    - Return ONLY valid JSON (AIGeneratedExamDTO)
    - No markdown blocks (```json)
    - No explanations
    - No extra text

    Generate EXACTLY {quantity} prompts now:
    """;
        }

        private static string CreateListeningPart3Prompt(int quantity, string topic)
        {

            Console.WriteLine("Quantity :" + quantity);
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Listening Part 3 ",
                Skill = "Listening",
                PartLabel = "Part 3",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Conversation about a Report",
                Description = "You will hear a question or statement and three responses...",
                ReferenceAudioUrl = "\"A woman asks a man, \\\"Hi Tom, do you have a moment to talk about the quarterly report?\\\" The man replies, \\\"Sure, come on in. I was just reviewing the sales data. Is there a problem?\\\" The woman says, \\\"Not a problem, but I think we should include the customer feedback from last month's survey. It provides some valuable insights.\\\" The man responds, \\\"That's a great idea. Can you summarize the key findings for me by noon?\\\"", // Cả hai đều dùng kịch bản tường thuật
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What are the speakers mainly discussing?",
                        Explanation = "Người kể chuyện cho biết cuộc trò chuyện bắt đầu về 'the quarterly report' và các chi tiết sau đó đều xoay quanh nó.",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Sales data", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="A quarterly report", IsCorrect=true},
                            new AIGeneratedOptionDTO { Label = "C", Content="A customer survey", IsCorrect=false},
                        }
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What does the woman suggest?",
                        Explanation = "Theo lời tường thuật, người phụ nữ nói 'I think we should include the customer feedback from last month's survey.'",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Reviewing the sales data", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="Changing the report's deadline", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "C", Content="Adding information from a survey", IsCorrect=true},
                        }
                    },
                     new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What does the man ask the woman to do?",
                        Explanation = "Người kể chuyện thuật lại lời người đàn ông yêu cầu: 'Can you summarize the key findings for me by noon?'.",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Finish the report immediately", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="Create a new survey", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "C", Content="Provide a summary of findings", IsCorrect=true}
                        }
                    }


                }

            },
             new AIGeneratedPromptDTO {
                ExamTitle = "Conversation about a Report",
                Description = "You will hear a question or statement and three responses...",
                ReferenceAudioUrl = "\"A woman asks a man, \\\"Hi Tom, do you have a moment to talk about the quarterly report?\\\" The man replies, \\\"Sure, come on in. I was just reviewing the sales data. Is there a problem?\\\" The woman says, \\\"Not a problem, but I think we should include the customer feedback from last month's survey. It provides some valuable insights.\\\" The man responds, \\\"That's a great idea. Can you summarize the key findings for me by noon?\\\"", // Cả hai đều dùng kịch bản tường thuật
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What are the speakers mainly discussing?",
                        Explanation = "Người kể chuyện cho biết cuộc trò chuyện bắt đầu về 'the quarterly report' và các chi tiết sau đó đều xoay quanh nó.",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Sales data", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="A quarterly report", IsCorrect=true},
                            new AIGeneratedOptionDTO { Label = "C", Content="A customer survey", IsCorrect=false},
                        }
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What does the woman suggest?",
                        Explanation = "Theo lời tường thuật, người phụ nữ nói 'I think we should include the customer feedback from last month's survey.'",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Reviewing the sales data", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="Changing the report's deadline", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "C", Content="Adding information from a survey", IsCorrect=true},
                        }
                    },
                     new AIGeneratedQuestionDTO {
                        PartId = 3, QuestionType = "MultipleChoice_Conversation",
                        StemText = "What does the man ask the woman to do?",
                        Explanation = "Người kể chuyện thuật lại lời người đàn ông yêu cầu: 'Can you summarize the key findings for me by noon?'.",
                        ScoreWeight = 1, Time = 30,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO { Label = "A", Content="Finish the report immediately", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "B", Content="Create a new survey", IsCorrect=false},
                            new AIGeneratedOptionDTO { Label = "C", Content="Provide a summary of findings", IsCorrect=true}
                        }
                    }


                }

            }
              // ... và cứ thế tiếp tục cho đến hết {quantity} prompts
        }
            };
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            // 3. Cập nhật lời nhắc để yêu cầu AI tạo kịch bản theo kiểu tường thuật
            return $"""
                Bạn là một chuyên gia tạo đề thi TOEIC Listening Part 3.

                **Nhiệm vụ:**
                Tạo ra một bài thi Listening Part 3 hoàn chỉnh, chứa chính xác **{quantity}** hội thoại (conversations). KHÔNG HƠN, KHÔNG KÉM

                **Cấu trúc dữ liệu QUAN TRỌNG:**
                - Mỗi hội thoại = 1 AIGeneratedPromptDTO
                - Mỗi hội thoại có đúng 3 câu hỏi trong Questions array
                - Tổng cộng: {quantity} prompts, mỗi prompt có 3 questions

                Chủ đề gợi ý: **"{topic}"**.

                **Yêu cầu QUAN TRỌNG về Kịch bản:**
                - Để phù hợp với hệ thống đọc văn bản (TTS) một giọng, kịch bản phải được viết theo **kiểu tường thuật (narrator style)**.
                - **KHÔNG** dùng các nhãn như `[Man]:` hay `[Woman]:`.
                - Thay vào đó, hãy mô tả ai đang nói. Ví dụ: `A man asks, "..."` sau đó `The woman replies, "..."`.
                - Điền kịch bản tường thuật này vào `ReferenceAudioUrl`.

                **Yêu cầu khác:**
                1. **Câu hỏi:** Mỗi hội thoại phải có **chính xác ba (3)** câu hỏi trắc nghiệm liên quan.
                2. **Lựa chọn:** Mỗi câu hỏi có **bốn (4)** lựa chọn (A, B, C, D) và chỉ **MỘT** đáp án đúng.
                3. **Giải thích:** Cung cấp giải thích ngắn gọn bằng tiếng Việt.

                **Định dạng đầu ra:**
                - Toàn bộ bài thi phải là một đối tượng JSON **AIGeneratedExamDTO** duy nhất.
                - Prompts array phải chứa đúng **{quantity}** items
                - Mỗi item trong Prompts phải có đúng 3 Questions

                **Ví dụ cấu trúc JSON (cho 1 hội thoại):**
                ```json
                {jsonExample}
                ```

                **Lưu ý:**
                Chỉ trả về đối tượng JSON, không thêm bất kỳ văn bản hay markdown nào khác.

                **Bây giờ, hãy bắt đầu tạo {quantity} hội thoại theo kiểu tường thuật.**
                """;
        }

        private static string CreateListeningPart4Prompt(int quantity, string topic)
        {

            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated TOEIC Listening Part 4 - {topic}",
                Skill = "Listening",
                PartLabel = "Part 4",
                Prompts = new List<AIGeneratedPromptDTO>
        {
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Company Picnic Announcement",
                Description = "You will hear a short talk given by one speaker. Listen carefully and answer the following questions.",
                ReferenceAudioUrl = "Good morning everyone. This is a reminder that our annual company picnic will be held this Saturday at Riverside Park. Please bring your family and enjoy a fun day of food and games. Lunch will be provided around noon.",
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 4,
                        QuestionType = "MultipleChoice_Talk",
                        StemText = "What is the purpose of the talk?",
                        Explanation = "The speaker says this is a reminder about the annual company picnic.",
                        ScoreWeight = 1,
                        Time = 30,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "To remind employees about a company event", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "B", Content = "To announce a new project", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "C", Content = "To introduce a new employee", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "To cancel a meeting", IsCorrect = false }
                        }
                    },
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 4,
                        QuestionType = "MultipleChoice_Talk",
                        StemText = "Where will the event take place?",
                        Explanation = "The speaker clearly mentions Riverside Park as the venue.",
                        ScoreWeight = 1,
                        Time = 30,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "At the company cafeteria", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "At Riverside Park", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "At the office building", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "At a local restaurant", IsCorrect = false }
                        }
                    },
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 4,
                        QuestionType = "MultipleChoice_Talk",
                        StemText = "What will be provided to participants?",
                        Explanation = "Lunch will be provided around noon.",
                        ScoreWeight = 1,
                        Time = 30,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "Transportation", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "Lunch", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "Souvenirs", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "Drinks only", IsCorrect = false }
                        }
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
                 Bạn là một chuyên gia tạo đề thi TOEIC Listening Part 4.

                 **Nhiệm vụ:**
                 Tạo ra một bài thi Listening Part 4 hoàn chỉnh, chứa chính xác **{quantity}** hội thoại (hội thoại). KHÔNG HƠN, KHÔNG KÉM

                 Chủ đề gợi ý: **"{topic}"**.

                 **Yêu cầu QUAN TRỌNG về Kịch bản:**
                 -   Để phù hợp với hệ thống đọc văn bản (TTS) một giọng, kịch bản phải được viết theo **kiểu tường thuật (narrator style)**.
                 -   **KHÔNG** dùng các nhãn như `[Man]:` hay `[Woman]:`.
                 -   Thay vào đó, hãy mô tả ai đang nói. Ví dụ: `A man asks, "..."` sau đó `The woman replies, "..."`.
                 -   Điền kịch bản tường thuật này vào trường `ReferenceAudioUrl`.

                 **Yêu cầu khác:**
                1.  **Câu hỏi:** Dựa vào bài nói, tạo ra **chính xác ba (3)** câu hỏi trắc nghiệm.
                2.  **Lựa chọn:** Mỗi câu hỏi phải có **bốn (4)** lựa chọn (A, B, C, D), trong đó chỉ có **MỘT** đáp án đúng.
                3.  **Giải thích:** Cung cấp giải thích ngắn gọn, rõ ràng bằng tiếng Việt cho đáp án đúng.

                 **Định dạng đầu ra:**
                 - Toàn bộ bài thi phải là một đối tượng JSON **AIGeneratedExamDTO** duy nhất.

                 **Ví dụ cấu trúc JSON (cho 1 cụm đề thi):**
                 ```json
                 {jsonExample}
                 ```

                 **Lưu ý:**
                 Chỉ trả về đối tượng JSON, không thêm bất kỳ văn bản hay markdown nào khác.

                 **Bây giờ, hãy bắt đầu tạo {quantity} cụm đề thi theo kiểu tường thuật.**
            """;
        }


        private static string CreateReadingPart5Prompt(int quantity, string? topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Reading Part 5",
                Skill = "Reading",
                PartLabel = "Part 5",
                Prompts = new List<AIGeneratedPromptDTO>
        {
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Incomplete Sentence",
                Description = "Choose the word or phrase that best completes the sentence.",
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 5,
                        QuestionType = "MultipleChoice_SentenceCompletion",
                        StemText = "The team ...... the project last month.",
                        Explanation = "Thì quá khứ đơn → 'completed'",
                        ScoreWeight = 1,
                        Time = 30,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "complete", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "completed", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "completes", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "completing", IsCorrect = false }
                        }
                    }
                }
            },
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Incomplete Sentence",
                Description = "Choose the word or phrase that best completes the sentence.",
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 5,
                        QuestionType = "MultipleChoice_SentenceCompletion",
                        StemText = "Submit reports ...... Friday.",
                        Explanation = "'By' chỉ thời hạn",
                        ScoreWeight = 1,
                        Time = 30,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "on", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "by", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "at", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "D", Content = "in", IsCorrect = false }
                        }
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

      
            return $"""
    You are an expert TOEIC Reading Part 5 question generator.

    🚨 CRITICAL STRUCTURE REQUIREMENT:
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    - Create EXACTLY {quantity} Prompt objects in the "Prompts" array
    - Each Prompt contains EXACTLY 1 Question
    - Structure: {quantity} Prompts → {quantity} independent sentences
    - Each sentence is COMPLETELY INDEPENDENT (no shared context)
    
    COUNT BEFORE RETURNING:
    - Prompts.length MUST equal {quantity}
    - Each Prompts[i].Questions.length MUST equal 1
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    **Topic/Focus:** {topic ?? "General business vocabulary and grammar"}

    **Each Prompt structure:**
    - ExamTitle: "Incomplete Sentence"
    - Description: "Choose the word or phrase that best completes the sentence."
    - Questions: Array with 1 item containing:
      - StemText: Incomplete sentence with "......" for the blank
      - 4 Options (A/B/C/D), only 1 IsCorrect=true
      - Explanation in Vietnamese
      - PartId: 5, QuestionType: "MultipleChoice_SentenceCompletion"
      - ScoreWeight: 1, Time: 30

    **Grammar/Vocabulary focus:**
    - Verb tenses, prepositions, word forms, conjunctions, quantifiers

    **Example (shows 2 INDEPENDENT prompts):**
    ```json
    {jsonExample}
    ```

    **Self-verification checklist:**
    ✓ Prompts array has {quantity} items (NOT 1 item with {quantity} questions!)
    ✓ Each Prompt has exactly 1 Question
    ✓ Each Question has 4 Options
    ✓ Only 1 Option has IsCorrect=true

    **Output requirements:**
    - Valid JSON only (AIGeneratedExamDTO structure)
    - No markdown code blocks (no ```json)
    - No explanations outside JSON

    Generate EXACTLY {quantity} INDEPENDENT prompts now:
    """;
        }

        private static string CreateReadingPart6Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Reading Part 6 - {topic}",
                Skill = "Reading",
                PartLabel = "Part 6",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi Prompt chứa 1 đoạn văn + 4 câu hỏi
                     new AIGeneratedPromptDTO {
                         ExamTitle = "Text Completion",
                         // Đoạn văn giờ nằm trong Description của Prompt
                         Description = "To: All Staff\nSubject: Office Renovation Update\nPlease be advised that the main entrance [__1__] closed next Monday due to ongoing renovations. Access to the building will be available [__2__] the rear entrance near the parking lot. [__3__]. We expect the work to be completed by Friday. Thank you for your [__4__].",
                         Questions = new List<AIGeneratedQuestionDTO> {
                             new AIGeneratedQuestionDTO {
                                 PartId = 6, QuestionType = "MultipleChoice_TextCompletion",
                                 StemText = "Select the best word for blank [1]", // Câu hỏi chỉ vị trí blank
                                 Explanation = "'Will be' is needed...",
                                 ScoreWeight = 1, Time = 45,
                                 Options = new List<AIGeneratedOptionDTO>
{
    new AIGeneratedOptionDTO { Label = "A", Content = "will be", IsCorrect = true },
    new AIGeneratedOptionDTO { Label = "B", Content = "was", IsCorrect = false },
    new AIGeneratedOptionDTO { Label = "C", Content = "is", IsCorrect = false },
    new AIGeneratedOptionDTO { Label = "D", Content = "were", IsCorrect = false }
}

                             },
                             new AIGeneratedQuestionDTO { /* Câu 2 */ },
                             new AIGeneratedQuestionDTO { /* Câu 3 */ },
                             new AIGeneratedQuestionDTO { /* Câu 4 */ }
                         }
                     }
                 }
            };
            // Cần điền đầy đủ options cho ví dụ trên
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            return $"""
            Bạn là một chuyên gia ra đề thi TOEIC Reading Part 6.

            **Yêu cầu:**
            - Tạo ra chính xác **{quantity}** cụm đề thi Part 6 (mỗi cụm là một đoạn văn).  KHÔNG HƠN, KHÔNG KÉM
            - Chủ đề chung: **{topic}**.
            - Mỗi cụm đề thi (prompt) phải bao gồm:
                - Một đoạn văn bản hoàn chỉnh (đặt trong `Description` của `AIGeneratedPromptDTO`) có **bốn (4)** chỗ trống `[__1__]` đến `[__4__]`.
                - **Bốn (4)** câu hỏi trắc nghiệm (`Questions` là `AIGeneratedQuestionDTO`), mỗi câu tương ứng một chỗ trống.
                    - `StemText` của câu hỏi chỉ rõ vị trí blank.
                - Cung cấp `Explanation` cho mỗi câu.
                - Điền các thông tin khác (`ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`...) như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây. Đảm bảo JSON là hợp lệ.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 đoạn văn):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} cụm đề thi.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            
            """;
        }

        private static string CreateReadingPart7Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Reading Part 7 - {topic}",
                Skill = "Reading",
                PartLabel = "Part 7",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Reading Comprehension - Single Passage",
                Description = "To: All Employees\nFrom: HR Department\nDate: March 15, 2024\nSubject: New Remote Work Policy\n\nDear Team,\n\nEffective April 1st, employees may work from home up to two days per week. To participate, please submit your preferred remote work schedule to your direct manager by March 25th. Note that employees in customer-facing roles may have limited flexibility due to operational needs. For questions, contact HR at hr@company.com.\n\nBest regards,\nHuman Resources",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 7,
                        QuestionType = "MultipleChoice_SinglePassage",
                        StemText = "What is the main purpose of the email?",
                        Explanation = "Email thông báo về chính sách làm việc từ xa mới ('New Remote Work Policy').",
                        ScoreWeight = 1,
                        Time = 60,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO{ Label="A", Content="To schedule a meeting", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="B", Content="To announce a policy change", IsCorrect=true },
                            new AIGeneratedOptionDTO{ Label="C", Content="To request employee feedback", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="D", Content="To introduce a new manager", IsCorrect=false }
                        }
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 7,
                        QuestionType = "MultipleChoice_SinglePassage",
                        StemText = "When will the new policy take effect?",
                        Explanation = "Email nói rõ 'Effective April 1st'.",
                        ScoreWeight = 1,
                        Time = 60,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO{ Label="A", Content="Immediately", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="B", Content="March 25th", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="C", Content="April 1st", IsCorrect=true },
                            new AIGeneratedOptionDTO{ Label="D", Content="Next quarter", IsCorrect=false }
                        }
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 7,
                        QuestionType = "MultipleChoice_SinglePassage",
                        StemText = "What are employees asked to do by March 25th?",
                        Explanation = "Email yêu cầu 'submit your preferred remote work schedule to your direct manager by March 25th'.",
                        ScoreWeight = 1,
                        Time = 60,
                        Options = new List<AIGeneratedOptionDTO>{
                            new AIGeneratedOptionDTO{ Label="A", Content="Contact HR", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="B", Content="Submit their work schedule", IsCorrect=true },
                            new AIGeneratedOptionDTO{ Label="C", Content="Attend a training session", IsCorrect=false },
                            new AIGeneratedOptionDTO{ Label="D", Content="Update their contact information", IsCorrect=false }
                        }
                    }
                }
            }
        }
            };
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            // Cập nhật yêu cầu prompt
            return $"""
            Bạn là một chuyên gia ra đề thi TOEIC Reading Part 7 (Single Passage).

            **Yêu cầu:**
            - Tạo ra một đối tượng JSON **AIGeneratedExamDTO** duy nhất.
            - Đối tượng này chứa một mảng `Prompts`. Mảng `Prompts` phải chứa **CHÍNH XÁC {quantity}** đối tượng `AIGeneratedPromptDTO` (mỗi đối tượng đại diện cho một đoạn văn đơn). KHÔNG HƠN, KHÔNG KÉM
            - Chủ đề chung cho các đoạn văn: **{topic}**. Các đoạn văn có thể là email, thông báo, quảng cáo, bài báo ngắn, form...
            - **Mỗi** `AIGeneratedPromptDTO` (đoạn văn) phải bao gồm:
                - MỘT đoạn văn hoàn chỉnh (đặt trong `Description`).
                - **Luôn luôn là Bốn (4)** câu hỏi trắc nghiệm (`Questions`) liên quan trực tiếp đến đoạn văn đó. Các câu hỏi kiểm tra đọc hiểu chi tiết, ý chính, suy luận, từ vựng trong ngữ cảnh.
                - Mỗi câu hỏi (`AIGeneratedQuestionDTO`) có `StemText`, 4 `Options` (`Label`, `Content`, `IsCorrect`), và `Explanation`. Chỉ MỘT lựa chọn đúng.
                - Điền các thông tin khác (`ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`...) như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây. Đảm bảo JSON là hợp lệ.
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} cụm đề thi (đoạn văn).**
            Hãy đảm bảo bạn chỉ trả về một JSON object AIGeneratedExamDTO duy nhất chứa đúng {quantity} prompt bên trong mảng Prompts. Mỗi prompt phải có đúng 3 question. Không thêm bất kỳ nội dung nào khác.
            """;
        }


        // --- Speaking Prompts (Updated DTOs) ---
        private static string CreateSpeakingPart1Prompt(int quantity)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Speaking Q1-2",
                Skill = "SPEAKING",
                PartLabel = "Q1-2",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Read a Text Aloud",
                Description = "Welcome to the City Museum's new exhibit on ancient Egypt. We are open daily from 9 AM to 5 PM. Tickets can be purchased online or at the entrance. Please note that photography without flash is permitted in most galleries. Enjoy your visit!",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 8,
                        QuestionType = "SPEAKING",
                        StemText = "Read the text aloud clearly and naturally.",
                        ScoreWeight = 3,
                        Time = 45,
                        // ✅ THÊM: Câu trả lời mẫu (đọc đúng text)
                        SampleAnswer = "Welcome to the City Museum's new exhibit on ancient Egypt. We are open daily from 9 AM to 5 PM. Tickets can be purchased online or at the entrance. Please note that photography without flash is permitted in most galleries. Enjoy your visit!"
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Speaking Questions 1-2 (Read a text aloud).

    **Yêu cầu:**
    - Tạo ra một đối tượng JSON **AIGeneratedExamDTO** duy nhất.
    - Mảng `Prompts` phải chứa **CHÍNH XÁC {quantity}** đối tượng `AIGeneratedPromptDTO` (mỗi đối tượng đại diện cho một đoạn văn cần đọc).
    - **Mỗi** `AIGeneratedPromptDTO`:
        - Phải chứa đoạn văn cần đọc (dài khoảng 100-150 từ) trong trường `Description`.
        - Phải chứa **MỘT** đối tượng `AIGeneratedQuestionDTO` trong mảng `Questions`. Đối tượng này dùng để lưu:
            - `PartId`: 8
            - `QuestionType`: "ReadAloud"
            - `StemText`: Hướng dẫn chung như "Read the text aloud clearly and naturally."
            - `ScoreWeight`: Điểm cho phần đọc (ví dụ: 3).
            - `Time`: Tổng thời gian cho phần đọc (ví dụ: 90 giây = 45s chuẩn bị + 45s đọc).
            - **`SampleAnswer`**: ✅ **BẮT BUỘC** - Câu trả lời mẫu (chính là nội dung đoạn văn cần đọc, copy từ `Description`)
    - Điền các thông tin khác (`ExamExamTitle`, `Skill`, `PartLabel`...) như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Ví dụ cấu trúc JSON đầu ra (cho 1 đoạn văn):**
    ```json
    {jsonExample}
    ```
    
    **Lưu ý về SampleAnswer:**
    - `SampleAnswer` PHẢI là **chính xác nội dung trong `Description`** (văn bản cần đọc)
    - Đây là bản mẫu để hệ thống so sánh với bản ghi âm của học viên
    
    **Hãy bắt đầu tạo {quantity} đoạn văn.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }

        private static string CreateSpeakingPart2Prompt(int quantity)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Speaking Q3",
                Skill = "SPEAKING",
                PartLabel = "Q3",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Describe a Picture",
                Description = "Look at the picture and describe it in as much detail as possible.",
                ReferenceImageUrl = "A group of people are sitting around a conference table in a modern office. There is a woman standing at a whiteboard presenting charts to the team. Laptops and documents are visible on the table.",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 9,
                        QuestionType = "SPEAKING",
                        StemText = "Describe the picture in detail.",
                        ScoreWeight = 3,
                        Time = 45,
                        SampleAnswer = "In this picture, I can see a business meeting taking place in a modern conference room. A group of approximately five people are sitting around a large conference table. They appear to be listening attentively to a female colleague who is standing at a whiteboard. She seems to be presenting some charts or data to the team. On the conference table, I can see several laptops and documents spread out, which suggests this is an important working session. The office looks professional and well-lit, creating a productive atmosphere for the meeting."
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Speaking Question 3 (Describe a picture).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** đề bài mô tả tranh.
    - Với mỗi đề bài:
        - **Tạo một mô tả chi tiết cho bức ảnh (đặt trong `ReferenceImageUrl` của `AIGeneratedPromptDTO`)**.
        - **✅ Tạo câu trả lời mẫu (`SampleAnswer`)**: Viết một đoạn văn mẫu (khoảng 100-150 từ) mô tả chi tiết bức ảnh theo phong cách TOEIC Speaking.
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Yêu cầu về SampleAnswer:**
    - Sử dụng cấu trúc: "In this picture, I can see..." → "There is/are..." → "It appears that..."
    - Mô tả chi tiết vị trí, hành động, bối cảnh
    - Ngôn ngữ tự nhiên, rõ ràng, phù hợp trình độ TOEIC

    **Ví dụ cấu trúc JSON đầu ra (cho 1 đề bài):**
    ```json
    {jsonExample}
    ```
    **Hãy bắt đầu tạo {quantity} đề bài.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }

        private static string CreateSpeakingPart3Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Speaking Q4-6 - {topic}",
                Skill = "SPEAKING",
                PartLabel = "Q4-6",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Respond to Questions",
                Description = "Imagine that a market research company is conducting a survey about people's reading habits. You have agreed to participate in a telephone interview.",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 10,
                        QuestionType = "SPEAKING",
                        StemText = "How often do you read books?",
                        ScoreWeight = 3,
                        Time = 60,
                        SampleAnswer = "I usually read books about two or three times a week, mostly in the evenings after work. On weekends, I try to spend more time reading, perhaps for an hour or two each day."
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 10,
                        QuestionType = "SPEAKING",
                        StemText = "What kind of books do you enjoy reading the most?",
                        ScoreWeight = 3,
                        Time = 60,
                        SampleAnswer = "I particularly enjoy reading mystery novels and business books. Mystery novels help me relax and escape from daily stress, while business books provide valuable insights for my career development."
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 10,
                        QuestionType = "SPEAKING",
                        StemText = "Describe your favorite place to read.",
                        ScoreWeight = 3,
                        Time = 60,
                        SampleAnswer = "My favorite place to read is a cozy corner in my living room. I have a comfortable armchair next to a large window that provides natural light during the day. There's a small side table where I can place my coffee and a reading lamp for evening reading. The quiet atmosphere and comfortable setting make it the perfect spot for me to concentrate on my books."
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Speaking Questions 4-6 (Respond to questions).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** bộ đề thi Q4-6.
    - Chủ đề chung: **{topic}**.
    - Mỗi bộ đề (prompt) phải bao gồm:
        - Một đoạn văn ngắn giới thiệu tình huống (đặt trong `Description` của `AIGeneratedPromptDTO`).
        - **Ba (3)** câu hỏi nói (`Questions` là `AIGeneratedQuestionDTO`), `StemText` là nội dung câu hỏi.
        - **✅ Mỗi câu hỏi phải có `SampleAnswer`**:
            - Câu 1-2: Câu trả lời ngắn (30-40 từ)
            - Câu 3: Câu trả lời dài hơn (40-60 từ)
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Yêu cầu về SampleAnswer:**
    - Trả lời trực tiếp câu hỏi
    - Sử dụng ngôn ngữ tự nhiên, rõ ràng
    - Cung cấp chi tiết cụ thể, ví dụ minh họa
    - Phù hợp với độ dài yêu cầu (câu 1-2 ngắn, câu 3 dài hơn)

    **Ví dụ cấu trúc JSON đầu ra (cho 1 bộ đề):**
    ```json
    {jsonExample}
    ```
    **Hãy bắt đầu tạo {quantity} bộ đề.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }

        private static string CreateSpeakingPart4Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Speaking Q7-9 - {topic}",
                Skill = "SPEAKING",
                PartLabel = "Q7-9",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Respond using Information",
                Description = "**Conference Schedule**\n9:00 AM: Opening Remarks\n10:00 AM: Workshop A - Marketing Strategies\n11:00 AM: Coffee Break\n11:30 AM: Workshop B - Financial Planning\n1:00 PM: Lunch",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 11,
                        QuestionType = "SPEAKING",
                        StemText = "What time does the conference begin?",
                        ScoreWeight = 3,
                        Time = 30,
                        SampleAnswer = "According to the schedule, the conference begins at 9:00 AM with the opening remarks."
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 11,
                        QuestionType = "SPEAKING",
                        StemText = "Could you tell me what Workshop A is about?",
                        ScoreWeight = 3,
                        Time = 30,
                        SampleAnswer = "Certainly. Workshop A, which takes place at 10:00 AM, focuses on Marketing Strategies."
                    },
                    new AIGeneratedQuestionDTO {
                        PartId = 11,
                        QuestionType = "SPEAKING",
                        StemText = "How long is the coffee break?",
                        ScoreWeight = 3,
                        Time = 30,
                        SampleAnswer = "Based on the schedule, the coffee break starts at 11:00 AM and Workshop B begins at 11:30 AM, so the coffee break lasts for 30 minutes."
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Speaking Questions 7-9 (Respond to questions using information provided).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** bộ đề thi Q7-9.
    - Chủ đề chung: **{topic}**.
    - Mỗi bộ đề (prompt) phải bao gồm:
        - Một đoạn văn bản chứa thông tin có cấu trúc (đặt trong `Description` của `AIGeneratedPromptDTO`).
        - **Ba (3)** câu hỏi nói (`Questions`) yêu cầu tìm và trình bày thông tin. `StemText` là nội dung câu hỏi.
        - **✅ Mỗi câu hỏi phải có `SampleAnswer`**: Câu trả lời mẫu dựa trên thông tin đã cho (40-60 từ mỗi câu)
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Yêu cầu về SampleAnswer:**
    - Trích dẫn chính xác thông tin từ `Description`
    - Sử dụng cụm từ giới thiệu: "According to...", "Based on the information...", "The schedule shows that..."
    - Trả lời đầy đủ, rõ ràng, không thêm thông tin không có trong nguồn

    **Ví dụ cấu trúc JSON đầu ra (cho 1 bộ đề):**
    ```json
    {jsonExample}
    ```
    **Hãy bắt đầu tạo {quantity} bộ đề.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }

        private static string CreateSpeakingPart5Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Speaking Q11 - {topic}",
                Skill = "Speaking",
                PartLabel = "Q11",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Express an Opinion",
                Description = "Listen to the question. Then give your opinion and support it with reasons and examples.",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 12,
                        QuestionType = "SPEAKING",
                        StemText = "Some people prefer to work for a large company, while others prefer a small company. Which do you prefer and why? Include specific reasons and examples to support your opinion.",
                        ScoreWeight = 5,
                        Time = 60,
                        SampleAnswer = "I personally prefer working for a large company for several reasons. First, large companies typically offer better career advancement opportunities. They have well-defined career paths and provide regular training programs that help employees develop their skills. For example, when I worked at a multinational corporation, I had access to professional development courses and mentorship programs that significantly improved my capabilities.\n\nSecond, large companies usually provide more comprehensive benefits packages, including health insurance, retirement plans, and paid vacation time. These benefits contribute to better work-life balance and financial security.\n\nFinally, working for a large company allows me to collaborate with diverse teams and learn from experienced professionals. This exposure to different perspectives and expertise has been invaluable for my professional growth.\n\nWhile I acknowledge that small companies can offer more flexibility and closer relationships with colleagues, I believe the structured environment and resources available at large companies better align with my career goals and personal preferences."
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Speaking Question 11 (Express an opinion).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** câu hỏi yêu cầu bày tỏ quan điểm Q11.
    - Chủ đề chung: **{topic}**.
    - Mỗi câu hỏi (prompt) phải:
        - Là một câu hỏi trực tiếp (`StemText` của `AIGeneratedQuestionDTO`) về một vấn đề phổ biến, yêu cầu thí sinh chọn phe và bảo vệ quan điểm bằng lý lẽ/ví dụ.
        - **✅ Phải có `SampleAnswer`**: Câu trả lời mẫu dài và chi tiết (120-150 từ), bao gồm:
            - Câu mở đầu: Nêu quan điểm rõ ràng
            - 2-3 lý do chính, mỗi lý do có ví dụ minh họa
            - Câu kết: Tổng kết lại quan điểm
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Cấu trúc SampleAnswer:**
    1. **Introduction** (20-30 từ): Nêu rõ quan điểm
    2. **Reason 1 + Example** (20-30 từ)
    3. **Reason 2 + Example** (20-30 từ)
    4. **Conclusion** (20-30 từ): Khẳng định lại quan điểm

    **Ví dụ cấu trúc JSON đầu ra (cho 1 câu hỏi):**
    ```json
    {jsonExample}
    ```
    **Hãy bắt đầu tạo {quantity} câu hỏi.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }


        // --- Writing Prompts (Updated DTOs) ---
        private static string CreateWritingPart1Prompt(int quantity)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Writing Q1-5",
                Skill = "Writing",
                PartLabel = "Q1-5",
                Prompts = new List<AIGeneratedPromptDTO>
        {
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Write a sentence based on a picture",
                // Hai từ/cụm từ được cung cấp
                Description = "agreement / sign",
                // Mô tả ảnh (để sinh hình)
                ReferenceImageUrl = "Two people are shaking hands across a desk in an office after signing a contract.",
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 19,
                        QuestionType = "WRITING",
                        // Hướng dẫn cố định
                        StemText = "Write ONE sentence based on the picture using the TWO words or phrases provided.",
                        // Câu mẫu đúng (để AI học cách viết)
                        CorrectAnswer = "null",
                        ScoreWeight = 3,
                        Time = 90
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
            Bạn là một chuyên gia ra đề thi **TOEIC Writing Questions 1–5** (Write a sentence based on a picture).

            ---

            ### 🧩 Mô tả phần thi:
            - Ở phần này, thí sinh sẽ **nhìn một bức ảnh** và **được cung cấp hai từ hoặc cụm từ**.  
            - Nhiệm vụ của thí sinh là **viết MỘT câu hoàn chỉnh** mô tả bức ảnh, **sử dụng cả hai từ/cụm từ** đã cho.  
            - Thí sinh có **8 phút cho 5 câu hỏi (khoảng 1.5 phút mỗi câu).**

            ---

            ### 🎯 Nhiệm vụ:
            Hãy tạo ra **{quantity} bộ đề Writing Q1–5**, mỗi bộ gồm:

            1. **Mô tả ảnh (`ReferenceImageUrl`)**  
               - Viết **một mô tả chi tiết bằng tiếng Anh** cho bức ảnh (ví dụ: “A man is repairing a bicycle in front of a shop”).  
               - Mô tả này sẽ được dùng để **tạo ảnh minh họa bằng AI** sau này.  

            2. **Hai từ hoặc cụm từ (`Description`)**  
               - Cung cấp hai từ hoặc cụm từ mà thí sinh bắt buộc phải sử dụng trong câu.  
               - Ví dụ: `"coffee / morning"` hoặc `"meeting / report"`.

            3. **Câu hỏi (`StemText`)**  
               - Ghi chính xác hướng dẫn:  
                 `"Write ONE sentence based on the picture using the TWO words or phrases provided."`

            4. **Câu mẫu đúng (`CorrectAnswer`)**  
               - Viết một câu hoàn chỉnh đúng ngữ pháp, tự nhiên, và có chứa cả hai từ/cụm từ đã cho.

            5. **Thông tin khác:**  
               - `PartId = 19`, `QuestionType = SentenceFromImageAndWords`, `ScoreWeight = 3`, `Time = 90`.

            ---

            ### 🧠 Ví dụ cấu trúc JSON (1 câu mẫu):
            ```json
            {jsonExample}
            ```

            ---

            ### ⚠️ Lưu ý:
            - Chỉ trả về **một đối tượng JSON duy nhất** theo cấu trúc `AIGeneratedExamDTO`.  
            - Không thêm markdown, mô tả, hay lời giải thích bên ngoài.  
            - Đảm bảo `ReferenceImageUrl` và `Description` được điền rõ ràng, tự nhiên, bằng tiếng Anh.

            Hãy bắt đầu tạo **{quantity} bộ đề Writing Q1–5** ngay bây giờ.
            """;
        }

        private static string CreateWritingPart2Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Writing Q6-7 - {topic}",
                Skill = "Writing",
                PartLabel = "Q6-7",
                Prompts = new List<AIGeneratedPromptDTO> {
            new AIGeneratedPromptDTO {
                ExamTitle = "Respond to a written request",
                // Email yêu cầu nằm trong Description
                Description = "From: Marketing Department\nTo: All Staff\nSubject: Customer Satisfaction Survey\n\nDear Team Members,\n\nWe are conducting our annual customer satisfaction survey next month. We need volunteers to help distribute and collect survey forms at our main office. The task will take approximately 2 hours on March 15th. If you are interested, please reply to this email by March 1st.\n\nThank you,\nMarketing Team",
                Questions = new List<AIGeneratedQuestionDTO> {
                    new AIGeneratedQuestionDTO {
                        PartId = 14, 
                        QuestionType = "WRITING",
                        // Hướng dẫn viết nằm trong StemText
                        StemText = "Read the email. Respond to the Marketing Team as a staff member. In your email, ask TWO questions and make ONE suggestion about the survey.",
                        // Email mẫu nằm trong Explanation
                        Explanation = "Dear Marketing Team,\n\nThank you for organizing the customer satisfaction survey. I am interested in volunteering to help.\n\nI have two questions: First, what time should volunteers arrive on March 15th? Second, will we need any special training before distributing the forms?\n\nI would also like to suggest that we provide small incentives, such as discount coupons, to encourage more customers to complete the survey.\n\nI look forward to hearing from you.\n\nBest regards,\n[Your Name]",
                        ScoreWeight = 4,
                        Time = 600 // 10 phút
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Writing Questions 6-7 (Respond to a written request).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** đề bài Q6-7.
    - Chủ đề chung: **{topic}**.
    - Mỗi đề bài (prompt) phải bao gồm:
        - Một email yêu cầu hoàn chỉnh (đặt trong `Description` của `AIGeneratedPromptDTO`).
        - Một câu hỏi/hướng dẫn (`StemText` của `AIGeneratedQuestionDTO`) yêu cầu viết email trả lời theo vai trò cụ thể.
        - Yêu cầu phải bao gồm: **Hỏi HAI (2) câu hỏi** và **Đưa ra MỘT (1) đề xuất/gợi ý**.
        - Cung cấp một email trả lời mẫu hoàn chỉnh (`Explanation`) thể hiện rõ 2 câu hỏi và 1 đề xuất.
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, **`PartId = 14`**, `QuestionType`, `Time`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Ví dụ cấu trúc JSON đầu ra (cho 1 đề bài):**
    ```json
    {jsonExample}
    ```
    
    **Cấu trúc email trả lời mẫu phải có:**
    1. Lời chào/mở đầu
    2. Câu hỏi thứ nhất (Question 1)
    3. Câu hỏi thứ hai (Question 2)
    4. Một đề xuất/gợi ý (Suggestion)
    5. Lời kết

    **Hãy bắt đầu tạo {quantity} đề bài.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    """;
        }
        private static string CreateWritingPart3Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Writing Q8 - {topic}",
                Skill = "Writing",
                PartLabel = "Q8",
                Prompts = new List<AIGeneratedPromptDTO>
        {
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Write an opinion essay",
                Description = "Read the question below and write an essay expressing your opinion. Give reasons and examples to support your view. Aim for approximately 300 words.",
                Questions = new List<AIGeneratedQuestionDTO>
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 2,
                        QuestionType = "WRITING",
                        StemText = "Do you agree or disagree with the following statement? 'Technology makes people less creative.' Use specific reasons and examples to support your answer.",
                        Explanation = "[Sample Outline:\n- Introduction: State opinion (agree/disagree).\n- Body Paragraph 1: Reason 1 + Example...\n- Conclusion: Restate opinion...]",
                        ScoreWeight = 5,
                        Time = 1800
                    }
                }
            }
        }
            };

            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);

            return $"""
    Bạn là một chuyên gia ra đề thi TOEIC Writing Question 8 (Write an opinion essay).

    **Yêu cầu:**
    - Tạo ra chính xác **{quantity}** đề bài luận Q8.
    - Chủ đề chung: **{topic}**.
    - Mỗi đề bài (prompt) phải:
        - Là một câu hỏi hoặc tuyên bố (`StemText` của `AIGeneratedQuestionDTO`) yêu cầu bày tỏ quan điểm và bảo vệ bằng lý lẽ/ví dụ.
        - Cung cấp một dàn ý gợi ý (`Explanation`).
        - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
    - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

    **Ví dụ cấu trúc JSON đầu ra (cho 1 đề bài):**
    ```json
    {jsonExample}
    ```
    **Hãy bắt đầu tạo {quantity} đề bài.**
    Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
    
    """;
        }

    
    }
}