using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using DataLayer.DTOs.AIGeneratedExam;
using System.Text.RegularExpressions;

namespace ServiceLayer.AI.Prompt
{
    public static class PromptFactory
    {
        // --- CreateParsingPrompt ---
        // (Giữ nguyên, vì nó chỉ phân tích yêu cầu ban đầu, không liên quan trực tiếp đến DTO output)
        private static int ExtractQuantity(string text)
        {
            var match = Regex.Match(text, @"\b(\d+)\b");
            return match.Success && int.TryParse(match.Value, out int q) ? q : 1;
        }

        private static string ExtractTopic(string text)
        {
            var match = Regex.Match(text, @"(chủ đề|về)\s+([^\d]+)$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null;
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
            { 3, 5 },  // Listening Part 3: 13 prompts (hội thoại)
            { 4, 5 },  // Listening Part 4: 10 prompts (bài nói)
            { 5, 30 },  // Reading Part 5: 30 prompts (câu hỏi)
            { 6, 4 },   // Reading Part 6: 4 prompts (đoạn văn)
            { 7, 5 },  // Reading Part 7: Tổng cộng ~15 cụm (10 single, 2 double, 3 triple)
            { 8, 2 },   // Speaking Q1-2: 2 prompts (đoạn văn đọc)
            { 9, 1 },   // Speaking Q3: 1 prompt (ảnh)
            { 10, 1 },  // Speaking Q4-6: 1 prompt (tình huống)
            { 11, 1 },  // Speaking Q7-10: 1 prompt (thông tin)
            { 12, 1 },   // Speaking Q11: 1 prompt (ý kiến)
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
                    * Reading Part 5 -> 5
                    * Reading Part 6 -> 6
                    * Reading Part 7 -> 7
                    * Speaking Part1 (Read Aloud) -> 8
                    * Speaking Part2 (Describe Picture) -> 9
                    * Speaking Part3 (Respond Questions Scenario) -> 10
                    * Speaking Part4 (Respond Questions Info) -> 11
                    * Speaking Part5 (Express Opinion) -> 12
                    * Writing Part1 (Sentence from Picture) -> 13
                    * Writing Part2 (Respond Email) -> 14
                    * Writing Part3 (Opinion Essay) -> 15
                * Nếu không thể xác định `partNumber`, trả về 0.

            2.  **`quantity` (Integer):** Số lượng **cụm đề bài (prompts)** hoặc **câu hỏi đơn lẻ** cần tạo.
                * **Ưu tiên:** Nếu người dùng chỉ định rõ số lượng (ví dụ: "tạo 5 câu", "cho tôi 2 bài nói", "10 câu part 5"), hãy lấy chính xác con số đó.
                * **Mặc định:** Nếu người dùng KHÔNG chỉ định số lượng mà chỉ nói "tạo đề Part X" hoặc "cho tôi bài Part Y", hãy sử dụng số lượng mặc định chuẩn của phần thi đó dựa trên bảng dưới đây.
                * **Bảng số lượng mặc định (`defaultQuantities`):**
                    ```json
                    {{{defaultQuantitiesJson}}}
                    ```
                    *(Giải thích bảng: Key là partNumber, Value là số lượng prompts/items mặc định cần tạo cho part đó)*
                * Nếu không xác định được `partNumber`, `quantity` có thể là 1.

            3.  **`topic` (String | null):** Chủ đề cụ thể, điểm ngữ pháp, hoặc loại tình huống mà người dùng yêu cầu (ví dụ: "thì hiện tại hoàn thành", "email xin nghỉ phép", "họp trực tuyến", "chủ đề môi trường"). Nếu không có chủ đề nào được đề cập, trả về `null`. Cố gắng trích xuất chủ đề chính xác nhất có thể.

            **Yêu cầu đầu vào:**
            Yêu cầu của người dùng (dưới dạng JSON string): {{{jsonStringUserRequest}}}

            **Ví dụ phân tích:**

                * Input: `"tạo 5 câu Reading Part 5 về giới từ"`
                    Output: `{{ "partNumber": 5, "quantity": 5, "topic": "giới từ" }}`
                * Input: `"Cho tôi bài Listening Part 1"` (Không có số lượng)
                    Output: `{{ "partNumber": 1, "quantity": 6, "topic": null }}` (Lấy quantity=6 từ bảng mặc định)
                * Input: `"Tạo 1 đoạn hội thoại Listening Part 3 chủ đề đặt phòng khách sạn"`
                    Output: `{{ "partNumber": 3, "quantity": 1, "topic": "đặt phòng khách sạn" }}`
                * Input: `"Reading Part 7 double passage về review sản phẩm"`
                    Output: `{{ "partNumber": 7, "quantity": 2, "topic": "double passage review sản phẩm" }}` (Lấy quantity=2 từ bảng, topic bao gồm cả loại passage)
                * Input: `"Cho tôi đề Speaking task 11"` (Không có số lượng)
                    Output: `{{ "partNumber": 13, "quantity": 1, "topic": null }}` (Lấy quantity=1 từ bảng)
                * Input: `"tạo đề writing question 8 chủ đề công việc từ xa"`
                    Output: `{{ "partNumber": 21, "quantity": 1, "topic": "công việc từ xa" }}` (Lấy quantity=1 từ bảng)
                * Input: `"làm giúp bài part 4 listening"` (Không có số lượng)
                    Output: `{{ "partNumber": 4, "quantity": 10, "topic": null }}` (Lấy quantity=10 từ bảng)
                * Input: `"tạo đề thi"` (Không rõ part)
                    Output: `{{ "partNumber": 0, "quantity": 1, "topic": null }}`

            **Quan trọng:** Chỉ trả về đối tượng JSON đã phân tích, không thêm bất kỳ văn bản, giải thích hay markdown nào khác.
            """;
        }

        // --- GetGenerationPrompt (Dispatcher) ---
        // (Giữ nguyên logic)
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

                // Speaking (Mapping giả định: 8=Q1-2, 9=Q3, 10=Q4-6, 11=Q7-9, 12=Q10, 13=Q11)
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
                        Explanation = "Lựa chọn (A) mô tả đúng nhất hành động trong ảnh: 'The woman is pointing at the screen'. Các lựa chọn khác không khớp với bối cảnh.",
                        ScoreWeight = 1,
                        Time = 60,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "The woman is pointing at the screen.", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "B", Content = "The man is talking on the phone.", IsCorrect = false },
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
                Prompts = new List<AIGeneratedPromptDTO> // Danh sách chứa nhiều Prompts
        {
            // Prompt cho câu hỏi đầu tiên
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Question-Response",
                Description = "You will hear a question or statement and three responses...",
                ReferenceAudioUrl = "Where is the marketing report?",
                Questions = new List<AIGeneratedQuestionDTO> // Danh sách này chỉ chứa 1 câu hỏi
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 2,
                        QuestionType = "Listening",
                        StemText = "Where is the marketing report?",
                        Explanation = "Câu hỏi 'Where' hỏi về địa điểm. Lựa chọn (B) 'It's on your desk.' là câu trả lời hợp lý nhất.",
                        ScoreWeight = 1, Time = 5,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "Yes, it was reported.", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "It's on your desk.", IsCorrect = true },
                            new AIGeneratedOptionDTO { Label = "C", Content = "At 2:00 PM.", IsCorrect = false }
                        }
                    }
                }
            },
            // Prompt cho câu hỏi thứ hai (để làm mẫu)
            new AIGeneratedPromptDTO
            {
                ExamTitle = "Question-Response",
                Description = "You will hear a question or statement and three responses...",
                ReferenceAudioUrl = "When should I send this package?",
                Questions = new List<AIGeneratedQuestionDTO> // Danh sách này cũng chỉ chứa 1 câu hỏi
                {
                    new AIGeneratedQuestionDTO
                    {
                        PartId = 2,
                        QuestionType = "Listening",
                        StemText = "When should I send this package?",
                        Explanation = "Câu hỏi 'When' hỏi về thời gian. Lựa chọn (C) 'Before lunchtime.' là câu trả lời hợp lý nhất.",
                        ScoreWeight = 1, Time = 45,
                        Options = new List<AIGeneratedOptionDTO>
                        {
                            new AIGeneratedOptionDTO { Label = "A", Content = "By express mail.", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "B", Content = "To the new client.", IsCorrect = false },
                            new AIGeneratedOptionDTO { Label = "C", Content = "Before lunchtime.", IsCorrect = true }
                        }
                    }
                }
            }
            // ... và cứ thế tiếp tục cho đến hết {quantity} prompts
                 }
            };
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            return $"""
            Bạn là một chuyên gia ra đề thi TOEIC Listening Part 2.

            **Yêu cầu:**
            - Tạo ra chính xác **{quantity}** bộ đề thi Part 2 (mỗi bộ là một câu hỏi-đáp).
            - Mỗi bộ đề bao gồm:
                - Một câu hỏi hoặc một câu nói ngắn (đặt trong `StemText` của `AIGeneratedQuestionDTO`) mà thí sinh sẽ nghe.
                - Ba (3) câu lựa chọn trả lời (đặt trong `Content` của `AIGeneratedOptionDTO`).
                - Chỉ MỘT lựa chọn đúng (`IsCorrect: true`). Hai lựa chọn còn lại sai.
                - Cung cấp giải thích (`Explanation`) ngắn gọn.
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây (Lưu ý: mỗi câu hỏi Part 2 nằm trong một `prompt` riêng). Đảm bảo JSON là hợp lệ.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 bộ đề):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} bộ đề.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            
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
                 -   Điền kịch bản tường thuật này vào cả hai trường `Description` và `ReferenceAudioUrl`.

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


        // --- Reading Prompts (Updated DTOs) ---
        private static string CreateReadingPart5Prompt(int quantity, string? topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Reading Part 5 - {topic ?? "Tự do"}",
                Skill = "Reading",
                PartLabel = "Part 5",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi Prompt chứa 1 câu hỏi Part 5
                       new AIGeneratedPromptDTO
                       {
                           ExamTitle = "Incomplete Sentence",
                           Description = "Choose the word or phrase that best completes the sentence.",
                           Questions = new List<AIGeneratedQuestionDTO>
                           {
                               new AIGeneratedQuestionDTO
                               {
                                   PartId = 5, QuestionType = "MultipleChoice_SentenceCompletion",
                                   StemText = "The marketing team ...... a new advertising campaign last month.",
                                   Explanation = "Câu này ở thì quá khứ đơn...",
                                   ScoreWeight = 1, Time = 30,
                                   Options = new List<AIGeneratedOptionDTO>
                                   {
                                       new AIGeneratedOptionDTO { Label = "A", Content = "launch", IsCorrect = false },
                                       new AIGeneratedOptionDTO { Label = "B", Content = "launches", IsCorrect = false },
                                       new AIGeneratedOptionDTO { Label = "C", Content = "launched", IsCorrect = true },
                                       new AIGeneratedOptionDTO { Label = "D", Content = "launching", IsCorrect = false }
                                   }
                               }
                           }
                       }
                 }
            };
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            return $"""
            Bạn là một chuyên gia ra đề thi TOEIC Reading Part 5.

            **Yêu cầu:**
            - Tạo ra chính xác **{quantity}** bộ đề thi Part 5 (mỗi bộ là một câu hỏi).
            - {(string.IsNullOrWhiteSpace(topic) ? "Chủ đề hoặc điểm ngữ pháp tự do." : $"Tập trung vào chủ đề: **{topic}**.")}
            - Mỗi bộ đề bao gồm:
                - Một câu chưa hoàn chỉnh (đặt trong `StemText` của `AIGeneratedQuestionDTO`).
                - Bốn (4) lựa chọn (`Options` là `AIGeneratedOptionDTO`). Chỉ MỘT lựa chọn đúng (`IsCorrect: true`).
                - Giải thích (`Explanation`) ngắn gọn.
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây (Lưu ý: mỗi câu hỏi Part 5 nằm trong một `prompt` riêng). Đảm bảo JSON là hợp lệ.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 câu hỏi):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} bộ đề.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            
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
                         Description = "To: All Staff\nSubject: Office Renovation Update\nPlease be advised that the main entrance [BLANK_1] closed next Monday due to ongoing renovations. Access to the building will be available [BLANK_2] the rear entrance near the parking lot. [BLANK_3]. We expect the work to be completed by Friday. Thank you for your [BLANK_4].",
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
                             new AIGeneratedQuestionDTO { /* Câu 3 (Điền câu) */ },
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
                - Một đoạn văn bản hoàn chỉnh (đặt trong `Description` của `AIGeneratedPromptDTO`) có **bốn (4)** chỗ trống `[BLANK_1]` đến `[BLANK_4]`.
                - **Bốn (4)** câu hỏi trắc nghiệm (`Questions` là `AIGeneratedQuestionDTO`), mỗi câu tương ứng một chỗ trống.
                    - `StemText` của câu hỏi chỉ rõ vị trí blank.
                    - Câu hỏi thứ 3 hoặc 4 thường yêu cầu điền cả câu.
                - Mỗi câu hỏi có 4 `Options`. Chỉ MỘT lựa chọn đúng.
                - Cung cấp `Explanation` cho mỗi câu.
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`... như trong ví dụ.
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
            // Luôn tạo 2 câu hỏi cho mỗi đoạn văn
            int questionsPerPassage = 2;
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Reading Part 7 - {topic}",
                Skill = "Reading",
                PartLabel = "Part 7",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi Prompt chứa 1 đoạn văn + 2 câu hỏi
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Reading Comprehension - Single Passage",
                        Description = "[Sample email about a company policy change...]", // Đoạn văn
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 7, QuestionType = "MultipleChoice_SinglePassage",
                                StemText = "What is the main purpose of the email?",
                                Explanation = "The email announces a change to the company's remote work policy.",
                                ScoreWeight = 1, Time = 60,
                                Options = new List<AIGeneratedOptionDTO>{
                                    new AIGeneratedOptionDTO{ Label="A", Content="To schedule a meeting", IsCorrect=false },
                                    new AIGeneratedOptionDTO{ Label="B", Content="To announce a policy change", IsCorrect=true },
                                    new AIGeneratedOptionDTO{ Label="C", Content="To request employee feedback", IsCorrect=false },
                                    new AIGeneratedOptionDTO{ Label="D", Content="To introduce a new manager", IsCorrect=false }
                                }
                            },
                            new AIGeneratedQuestionDTO {
                                PartId = 7, QuestionType = "MultipleChoice_SinglePassage",
                                StemText = "According to the email, when will the change take effect?",
                                Explanation = "The email states the policy will be effective 'starting next month'.",
                                ScoreWeight = 1, Time = 60,
                                Options = new List<AIGeneratedOptionDTO>{
                                    new AIGeneratedOptionDTO{ Label="A", Content="Immediately", IsCorrect=false },
                                    new AIGeneratedOptionDTO{ Label="B", Content="Next week", IsCorrect=false },
                                    new AIGeneratedOptionDTO{ Label="C", Content="Next month", IsCorrect=true },
                                    new AIGeneratedOptionDTO{ Label="D", Content="Next quarter", IsCorrect=false }
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

            **Ví dụ cấu trúc JSON đầu ra (cho một cụm Single Passage với 2 câu hỏi):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} cụm đề thi (đoạn văn).**
            Hãy đảm bảo bạn chỉ trả về một JSON object AIGeneratedExamDTO duy nhất chứa đúng {quantity} prompt bên trong mảng Prompts. Mỗi prompt phải có đúng 2 question. Không thêm bất kỳ nội dung nào khác.
            """;
        }


        // --- Speaking Prompts (Updated DTOs) ---
        private static string CreateSpeakingPart1Prompt(int quantity)
        {
            // --- Cập nhật exampleDto ---
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Speaking Q1-2",
                Skill = "Speaking",
                PartLabel = "Q1-2",
                Prompts = new List<AIGeneratedPromptDTO> {
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Read a Text Aloud",
                        Description = "Welcome to the City Museum's new exhibit on ancient Egypt. We are open daily from 9 AM to 5 PM. Tickets can be purchased online or at the entrance. Please note that photography without flash is permitted in most galleries. Enjoy your visit!", // Đoạn văn đọc
                        // Thêm một QuestionDTO để lưu metadata
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 8, // Part ID cho Q1-2
                                QuestionType = "ReadAloud",
                                StemText = "Read the text aloud clearly and naturally.", // Hướng dẫn chung
                                ScoreWeight = 3, // Điểm ví dụ (thang 0-3)
                                Time = 90 // Thời gian ví dụ (45s chuẩn bị + 45s đọc)
                                // Options, CorrectAnswer, Explanation, Translation là null/empty
                            }
                        }
                    }
                }
            };
            // --- Kết thúc cập nhật exampleDto ---
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            // --- Cập nhật yêu cầu prompt ---
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
            - Điền các thông tin khác (`ExamExamTitle`, `Skill`, `PartLabel`...) như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 đoạn văn):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} đoạn văn.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            """;
            // --- Kết thúc cập nhật yêu cầu prompt ---
        }

        private static string CreateSpeakingPart2Prompt(int quantity)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = "AI Generated Speaking Q3",
                Skill = "Speaking",
                PartLabel = "Q3",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 đề bài mô tả tranh
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Describe a Picture",
                        Description = "Look at the picture and describe it in as much detail as possible.", // Mô tả chung
                        ReferenceImageUrl = "Detailed description of the image", // Sẽ điền sau
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 9, QuestionType = "DescribeImage",
                                // StemText chứa mô tả ảnh để AI Image dùng
                                StemText = "A group of people are sitting around a conference table...",
                                ScoreWeight = 3, Time = 45 // Điểm và thời gian ví dụ
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
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

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
                Skill = "Speaking",
                PartLabel = "Q4-6",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 tình huống + 3 câu hỏi
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Respond to Questions",
                        // Tình huống nằm trong Description
                        Description = "Imagine that a market research company is conducting a survey about people's reading habits. You have agreed to participate in a telephone interview.",
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO { PartId = 10, QuestionType = "RespondToQuestion_Scenario", StemText = "How often do you read books?", ScoreWeight = 3, Time = 15 },
                            new AIGeneratedQuestionDTO { PartId = 10, QuestionType = "RespondToQuestion_Scenario", StemText = "What kind of books do you enjoy reading the most?", ScoreWeight = 3, Time = 15 },
                            new AIGeneratedQuestionDTO { PartId = 10, QuestionType = "RespondToQuestion_Scenario", StemText = "Describe your favorite place to read.", ScoreWeight = 3, Time = 30 }
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
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

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
                Skill = "Speaking",
                PartLabel = "Q7-9",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 thông tin + 3 câu hỏi
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Respond using Information",
                        // Thông tin nằm trong Description
                        Description = "**Conference Schedule**\n9:00 AM: Opening Remarks\n10:00 AM: Workshop A - Marketing Strategies\n11:00 AM: Coffee Break\n11:30 AM: Workshop B - Financial Planning\n1:00 PM: Lunch",
                        Questions = new List<AIGeneratedQuestionDTO> {
                             new AIGeneratedQuestionDTO { PartId = 11, QuestionType = "RespondToQuestion_Info", StemText = "What time does the conference begin?", ScoreWeight = 3, Time = 15 },
                             new AIGeneratedQuestionDTO { PartId = 11, QuestionType = "RespondToQuestion_Info", StemText = "Could you tell me what Workshop A is about?", ScoreWeight = 3, Time = 15 },
                             new AIGeneratedQuestionDTO { PartId = 11, QuestionType = "RespondToQuestion_Info", StemText = "How long is the coffee break?", ScoreWeight = 3, Time = 30 }
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
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 bộ đề):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} bộ đề.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            
            """;
        }

/*        private static string CreateSpeakingQ10Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Speaking Q10 - {topic}",
                Skill = "Speaking",
                PartLabel = "Q10",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 vấn đề + 1 yêu cầu
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Propose a Solution",
                         // Kịch bản vấn đề nằm trong Description
                        Description = "Hi, this is Mark from accounting. I'm calling because I noticed a discrepancy in the latest sales report you submitted. The total revenue figure seems much lower than expected for the last quarter. Could you please look into this and get back to me as soon as possible? We need to finalize the quarterly review by tomorrow.",
                        ReferenceAudioUrl = null, // Sẽ chứa audio của Description
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 12, QuestionType = "ProposeSolution",
                                // Hướng dẫn trả lời nằm trong StemText
                                StemText = "Listen to the message. Then, respond as if you are the recipient. In your response, you should:\n* Acknowledge you received the message and understand the problem.\n* Propose two steps you will take to investigate the discrepancy.\n* State when you will provide an update.",
                                ScoreWeight = 5, Time = 60 // Điểm và thời gian ví dụ
                            }
                        }
                    }
                }
            };
            string jsonExample = JsonConvert.SerializeObject(exampleDto, Formatting.Indented);
            return $"""
            Bạn là một chuyên gia ra đề thi TOEIC Speaking Question 10 (Propose a solution).

             **Yêu cầu:**
            - Tạo ra chính xác **{quantity}** tình huống vấn đề Q10.
            - Chủ đề chung: **{topic}**.
            - Mỗi tình huống (prompt) phải bao gồm:
                - Một kịch bản mô tả vấn đề (đặt trong `Description` của `AIGeneratedPromptDTO`), thường là dạng tin nhắn thoại.
                - Một câu hỏi/hướng dẫn (`StemText` của `AIGeneratedQuestionDTO`) yêu cầu thí sinh phản hồi và đề xuất giải pháp theo các điểm cụ thể.
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

            **Ví dụ cấu trúc JSON đầu ra (cho 1 tình huống):**
            ```json
            {jsonExample}
            ```
            **Hãy bắt đầu tạo {quantity} tình huống.**
            Hãy chỉ trả về một JSON object duy nhất, không có lời dẫn, không có markdown, không có ký hiệu ```json, không có mô tả hoặc lời giải thích nào khác.
            
            """;
        }*/

        private static string CreateSpeakingPart5Prompt(int quantity, string topic)
        {
            var exampleDto = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated Speaking Q11 - {topic}",
                Skill = "Speaking",
                PartLabel = "Q11",
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 câu hỏi quan điểm
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Express an Opinion",
                        Description = "Listen to the question. Then give your opinion and support it with reasons and examples.", // Mô tả chung
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 13, QuestionType = "ExpressOpinion",
                                StemText = "Some people prefer to work for a large company, while others prefer a small company. Which do you prefer and why? Include specific reasons and examples to support your opinion.", // Câu hỏi quan điểm
                                ScoreWeight = 5, Time = 60 // Điểm và thời gian ví dụ
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
                - Điền các thông tin khác như `ExamExamTitle`, `Skill`, `PartLabel`, `PartId`, `QuestionType`, `Time`... như trong ví dụ.
            - **Quan trọng:** Trả về kết quả dưới dạng một đối tượng JSON **AIGeneratedExamDTO** duy nhất, không có markdown hay giải thích bên ngoài, theo đúng cấu trúc ví dụ dưới đây.

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
                        QuestionType = "SentenceFromImageAndWords",
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
               - Viết mô tả chi tiết bằng tiếng Anh cho bức ảnh.  
               - Ví dụ: `"A man is reading a newspaper at a café table."`  
               - Mô tả này dùng để **tạo hình ảnh minh họa bằng AI**.

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
                Prompts = new List<AIGeneratedPromptDTO> { // Mỗi prompt chứa 1 email yêu cầu + hướng dẫn
                    new AIGeneratedPromptDTO {
                        ExamTitle = "Respond to a written request",
                        // Email yêu cầu nằm trong Description
                        Description = "From: Dale City Library\nTo: Library Members\nSubject: Upcoming Author Event...",
                        Questions = new List<AIGeneratedQuestionDTO> {
                            new AIGeneratedQuestionDTO {
                                PartId = 20, QuestionType = "RespondToEmail",
                                // Hướng dẫn viết nằm trong StemText
                                StemText = "Read the email. Respond to the Library Staff as a library member. In your email, ask TWO questions and make ONE suggestion.",
                                // Email mẫu nằm trong Explanation
                                Explanation = "[Sample response email including 2 questions and 1 suggestion...]",
                                ScoreWeight = 4, Time = 600 // 10 phút
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
                - Một email yêu cầu (đặt trong `Description` của `AIGeneratedPromptDTO`).
                - Một câu hỏi/hướng dẫn (`StemText` của `AIGeneratedQuestionDTO`) yêu cầu viết email trả lời theo vai trò và yêu cầu cụ thể (hỏi thêm, đề xuất...).
                - Cung cấp một email trả lời mẫu (`Explanation`).
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
                        QuestionType = "OpinionEssay",
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


