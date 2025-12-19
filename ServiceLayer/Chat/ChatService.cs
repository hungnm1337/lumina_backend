using DataLayer.DTOs.Chat;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using ServiceLayer.UploadFile;
using DataLayer.DTOs;
using System.Net.Http.Headers;

namespace ServiceLayer.Chat
{
    public class ChatService : IChatService
    {
        private readonly LuminaSystemContext _context;
        private readonly OpenAIOptions _openAIOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUploadService _uploadService;

        public ChatService(
            LuminaSystemContext context, 
            IOptions<OpenAIOptions> openAIOptions, 
            IHttpClientFactory httpClientFactory, 
            IUploadService uploadService)
        {
            _context = context;
            _openAIOptions = openAIOptions.Value;
            _httpClientFactory = httpClientFactory;
            _uploadService = uploadService;
        }

        public async Task<ChatResponseDTO> ProcessMessage(ChatRequestDTO request)
        {
            try
            {
                // Kiểm tra câu hỏi ngoài phạm vi TOEIC
                if (IsOutOfScopeQuestion(request.Message))
                {
                    return new ChatResponseDTO
                    {
                        Answer = "Xin lỗi, tôi chỉ có thể hỗ trợ bạn về TOEIC và học tiếng Anh. Bạn có câu hỏi nào về từ vựng, ngữ pháp, chiến lược làm bài TOEIC, hoặc luyện tập không?",
                        ConversationType = "out_of_scope",
                        Suggestions = new List<string> 
                        { 
                            "Từ vựng TOEIC thường gặp",
                            "Ngữ pháp Part 5",
                            "Chiến lược làm Part 7", 
                            "Luyện tập Listening",
                            "Mẹo làm bài Reading"
                        }
                    };
                }
                
                // Xác định loại câu hỏi
                var questionType = DetermineQuestionType(request.Message);
                
                switch (questionType)
                {
                    /*case "vocabulary":
                        return await HandleVocabularyQuestion(request);*/
                    case "grammar":
                        return await HandleGrammarQuestion(request);
                    case "toeic_strategy":
                        return await HandleTOEICStrategyQuestion(request);
                    case "practice":
                        return await HandlePracticeQuestion(request);
                    case "vocabulary_generation":
                        return await GenerateVocabularyResponse(request);
                    default:
                        return await HandleGeneralQuestion(request);
                }
            }
            catch (Exception ex)
            {
                return new ChatResponseDTO
                {
                    Answer = $"Xin lỗi, tôi gặp lỗi khi xử lý câu hỏi của bạn: {ex.Message}",
                    ConversationType = "error",
                    Suggestions = new List<string> { "Hãy thử hỏi lại", "Liên hệ hỗ trợ" }
                };
            }
        }

        private bool IsOutOfScopeQuestion(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Danh sách từ khóa ngoài phạm vi TOEIC
            var outOfScopeKeywords = new[]
            {
                "lập trình", "programming", "code", "javascript", "python", "java", "c#", "html", "css", "react", "angular",
                "y tế", "medical", "bác sĩ", "thuốc", "bệnh", "sức khỏe", "bệnh viện", "khám bệnh",
                "pháp luật", "legal", "luật sư", "tòa án", "luật", "kiện tụng", "hợp đồng",
                "chính trị", "politics", "bầu cử", "chính phủ", "đảng phái", "tổng thống", "thủ tướng",
                "thời sự", "news", "tin tức", "sự kiện", "báo chí", "phóng viên", "truyền hình",
                "công nghệ", "technology", "máy tính", "phần mềm", "app", "website", "database",
                "nấu ăn", "cooking", "nấu", "món ăn", "thực phẩm", "nhà hàng", "đầu bếp",
                "du lịch", "travel", "đi du lịch", "khách sạn", "vé máy bay", "tour", "nghỉ dưỡng",
                "thể thao", "sport", "bóng đá", "bóng rổ", "tennis", "cầu lông", "bơi lội",
                "giải trí", "entertainment", "phim", "nhạc", "game", "game show", "ca sĩ", "diễn viên",
                "kinh tế", "tài chính", "ngân hàng", "đầu tư", "cổ phiếu", "chứng khoán", "bitcoin",
                "toán học", "math", "vật lý", "physics", "hóa học", "chemistry", "sinh học", "biology",
                "lịch sử", "history", "địa lý", "geography", "văn học", "literature", "triết học",
                "nghệ thuật", "art", "hội họa", "điêu khắc", "kiến trúc", "thiết kế",
                "tâm lý học", "psychology", "xã hội học", "sociology", "nhân chủng học"
            };
            
            return outOfScopeKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        private string GetSystemPrompt()
        {
            return @"You are Lumina AI Tutor, a specialized TOEIC English learning assistant. 

**YOUR EXPERTISE AREAS:**
- TOEIC vocabulary and word usage
- English grammar for TOEIC test  
- TOEIC test strategies and tips
- Practice exercises and study plans
- English learning motivation and guidance

**LANGUAGE SUPPORT:**
- Accept questions in BOTH Vietnamese and English
- Users can ask in either language or mix both languages
- Always respond in Vietnamese with English examples when relevant

**IMPORTANT RULES:**
1. ONLY answer questions related to TOEIC English learning
2. If asked about topics outside TOEIC/English learning, politely redirect:
   'Xin lỗi, tôi chỉ có thể hỗ trợ bạn về TOEIC và học tiếng Anh. Bạn có câu hỏi nào về từ vựng, ngữ pháp, chiến lược làm bài TOEIC, hoặc luyện tập không?'

3. Be encouraging and educational
4. Provide TOEIC-specific context when applicable

**OUT OF SCOPE TOPICS:**
- General knowledge outside English learning
- Technical programming questions
- Personal advice unrelated to English learning
- Current events or politics
- Medical or legal advice
- Any topic not related to TOEIC English learning

**RESPONSE FORMAT:**
Always respond with a valid JSON object in the specified format for each question type.";
        }

        private string DetermineQuestionType(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Vocabulary generation - Improved logic for bilingual support
            // Support both Vietnamese and English
            bool hasCreateAction = lowerMessage.Contains("tạo") || 
                                  lowerMessage.Contains("create") || 
                                  lowerMessage.Contains("generate") ||
                                  lowerMessage.Contains("make");
            
            bool hasVocabKeyword = lowerMessage.Contains("từ vựng") || 
                                  lowerMessage.Contains("từ") ||
                                  lowerMessage.Contains("vocabulary") || 
                                  lowerMessage.Contains("vocabularies") ||
                                  lowerMessage.Contains("word") ||
                                  lowerMessage.Contains("words");
            
            if (hasCreateAction && hasVocabKeyword)
                return "vocabulary_generation";
                
            // Vocabulary questions
           /* if (lowerMessage.Contains("từ vựng") || lowerMessage.Contains("vocabulary") || 
                lowerMessage.Contains("nghĩa") || lowerMessage.Contains("từ"))
                return "vocabulary";
                */
            // Grammar questions
            if (lowerMessage.Contains("ngữ pháp") || lowerMessage.Contains("grammar") ||
                lowerMessage.Contains("thì") || lowerMessage.Contains("tense"))
                return "grammar";
                
            // TOEIC strategy questions
            if (lowerMessage.Contains("part") || lowerMessage.Contains("mẹo") ||
                lowerMessage.Contains("chiến lược") || lowerMessage.Contains("strategy"))
                return "toeic_strategy";
                
            // Practice questions
            if (lowerMessage.Contains("luyện tập") || lowerMessage.Contains("practice") ||
                lowerMessage.Contains("bài tập") || lowerMessage.Contains("exercise"))
                return "practice";
                
            return "general";
        }

        private async Task<ChatResponseDTO> HandleVocabularyQuestion(ChatRequestDTO request)
        {
            // Lấy từ vựng của user làm context
            var userVocabularies = await GetUserVocabularies(request.UserId);
            
            var prompt = $@"You are a TOEIC vocabulary expert. Answer the user's question about vocabulary.

**User's Question:** {request.Message}
**User's Current Vocabulary:** {string.Join(", ", userVocabularies.Select(v => v.Word))}

**Instructions:**
1. Answer in Vietnamese with English examples
2. Provide TOEIC-specific context
3. Suggest related words from user's vocabulary
4. Give memory tips if applicable

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Your detailed answer in Vietnamese here"",
    ""suggestions"": [""Related question 1"", ""Related question 2""],
    ""relatedWords"": [""word1"", ""word2""],
    ""conversationType"": ""vocabulary""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandleGrammarQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are a TOEIC grammar expert. Answer the user's grammar question.

**User's Question:** {request.Message}

**Instructions:**
1. Explain grammar rules clearly in Vietnamese
2. Provide English examples
3. Give TOEIC-specific tips
4. Include common mistakes to avoid

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Detailed grammar explanation here"",
    ""suggestions"": [""Practice question 1"", ""Practice question 2""],
    ""examples"": [""Example 1"", ""Example 2""],
    ""conversationType"": ""grammar""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandleTOEICStrategyQuestion(ChatRequestDTO request)
        {
            // Lấy điểm số gần nhất của user
            var recentScores = await GetUserRecentScores(request.UserId);
            
            var prompt = $@"You are a TOEIC test strategy expert. Help the user with TOEIC strategies.

**User's Question:** {request.Message}
**User's Recent Scores:** {string.Join(", ", recentScores.Select(s => $"{s.Exam.Name}: {s.Score}"))}

**Instructions:**
1. Provide specific strategies for TOEIC parts
2. Give time management tips
3. Suggest practice methods
4. Be encouraging and practical

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Detailed strategy explanation here"",
    ""suggestions"": [""Practice tip 1"", ""Practice tip 2""],
    ""conversationType"": ""toeic_strategy""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandlePracticeQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are a TOEIC practice expert. Help the user with practice and exercises.

**User's Question:** {request.Message}

**Instructions:**
1. Provide practice suggestions
2. Give exercise recommendations
3. Suggest study schedules
4. Be motivational and practical

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Practice recommendations here"",
    ""suggestions"": [""Practice method 1"", ""Practice method 2""],
    ""conversationType"": ""practice""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> GenerateVocabularyResponse(ChatRequestDTO request)
        {
            // Parse số lượng từ vựng từ user request
            int vocabularyCount = ExtractVocabularyCount(request.Message);
            
            // Giới hạn tối đa 30 từ
            if (vocabularyCount > 30) vocabularyCount = 30;
            if (vocabularyCount < 1) vocabularyCount = 10; // Default 10 nếu không parse được
            
            var prompt = $@"You are a TOEIC vocabulary expert. Generate vocabulary words based on user's request.

**User's Request:** {request.Message}

**Instructions:**
Generate EXACTLY {vocabularyCount} vocabulary words related to the requested topic. Each word should be:
1. Commonly used in TOEIC exams
2. Include definition in Vietnamese
3. Include example sentence
4. Include word type (Noun, Verb, Adjective, etc.)
5. Include category
6. Include a detailed image description for EACH word (not one for the whole topic)

For EACH vocabulary word, create a detailed image description that will be used to generate a visual representation. The image description should be:
- In English
- Descriptive and relevant to THAT SPECIFIC WORD
- Suitable for educational/learning context
- About 15-25 words
- Focus on the meaning and context of that specific word

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": """",
    ""vocabularies"": [
        {{
            ""word"": ""acquire"",
            ""definition"": ""đạt được, thu được"",
            ""example"": ""The company acquired a new building last year."",
            ""typeOfWord"": ""Verb"",
            ""category"": ""Business"",
            ""imageDescription"": ""A business person signing a contract and acquiring a new property, professional office setting with documents""
        }}
    ],
    ""hasSaveOption"": true,
    ""saveAction"": ""CREATE_VOCABULARY_LIST"",
    ""conversationType"": ""vocabulary_generation""
}}

CRITICAL REQUIREMENTS:
- You MUST generate EXACTLY {vocabularyCount} vocabulary words (not more, not less)
- EVERY vocabulary item MUST have an imageDescription field (do not skip any)
- imageDescription must be a valid string (not null, not empty)
- Set ""answer"" to empty string (""""), do not include any text in answer field
- Do not include any text outside the JSON object
- Start your response with {{ and end with }}.";

            var response = await CallOpenAIAPI(prompt);
            
            // Parse vocabularies if present
            if (response.Vocabularies != null && response.Vocabularies.Count > 0)
            {
                response.HasSaveOption = true;
                response.SaveAction = "CREATE_VOCABULARY_LIST";
                
                // Generate Pollinations URL cho TỪNG vocabulary từ imageDescription của nó
                // KHÔNG upload lên Cloudinary ngay - chỉ upload khi user click save button
                foreach (var vocab in response.Vocabularies)
                {
                    // Nếu không có imageDescription, tạo một mô tả đơn giản từ word
                    if (string.IsNullOrWhiteSpace(vocab.ImageDescription))
                    {
                        // Fallback: Tạo imageDescription từ word và definition
                        vocab.ImageDescription = $"A visual representation of {vocab.Word.ToLower()}, {vocab.Definition}";
                    }
                    
                    // Generate Pollinations AI URL từ imageDescription
                    // Lưu Pollinations URL tạm thời, sẽ upload lên Cloudinary khi user click save
                    var pollinationsUrl = GeneratePollinationsImageUrl(vocab.ImageDescription);
                    vocab.ImageUrl = pollinationsUrl; // Lưu Pollinations URL tạm thời
                }
            }
            
            return response;
        }

        // Extract số lượng từ vựng từ user request
        private int ExtractVocabularyCount(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return 10; // Default 10 từ

            // Tìm số trong message (ví dụ: "20 từ vựng", "tạo 20 từ", "20 words", "20 từ")
            var patterns = new[]
            {
                @"(\d+)\s*(?:từ|từ vựng|vocabulary|vocabularies|words|word)",
                @"tạo\s*(\d+)",
                @"generate\s*(\d+)",
                @"(\d+)\s*(?:cho|for|về|about)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
                {
                    return count;
                }
            }

            // Nếu không tìm thấy, tìm bất kỳ số nào trong message
            var numberMatch = System.Text.RegularExpressions.Regex.Match(message, @"\b(\d+)\b");
            if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int number))
            {
                // Chỉ chấp nhận số hợp lý (từ 1 đến 30)
                if (number >= 1 && number <= 30)
                {
                    return number;
                }
            }

            return 10; // Default 10 từ nếu không parse được
        }

        // Generate Pollinations AI image URL from description
        private string GeneratePollinationsImageUrl(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string encodedPrompt = Uri.EscapeDataString(description.Trim());
            string imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=512&height=512&seed=random&nologo=true&enhance=true&safe=true";
            return imageUrl;
        }

        private async Task<ChatResponseDTO> HandleGeneralQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are an AI assistant specialized in TOEIC English learning.

**User's Question:** {request.Message}

**Instructions:**
1. Answer in Vietnamese with English examples when relevant
2. Be helpful and educational
3. Provide TOEIC-specific context when applicable
4. Be encouraging and supportive

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Helpful answer in Vietnamese here"",
    ""suggestions"": [""Related question 1"", ""Related question 2""],
    ""conversationType"": ""general""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> CallOpenAIAPI(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_openAIOptions.ApiKey))
                {
                    throw new Exception("OpenAI API key is not configured");
                }
                
                // Thêm System Prompt vào đầu
                var systemPrompt = GetSystemPrompt();
                
                // Create HttpClient from factory
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _openAIOptions.ApiKey);
                
                // Prepare the request - OpenAI format
                var requestBody = new
                {
                    model = _openAIOptions.Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3, // Giảm temperature để JSON chính xác hơn
                    max_tokens = 8192 // Tăng token limit để đủ cho nhiều vocabularies
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make the API call to OpenAI
                var apiUrl = "https://api.openai.com/v1/chat/completions";
                var response = await httpClient.PostAsync(apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from OpenAI API");
                }

                // Parse the response - OpenAI format
                dynamic openAIResponse = JsonConvert.DeserializeObject(responseText);
                var generatedText = openAIResponse?.choices?[0]?.message?.content?.ToString();
                
                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new Exception("No content in OpenAI API response");
                }

                // Clean up potential markdown formatting
                generatedText = generatedText.Trim();
                
                // Remove markdown code blocks
                generatedText = generatedText.Replace("```json", "").Replace("```", "").Trim();
                
                // Extract JSON from text if wrapped in other text
                int firstBrace = generatedText.IndexOf('{');
                int lastBrace = generatedText.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    generatedText = generatedText.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
                
                // Try to parse JSON response
                ChatResponseDTO result;
                try
                {
                    result = JsonConvert.DeserializeObject<ChatResponseDTO>(generatedText);
                    if (result == null)
                    {
                        throw new Exception("Failed to deserialize OpenAI API response");
                    }
                    
                    // Nếu có vocabularies, luôn set answer rỗng để frontend chỉ hiển thị vocabulary list
                    if (result.Vocabularies != null && result.Vocabularies.Count > 0)
                    {
                        result.Answer = string.Empty;
                    }
                    // Validate that answer is not raw JSON
                    else if (string.IsNullOrWhiteSpace(result.Answer) || result.Answer.Trim().StartsWith("{"))
                    {
                        // Nếu không có vocabularies và answer rỗng hoặc là JSON, kiểm tra xem có phải là raw JSON không
                        if (generatedText.Contains("\"word\"") || generatedText.Contains("\"vocabularies\""))
                        {
                            result.Answer = string.Empty; // Nếu có vẻ như là JSON vocabulary, set rỗng
                        }
                        else if (!string.IsNullOrWhiteSpace(result.Answer) && result.Answer.Trim().StartsWith("{"))
                        {
                            result.Answer = string.Empty; // Nếu answer là JSON fragment, set rỗng
                        }
                    }
                    
                    // Loại bỏ bất kỳ JSON fragments nào trong answer
                    if (!string.IsNullOrWhiteSpace(result.Answer) && 
                        (result.Answer.Contains("\"word\"") || result.Answer.Contains("\"definition\"") || 
                         result.Answer.Contains("\"example\"") || result.Answer.Contains("\"typeOfWord\"")))
                    {
                        result.Answer = string.Empty; // Nếu answer chứa JSON fragments, set rỗng
                    }
                }
                catch (Exception ex)
                {
                    // If JSON parsing fails, try to extract vocabularies manually if possible
                    Console.WriteLine($"JSON Parse Error: {ex.Message}");
                    Console.WriteLine($"Raw Response: {generatedText.Substring(0, Math.Min(500, generatedText.Length))}");
                    
                    // Try to extract vocabularies using regex if JSON is malformed
                    var vocabularies = new List<GeneratedVocabularyDTO>();
                    try
                    {
                        // Try to find vocabulary patterns in the text
                        var wordPattern = @"\""word\"":\s*\""([^""]+)\""";
                        var definitionPattern = @"\""definition\"":\s*\""([^""]+)\""";
                        var examplePattern = @"\""example\"":\s*\""([^""]+)\""";
                        var typePattern = @"\""typeOfWord\"":\s*\""([^""]+)\""";
                        var categoryPattern = @"\""category\"":\s*\""([^""]+)\""";
                        var imageDescPattern = @"\""imageDescription\"":\s*\""([^""]+)\""";
                        
                        // If we can't parse properly, return error message
                        result = new ChatResponseDTO
                        {
                            Answer = "Xin lỗi, tôi gặp lỗi khi xử lý phản hồi. Vui lòng thử lại.",
                            ConversationType = "error",
                            Suggestions = new List<string> { "Thử hỏi lại", "Đặt câu hỏi khác" },
                            Vocabularies = vocabularies
                        };
                    }
                    catch
                    {
                        // Final fallback
                        result = new ChatResponseDTO
                        {
                            Answer = "Xin lỗi, tôi gặp lỗi khi xử lý phản hồi. Vui lòng thử lại.",
                            ConversationType = "error",
                            Suggestions = new List<string> { "Thử hỏi lại", "Đặt câu hỏi khác" }
                        };
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"OpenAI API Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return new ChatResponseDTO
                {
                    Answer = $"Xin lỗi, tôi không thể xử lý câu hỏi này lúc này. Vui lòng thử lại sau.\n\nChi tiết lỗi: {ex.Message}",
                    ConversationType = "error",
                    Suggestions = new List<string> { "Thử hỏi lại", "Liên hệ hỗ trợ" }
                };
            }
        }

        private async Task<List<DataLayer.Models.Vocabulary>> GetUserVocabularies(int userId)
        {
            return await _context.Vocabularies
                .Where(v => v.VocabularyList.MakeBy == userId && v.IsDeleted != true)
                .Include(v => v.VocabularyList)
                .Take(20)
                .ToListAsync();
        }

        private async Task<List<DataLayer.Models.ExamAttempt>> GetUserRecentScores(int userId)
        {
            return await _context.ExamAttempts
                .Where(e => e.UserID == userId)
                .Include(e => e.Exam)
                .OrderByDescending(e => e.StartTime)
                .Take(5)
                .ToListAsync();
        }

        public async Task<SaveVocabularyResponseDTO> SaveGeneratedVocabularies(SaveVocabularyRequestDTO request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Tạo VocabularyList mới (không cần ImageUrl cho folder, mỗi vocabulary có ảnh riêng)
                var vocabularyList = new VocabularyList
                {
                    MakeBy = request.UserId,
                    Name = request.FolderName,
                    CreateAt = DateTime.Now,
                    // Tự động public và publish folder khi lưu từ từ AI để học sinh xem được ngay
                    IsPublic = true,
                    IsDeleted = false,
                    Status = "Published"
                };
                
                _context.VocabularyLists.Add(vocabularyList);
                await _context.SaveChangesAsync();
                
                // 2. Tạo các Vocabulary và upload ảnh lên Cloudinary khi user click save
                var vocabularies = new List<DataLayer.Models.Vocabulary>();
                foreach (var vocab in request.Vocabularies)
                {
                    // Upload ảnh lên Cloudinary từ Pollinations URL (nếu có)
                    string? finalImageUrl = vocab.ImageUrl; // Mặc định giữ nguyên URL
                    
                    // Kiểm tra nếu ImageUrl là Pollinations URL (chứa "pollinations.ai")
                    if (!string.IsNullOrWhiteSpace(vocab.ImageUrl) && vocab.ImageUrl.Contains("pollinations.ai"))
                    {
                        try
                        {
                            // Upload từ Pollinations URL lên Cloudinary
                            var uploadResult = await _uploadService.UploadFromUrlAsync(vocab.ImageUrl);
                            finalImageUrl = uploadResult.Url; // Lưu Cloudinary URL
                        }
                        catch (Exception ex)
                        {
                            // Nếu upload fail, fallback về Pollinations URL hoặc null
                            Console.WriteLine($"Warning: Failed to upload image to Cloudinary for vocabulary '{vocab.Word}': {ex.Message}");
                            // Giữ nguyên Pollinations URL nếu upload thất bại
                            finalImageUrl = vocab.ImageUrl;
                        }
                    }
                    
                    var vocabulary = new DataLayer.Models.Vocabulary
                    {
                        VocabularyListId = vocabularyList.VocabularyListId,
                        Word = vocab.Word,
                        Definition = vocab.Definition,
                        Example = vocab.Example,
                        TypeOfWord = vocab.TypeOfWord,
                        Category = vocab.Category,
                        IsDeleted = false,
                        ImageUrl = finalImageUrl // Lưu Cloudinary URL hoặc Pollinations URL (nếu upload fail)
                    };
                    vocabularies.Add(vocabulary);
                }
                
                _context.Vocabularies.AddRange(vocabularies);
                await _context.SaveChangesAsync();
                
                // 3. Tạo UserSpacedRepetition cho từng từ
                var spacedRepetitions = vocabularies.Select(v => new DataLayer.Models.UserSpacedRepetition
                {
                    UserId = request.UserId,
                    VocabularyListId = vocabularyList.VocabularyListId,
                    LastReviewedAt = DateTime.Now,
                    NextReviewAt = DateTime.Now.AddDays(1),
                    ReviewCount = 0,
                    Intervals = 1,
                    Status = "New"
                }).ToList();
                
                _context.UserSpacedRepetitions.AddRange(spacedRepetitions);
                await _context.SaveChangesAsync();
                
                // 4. Lưu chat history vào UserNote
                await SaveChatMessage(request.UserId, $"Tạo folder '{request.FolderName}' với {vocabularies.Count} từ vựng", 
                    $"Đã tạo thành công folder '{request.FolderName}' và lưu {vocabularies.Count} từ vựng!");
                
                await transaction.CommitAsync();
                
                return new SaveVocabularyResponseDTO
                {
                    Success = true,
                    Message = $"Đã tạo folder '{request.FolderName}' và lưu {vocabularies.Count} từ vựng!",
                    VocabularyListId = vocabularyList.VocabularyListId,
                    VocabularyCount = vocabularies.Count
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Lỗi khi lưu từ vựng: {ex.Message}");
            }
        }

        private async Task SaveChatMessage(int userId, string userMessage, string aiResponse)
        {
            try
            {
                // Lưu câu hỏi của user
                var userNote = new DataLayer.Models.UserNote
                {
                    UserId = userId,
                    ArticleId = 0, // Không liên quan article
                    SectionId = 0, // Không liên quan section
                    NoteContent = $"User: {userMessage}",
                    CreateAt = DateTime.Now
                };

                // Lưu câu trả lời của AI
                var aiNote = new DataLayer.Models.UserNote
                {
                    UserId = userId,
                    ArticleId = 0,
                    SectionId = 0,
                    NoteContent = $"AI: {aiResponse}",
                    CreateAt = DateTime.Now
                };

                _context.UserNotes.Add(userNote);
                _context.UserNotes.Add(aiNote);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the main flow
                Console.WriteLine($"Error saving chat message: {ex.Message}");
            }
        }
    }
}
