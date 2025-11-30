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

            return (parseResult.PartNumber, parseResult.Quantity, parseResult.Topic);
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

        public async Task<string> GeneralChatAsync(string userRequest)
        {
            var chatPrompt = $@"
You are a friendly TOEIC expert assistant. Answer naturally in PLAIN TEXT.

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
                return "Xin lỗi, tôi không thể trả lời câu hỏi này. Vui lòng thử lại! 😊";

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
