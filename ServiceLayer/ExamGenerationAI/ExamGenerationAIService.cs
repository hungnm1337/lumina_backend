using DataLayer.DTOs;
using DataLayer.DTOs.AIGeneratedExam;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceLayer.AI.Prompt;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

namespace ServiceLayer.ExamGenerationAI
{
    public class ExamGenerationAIService : IExamGenerationAIService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _openAIOptions;

        public ExamGenerationAIService(
            IOptions<OpenAIOptions> openAIOptions,
            HttpClient httpClient)
        {
            _openAIOptions = openAIOptions.Value;
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _openAIOptions.ApiKey);
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
            
            var requestBody = new
            {
                model = _openAIOptions.Model,
                messages = new[]
                {
                    new { role = "system", content = "You are a TOEIC exam generation assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/chat/completions", 
                    content, 
                    cts.Token
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                string messageContent = jsonResponse.choices[0].message.content.ToString();
                return messageContent;
            }
            catch (OperationCanceledException)
            {
                throw new Exception(" Request timeout sau 600 giây. Hãy thử lại hoặc giảm quantity.");
            }
        }

        public async Task<(int partNumber, int quantity, string? topic)> ParseUserRequestAsync(string userRequest)
        {
            var parsePrompt = PromptFactory.CreateParsingPrompt(userRequest);
            var responseStr = await GenerateResponseAsync(parsePrompt);

            Console.WriteLine("✅ [OpenAI] Raw Response for ParsePrompt: " + responseStr);

            var textJson = CleanAIResponse(responseStr);
            var parseResult = JsonConvert.DeserializeObject<ParseResultDTO>(textJson);

            Console.WriteLine($"📋 Parsed Result: Part={parseResult.PartNumber}, Quantity={parseResult.Quantity}, Topic={parseResult.Topic}");

            return (parseResult.PartNumber, parseResult.Quantity, parseResult.Topic);
        }

        // Helper method to check if user explicitly specified Part number
        private bool HasExplicitPartNumber(string userRequest)
        {
            var partPattern = @"part\s*\d+";
            return System.Text.RegularExpressions.Regex.IsMatch(
                userRequest, 
                partPattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        // Extract the Part number that user wrote in the request (not internal partNumber)
        private int? ExtractUserPartNumber(string userRequest)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                userRequest, 
                @"part\s*(\d+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (match.Success && int.TryParse(match.Groups[1].Value, out int partNum))
            {
                return partNum;
            }
            
            return null;
        }

        // Validate Part request
        public (bool isValid, string? errorMessage) ValidatePartRequest(int partNumber, string userRequest)
        {
            // Kiểm tra số âm trong user request (trước khi AI parse)
            if (System.Text.RegularExpressions.Regex.IsMatch(userRequest, @"part\s*-\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return (false, "Part number không hợp lệ.\n\nVui lòng nhập số Part hợp lệ!");
            }

            // Kiểm tra user có chỉ định rõ Part number không
            var lowerRequest = userRequest.ToLower();
            bool isSkillOnly = (lowerRequest.Contains("listening") || 
                                lowerRequest.Contains("reading") || 
                                lowerRequest.Contains("speaking") || 
                                lowerRequest.Contains("writing") ||
                                lowerRequest.Contains("nghe") ||
                                lowerRequest.Contains("đọc") ||
                                lowerRequest.Contains("nói") ||
                                lowerRequest.Contains("viết"));
            
            if (isSkillOnly && !HasExplicitPartNumber(userRequest))
            {
                return (false, @"Vui lòng chỉ rõ Part number bạn muốn tạo!
Ví dụ đúng:
• Tạo 5 câu Listening Part 1
• Tạo 10 câu Reading Part 5
• Gen đề Speaking Part 3
Không đủ thông tin:
• Tạo câu listening (thiếu Part number)
• Cho tôi đề reading (thiếu Part number)
Hãy cho tôi biết Part cụ thể nhé!");
            }

            // Kiểm tra Part có tồn tại không
            if (partNumber < 1 || partNumber > 15)
            {
                return (false, GetInvalidPartMessage(partNumber));
            }

            // Kiểm tra xem user có yêu cầu nhiều Part cùng lúc không
            if (IsMultiplePartsRequest(userRequest))
            {
                return (false, @"Xin lỗi, tôi chỉ có thể tạo câu hỏi cho từng Part một.

Ví dụ:
• Đúng: Tạo 10 câu Listening Part 1
• Đúng: Tạo 5 câu Reading Part 7
• Sai: Tạo đề Listening Part 1, 2, 3

Hãy chọn một Part để tạo đề nhé!");
            }

            // Kiểm tra Skill và Part có khớp không (dựa trên số Part mà user GHI)
            var userPartNum = ExtractUserPartNumber(userRequest);
            
            if (userPartNum.HasValue)
            {
                // Nếu có từ "listening" thì Part phải từ 1-4
                if ((lowerRequest.Contains("listening") || lowerRequest.Contains("nghe")))
                {
                    if (userPartNum.Value < 1 || userPartNum.Value > 4)
                    {
                        return (false, GetListeningPartErrorMessage());
                    }
                }

                // Nếu có từ "reading" thì Part phải từ 5-7
                if ((lowerRequest.Contains("reading") || lowerRequest.Contains("đọc")))
                {
                    if (userPartNum.Value < 5 || userPartNum.Value > 7)
                    {
                        return (false, GetReadingPartErrorMessage());
                    }
                }

                // Nếu có từ "speaking" thì Part phải từ 1-5 (user perspective)
                if ((lowerRequest.Contains("speaking") || lowerRequest.Contains("nói")))
                {
                    if (userPartNum.Value < 1 || userPartNum.Value > 5)
                    {
                        return (false, GetSpeakingPartErrorMessage());
                    }
                }

                // Nếu có từ "writing" thì Part phải từ 1-3 (user perspective)
                if ((lowerRequest.Contains("writing") || lowerRequest.Contains("viết")))
                {
                    if (userPartNum.Value < 1 || userPartNum.Value > 3)
                    {
                        return (false, GetWritingPartErrorMessage());
                    }
                }
            }

            return (true, null);
        }

        private bool IsMultiplePartsRequest(string userRequest)
        {
            var lowerRequest = userRequest.ToLower();
            
            // Kiểm tra pattern: "part 1, 2", "part 1 và 2", "part 1, part 2", etc.
            var multiPartPatterns = new[]
            {
                @"part\s+\d+\s*,\s*\d+",           // "part 1, 2"
                @"part\s+\d+\s*,\s*part\s+\d+",     // "part 1, part 2"
                @"part\s+\d+\s+và\s+\d+",           // "part 1 và 2"
                @"part\s+\d+\s+and\s+\d+",          // "part 1 and 2"
                @"part\s+\d+\s*,\s*\d+\s*,\s*\d+"  // "part 1, 2, 3"
            };

            foreach (var pattern in multiPartPatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(lowerRequest, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetInvalidPartMessage(int partNumber)
        {
            return $@"Xin lỗi, không tồn tại Part {partNumber} trong TOEIC.

{GetTOEICStructure()}";
        }

        private string GetListeningPartErrorMessage()
        {
            return @"Xin lỗi, TOEIC Listening chỉ có 4 Part (Part 1-4).
**LISTENING (Part 1-4):**
• Part 1: Mô tả tranh (6 câu)
• Part 2: Hỏi đáp (25 câu)
• Part 3: Hội thoại ngắn (39 câu)
• Part 4: Độc thoại (30 câu)

Bạn muốn tạo Part nào?";
        }

        private string GetReadingPartErrorMessage()
        {
            return @"Xin lỗi, TOEIC Reading chỉ có 3 Part (Part 5-7).
**READING (Part 5-7):**
• Part 5: Hoàn thành câu (30 câu)
• Part 6: Hoàn thành đoạn văn (16 câu)
• Part 7: Đọc hiểu (15 câu)

Bạn muốn tạo Part nào?";
        }

        private string GetSpeakingPartErrorMessage()
        {
            return @"Xin lỗi, TOEIC Speaking có 5 task (đánh số Part 1-5).
**SPEAKING (Part 1-5):**
• Part 1: Đọc văn bản (2 câu)
• Part 2: Mô tả ảnh (2 câu)
• Part 3: Trả lời câu hỏi (3 câu)
• Part 4: Trả lời với thông tin (3 câu)
• Part 5: Biểu đạt ý kiến (1 câu)

Bạn muốn tạo Part nào?";
        }

        private string GetWritingPartErrorMessage()
        {
            return @"Xin lỗi, TOEIC Writing có 3 task (đánh số Part 1-3).
**WRITING (Part 1-3):**
• Part 1: Viết câu dựa vào ảnh (5 câu)
• Part 2: Trả lời email (2 câu)
• Part 3: Viết bài luận (1 câu)

Bạn muốn tạo Part nào?";
        }

        private string GetTOEICStructure()
        {
            return @"**CẤU TRÚC ĐỀ THI TOEIC:**
**LISTENING:**
• Part 1: Mô tả tranh (6 câu)
• Part 2: Hỏi đáp (25 câu)
• Part 3: Hội thoại (15 câu)
• Part 4: Độc thoại (15 câu)
**READING:**
• Part 5: Hoàn thành câu (30 câu)
• Part 6: Hoàn thành đoạn văn (16 câu)
• Part 7: Đọc hiểu (15 câu)
**SPEAKING:**
• Part 1: Đọc văn bản (2 câu)
• Part 2: Mô tả ảnh (2 câu)
• Part 3: Trả lời câu hỏi (3 câu)
• Part 4: Trả lời với thông tin (3 câu)
• Part 5: Biểu đạt ý kiến (1 câu)
**WRITING:**
• Part 1: Viết câu dựa vào ảnh (5 câu)
• Part 2: Trả lời email (2 câu)
• Part 3: Viết bài luận (1 câu)
Bạn muốn tạo Part nào?";
        }

        public async Task<AIGeneratedExamDTO> GenerateExamAsync(
            int partNumber, 
            int quantity, 
            string? topic)
        {
            const int maxBatchSize = 10; 
            
            if (quantity > maxBatchSize)
            {
                return await GenerateExamInBatchesAsync(partNumber, quantity, topic, maxBatchSize);
            }
            else
            {
                return await GenerateSingleBatchAsync(partNumber, quantity, topic);
            }
        }

        public async Task<IntentResult> DetectIntentAsync(string userRequest)
        {
            var prompt = $@"
Phân loại yêu cầu của user sau đây:
- Nếu user muốn TẠO ĐỀ THI/CÂU HỎI TOEIC (có từ khóa: 'tạo', 'generate', 'câu hỏi', 'đề thi', 'Reading Part', 'Listening Part', 'số lượng câu') → IsExamRequest = true
- Nếu user HỎI THÔNG TIN/GIẢI THÍCH/TRÒ CHUYỆN → IsExamRequest = false

User request: ""{userRequest}""

Trả về JSON:
{{
  ""isExamRequest"": true/false,
  ""explanation"": ""lý do ngắn gọn""
}}";

            var responseStr = await GenerateResponseAsync(prompt);
            var textJson = CleanAIResponse(responseStr);

            return JsonConvert.DeserializeObject<IntentResult>(textJson);
        }

        private bool IsOutOfScopeQuestion(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Nếu câu hỏi rõ ràng về TỪ Vựng tiếng Anh → CHẤP NHẬN
            var vocabularyIndicators = new[] 
            { 
                "tiếng anh là gì", "in english", "english word", "từ tiếng anh",
                "dịch sang tiếng anh", "translate to english", "nghĩa là gì", "what does",
                "từ vựng", "vocabulary", "từ này", "word", "nghia cua"
            };
            
            if (vocabularyIndicators.Any(indicator => lowerMessage.Contains(indicator)))
            {
                return false; // Đây là câu hỏi từ vựng hợp lệ
            }
            
            // Danh sách từ khóa ngoài phạm vi TOEIC (loại bỏ từ vựng cơ bản)
            var outOfScopeKeywords = new[]
            {
                // Lập trình
                "lập trình", "programming", "code javascript", "code python", "code java", "html css", "react", "angular", "nodejs", "typescript", "php", "debug", "compiler",
                
                // Y tế
                "y tế", "medical", "bác sĩ", "thuốc chữa", "bệnh viện", "khám bệnh", "chữa bệnh", "phẫu thuật", "chẩn đoán", "bệ nhân",
                
                // Pháp luật
                "pháp luật", "legal", "luật sư", "tòa án", "kiện tụng", "hợp đồng pháp lý", "vi phạm pháp luật", "bản án",
                
                // Chính trị & Thời sự
                "chính trị", "politics", "bầu cử", "chính phủ", "đảng phái", "tổng thống", "thủ tướng",
                "thời sự hôm nay", "tin tức mới nhất", "sự kiện hiện nay", "báo chí",
                
                // Công nghệ (không phải English for IT)
                "cài đặt phần mềm", "sửa máy tính", "hướng dẫn cài", "database design", "server setup", "cloud deployment",
                
                // Ẩm thực (không phải từ vựng food)
                "công thức nấu ăn", "recipe for", "cách nấu", "how to cook", "bí quyết nấu",
                
                // Thể thao (không phải từ vựng sports)
                "kết quả trận đấu", "lịch thi đấu", "world cup 20", "giải bóng đá",
                
                // Giải trí
                "phim mới", "netflix", "spotify", "xem phim ở đâu", "ca sĩ nào",
                
                // Tài chính
                "đầu tư cổ phiếu", "mua bitcoin", "cryptocurrency", "forex trading", "chứng khoán",
                
                // Khoa học (không phải từ vựng khoa học)
                "giải toán", "solve math", "công thức vật lý", "phương trình hóa học", "thí nghiệm",
                
                // Người nổi tiếng (câu hỏi về người cụ thể)
                "sơn tùng", "jack 97", "k-icm", "bts army", "blackpink", "cristiano ronaldo", "messi", "donald trump",
                "elon musk", "bill gates", "mark zuckerberg", "steve jobs", "taylor swift concert",
                
                // Các chủ đề khác
                "chơi game", "esports", "streamer", "youtuber nào", "tiktoker",
                "mua sắm online", "shop thời trang", "skincare routine",
                "mua xe hơi", "honda exciter", "toyota camry",
                "nuôi chó mèo", "pet care", "chăm sóc thú cưng",
                "hẹn hò thế nào", "dating tips", "cách tán gái"
            };
            
            // Kiểm tra các câu hỏi về người cụ thể (pattern: "bạn có biết [tên người]")
            if (lowerMessage.Contains("bạn có biết") || lowerMessage.Contains("ban có biết") ||
                lowerMessage.Contains("có biết không") || lowerMessage.Contains("ai là") ||
                lowerMessage.Contains("who is") || lowerMessage.Contains("do you know"))
            {
                // Nếu không hỏi về từ vựng, ngữ pháp, hoặc TOEIC thì là ngoài phạm vi
                if (!lowerMessage.Contains("từ vựng") && !lowerMessage.Contains("vocabulary") &&
                    !lowerMessage.Contains("ngữ pháp") && !lowerMessage.Contains("grammar") &&
                    !lowerMessage.Contains("toeic") && !lowerMessage.Contains("tiếng anh") && 
                    !lowerMessage.Contains("english"))
                {
                    return true;
                }
            }
            
            return outOfScopeKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        public async Task<string> GeneralChatAsync(string userRequest)
        {
            // Kiểm tra câu hỏi ngoài phạm vi TOEIC
            if (IsOutOfScopeQuestion(userRequest))
            {
                return @"Xin lỗi, tôi chỉ có thể hỗ trợ bạn về các chủ đề liên quan đến TOEIC và học tiếng Anh.

Tôi có thể giúp bạn với:
• Tạo đề thi và câu hỏi TOEIC (Reading, Listening, Speaking, Writing)
• Từ vựng TOEIC và cách sử dụng
• Ngữ pháp tiếng Anh
• Chiến lược làm bài các Part trong TOEIC
• Luyện tập và bài tập thực hành
• Lộ trình học và động viên học tập

Bạn có câu hỏi nào về TOEIC hoặc tiếng Anh mà tôi có thể giúp không?";
            }

            var chatPrompt = $@"
You are a friendly TOEIC expert assistant. Answer naturally in PLAIN TEXT.

**IMPORTANT SCOPE:**
You ONLY answer questions related to:
- TOEIC exam (all parts: Listening, Reading, Speaking, Writing)
- English vocabulary and grammar for TOEIC
- TOEIC test strategies and tips
- English learning methods

If asked about topics outside TOEIC/English learning, politely decline.

User question: ""{userRequest}""

 CRITICAL OUTPUT RULES:
1. Respond ONLY in PLAIN TEXT format (absolutely NO JSON!)
2. Use simple formatting for readability:
   - **text** for emphasis/bold
   - Numbers: 1. 2. 3. for lists
   - Bullet points: • for items
   - Blank lines between paragraphs
3. Write in Vietnamese (unless user asks in English)
4. Be warm, helpful and conversational
5. Stay within TOEIC/English learning topics only

Example responses:

---
**For general questions:**
Chào bạn! 

**TOEIC Reading Part 5** là phần Incomplete Sentences (Hoàn thành câu).

**Đặc điểm:**
• 30 câu hỏi trắc nghiệm
• Mỗi câu có 1 chỗ trống
• 4 lựa chọn A/B/C/D

**Nội dung kiểm tra:**
• Ngữ pháp: thì, câu điều kiện, bị động
• Từ vựng: giới từ, liên từ, phrasal verbs
• Từ loại: động từ, danh từ, tính từ

**Mẹo làm bài:**
1. Đọc toàn bộ câu trước khi chọn
2. Xác định từ loại cần điền
3. Loại trừ các đáp án sai
4. Thời gian: 30 câu trong 15 phút

Chúc bạn ôn tập tốt! 

---
**For vocabulary requests:**
Chào bạn! 

**10 từ vựng về thiên nhiên:**

1. **Forest** (rừng)
   The Amazon forest is disappearing rapidly.

2. **River** (sông)
   We went kayaking on the river yesterday.

3. **Mountain** (núi)
   Climbing the mountain was very challenging.

4. **Ocean** (đại dương)
   The ocean is home to millions of species.

5. **Lake** (hồ)
   The lake water is crystal clear.

6. **Desert** (sa mạc)
   Few plants can survive in the desert.

7. **Beach** (bãi biển)
   They walked along the beach at sunset.

8. **Valley** (thung lũng)
   The valley was covered with flowers.

9. **Waterfall** (thác nước)
   The waterfall creates a beautiful rainbow.

10. **Island** (đảo)
    The island has pristine white beaches.

 **Mẹo:** Tạo câu ví dụ của riêng bạn để nhớ từ tốt hơn!

Chúc bạn học tốt! 

---

Now answer the user's question (PLAIN TEXT, NO JSON):";

            var responseText = await GenerateResponseAsync(chatPrompt);
            
            return CleanChatResponseSimple(responseText);
        }

        private string CleanChatResponseSimple(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Xin lỗi, tôi không thể trả lời câu hỏi này. Vui lòng thử lại!";

            try
            {
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    @"```(json|markdown|html|text)?\s*([\s\S]*?)\s*```",
                    "$2",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                var trimmed = text.Trim();
                
                if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                {
                    try
                    {
                        var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(trimmed);
                        
                        // Tìm key chứa text response
                        if (jsonObj != null)
                        {
                            if (jsonObj.ContainsKey("response"))
                                return jsonObj["response"]?.ToString() ?? text;
                            
                            if (jsonObj.ContainsKey("message"))
                                return jsonObj["message"]?.ToString() ?? text;
                            
                            if (jsonObj.ContainsKey("text"))
                                return jsonObj["text"]?.ToString() ?? text;
                        }
                    }
                    catch
                    {
                        // Parse fail → tiếp tục xử lý
                    }
                }
                
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    try
                    {
                        var jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(trimmed);
                        
                        if (jsonArray != null && jsonArray.Count > 0)
                        {
                            var result = new StringBuilder("**Danh sách từ vựng:**\n\n");
                            int index = 1;
                            
                            foreach (var item in jsonArray)
                            {
                                string word = item.GetValueOrDefault("word") 
                                           ?? item.GetValueOrDefault("tu_vung") 
                                           ?? item.GetValueOrDefault("vocabulary") 
                                           ?? "";
                                
                                string example = item.GetValueOrDefault("example") 
                                                  ?? item.GetValueOrDefault("cau_ví_du") 
                                                  ?? item.GetValueOrDefault("câu_ví_dụ") 
                                                  ?? "";
                                
                                if (!string.IsNullOrEmpty(word))
                                {
                                    result.AppendLine($"{index}. **{word}**");
                                    if (!string.IsNullOrEmpty(example))
                                    {
                                        result.AppendLine($"   {example}\n");
                                    }
                                    index++;
                                }
                            }
                            
                            return result.ToString();
                        }
                    }
                    catch
                    {
                        // Parse fail → return text gốc
                    }
                }

                text = text
                    .Replace("Sure! ", "")
                    .Replace("Sure, ", "")
                    .Replace("Here is ", "")
                    .Replace("Here's ", "")
                    .Replace("Response: ", "")
                    .Replace("Output: ", "")
                    .Trim();

                return text;
            }
            catch (Exception ex)
            {
                return text; // Return nguyên nếu lỗi
            }
        }

        private string CleanAIResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"```(json|markdown|html)?\s*([\s\S]*?)\s*```",
                "$2",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            text = text
                .Replace("**", "")
                .Replace("##", "")
                .Replace("Here is the JSON output you requested:", "")
                .Replace("Here is your output:", "")
                .Replace("Response:", "")
                .Replace("Output:", "")
                .Replace("Sure!", "")
                .Trim();

            int firstBrace = text.IndexOf('{');
            int lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                text = text.Substring(firstBrace, lastBrace - firstBrace + 1);

            return text.Trim();
        }

        private async Task<AIGeneratedExamDTO> GenerateExamInBatchesAsync(
            int partNumber, 
            int totalQuantity, 
            string? topic,
            int batchSize)
        {
            var combinedExam = new AIGeneratedExamDTO
            {
                ExamExamTitle = $"AI Generated TOEIC Part {partNumber}",
                Skill = GetSkillFromPart(partNumber),
                PartLabel = $"Part {partNumber}",
                Prompts = new List<AIGeneratedPromptDTO>()
            };

            int remaining = totalQuantity;
            int currentBatch = 1;

            while (remaining > 0)
            {
                int currentQuantity = Math.Min(batchSize, remaining);
                
                Console.WriteLine($"📦 Batch {currentBatch}/{Math.Ceiling((double)totalQuantity / batchSize)}: Creating {currentQuantity} items...");

                try
                {
                    var batchExam = await GenerateSingleBatchAsync(partNumber, currentQuantity, topic);
                    
                    if (batchExam?.Prompts != null && batchExam.Prompts.Any())
                    {
                        combinedExam.Prompts.AddRange(batchExam.Prompts);
                    }
                    else
                    {
                    }
                    
                    remaining -= currentQuantity;
                    currentBatch++;
                    
                    if (remaining > 0)
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Batch processing failed at batch {currentBatch}: {ex.Message}", ex);
                }
            }

            return combinedExam;
        }

        private async Task<AIGeneratedExamDTO> GenerateSingleBatchAsync(
            int partNumber, 
            int quantity, 
            string? topic)
        {
            const int MAX_RETRIES = 3;
            int attempt = 0;

            var config = PromptFactory.GetPartConfiguration(partNumber); 
            Console.WriteLine($" Part {partNumber} config: {config.QuestionsPerPrompt} Q/Prompt, expecting {quantity} prompts");

            while (attempt < MAX_RETRIES)
            {
                try
                {
                    attempt++;

                    var genPrompt = PromptFactory.GetGenerationPrompt(partNumber, quantity, topic);
                    var textJson = await GenerateResponseAsync(genPrompt);


                    textJson = CleanAIResponse(textJson);
                    var examDto = JsonConvert.DeserializeObject<AIGeneratedExamDTO>(textJson);

                    if (examDto?.Prompts == null)
                    {
                        throw new InvalidOperationException("AI returned invalid exam structure");
                    }

                    int receivedPrompts = examDto.Prompts.Count;
                    bool isValid = (receivedPrompts == quantity);
                    
                    Console.WriteLine($"📊 Part {partNumber}: Received {receivedPrompts}/{quantity} prompts");

                    if (config.QuestionsPerPrompt > 1)
                    {
                        foreach (var prompt in examDto.Prompts)
                        {
                            int questionsCount = prompt.Questions?.Count ?? 0;
                            if (questionsCount != config.QuestionsPerPrompt)
                            {
                                Console.WriteLine($" Warning: Prompt has {questionsCount} questions, expected {config.QuestionsPerPrompt}");
                            }
                        }
                    }

                    if (!isValid)
                    {
                        Console.WriteLine($" Warning: Expected {quantity} prompts but got {receivedPrompts}");
                        
                        // BỔ SUNG NẾI THIẾU ÍT
                        if (receivedPrompts >= quantity - 2 && receivedPrompts < quantity)
                        {
                            int missingCount = quantity - receivedPrompts;
                            Console.WriteLine($" Attempting to fill missing {missingCount} prompts...");
                            
                            var supplementExam = await GenerateSingleBatchAsync(partNumber, missingCount, topic);
                            
                            if (supplementExam?.Prompts != null)
                            {
                                examDto.Prompts.AddRange(supplementExam.Prompts.Take(missingCount));
                                Console.WriteLine($" Added {missingCount} supplementary prompts");
                            }
                        }
                        // RETRY NẾI SAI QUỚN
                        else if (attempt < MAX_RETRIES)
                        {
                            Console.WriteLine($" Discrepancy too large. Retrying...");
                            await Task.Delay(1000);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($" Max retries reached. Returning {receivedPrompts} prompts instead of {quantity}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($" Perfect! Received exactly {quantity} prompts");
                    }

                    // Generate image URLs
                    foreach (var prompt in examDto.Prompts.Where(p => !string.IsNullOrEmpty(p.ReferenceImageUrl)))
                    {
                        if (!prompt.ReferenceImageUrl!.StartsWith("http"))
                        {
                            prompt.ReferenceImageUrl = GeneratePollinationsImageUrl(prompt.ReferenceImageUrl);
                        }
                    }

                    return examDto;
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($" JSON Parse Error (Attempt {attempt}): {ex.Message}");
                    
                    if (attempt >= MAX_RETRIES)
                    {
                        throw new InvalidOperationException("Failed to parse AI response as valid JSON after retries", ex);
                    }
                    
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" GenerateSingleBatchAsync Error (Attempt {attempt}): {ex.Message}");
                    
                    if (attempt >= MAX_RETRIES)
                    {
                        throw;
                    }
                    
                    await Task.Delay(1000);
                }
            }

            throw new InvalidOperationException($"Failed to generate {quantity} prompts after {MAX_RETRIES} attempts");
        }
        public string GeneratePollinationsImageUrl(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string encodedPrompt = Uri.EscapeDataString(description.Trim());

            string imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=512&height=512&seed=random&nologo=true&enhance=true&safe=true";

            return imageUrl;
        }

        /* public string GeneratePollinationsImageUrl(string description)
         {
             if (string.IsNullOrWhiteSpace(description))
                 return string.Empty;

             string enhancePrompt =
                 "ultra sharp, high clarity, detailed texture, crisp edges, photorealistic, high-detail lighting, noise-free";

             string finalPrompt = $"{description}, {enhancePrompt}";
             string encodedPrompt = Uri.EscapeDataString(finalPrompt);

             return $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=512&height=512&seed=random&nologo=true&enhance=true&safe=true";
         }*/



        private static string GetSkillFromPart(int partNumber)
        {
            return partNumber switch
            {
                >= 1 and <= 4 => "Listening",
                >= 5 and <= 7 => "Reading",
                >= 8 and <= 12 => "Speaking",
                >= 13 and <= 15 => "Writing",
                _ => "Unknown"
            };
        }
    }
}
