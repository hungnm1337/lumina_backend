using DataLayer.DTOs.Chat;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;

namespace ServiceLayer.Chat
{
    public class ChatService : IChatService
    {
        private readonly LuminaSystemContext _context;
        private readonly IConfiguration _configuration;
        private readonly string? _apiKey;
        private readonly string? _baseUrl;
        private readonly string? _model;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatService(LuminaSystemContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _apiKey = _configuration["Gemini:ApiKey"];
            _baseUrl = _configuration["Gemini:BaseUrl"];
            _model = _configuration["Gemini:Model"];
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ChatResponseDTO> ProcessMessage(ChatRequestDTO request)
        {
            try
            {
                // Xác định loại câu hỏi
                var questionType = DetermineQuestionType(request.Message);
                
                switch (questionType)
                {
                    case "vocabulary":
                        return await HandleVocabularyQuestion(request);
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

        private string DetermineQuestionType(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Vocabulary generation
            if (lowerMessage.Contains("tạo") && (lowerMessage.Contains("từ vựng") || lowerMessage.Contains("vocabulary")))
                return "vocabulary_generation";
                
            // Vocabulary questions
            if (lowerMessage.Contains("từ vựng") || lowerMessage.Contains("vocabulary") || 
                lowerMessage.Contains("nghĩa") || lowerMessage.Contains("từ"))
                return "vocabulary";
                
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

            return await CallGeminiAPI(prompt);
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

            return await CallGeminiAPI(prompt);
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

            return await CallGeminiAPI(prompt);
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

            return await CallGeminiAPI(prompt);
        }

        private async Task<ChatResponseDTO> GenerateVocabularyResponse(ChatRequestDTO request)
        {
            var prompt = $@"You are a TOEIC vocabulary expert. Generate vocabulary words based on user's request.

**User's Request:** {request.Message}

**Instructions:**
Generate exactly 10 vocabulary words related to the requested topic. Each word should be:
1. Commonly used in TOEIC exams
2. Include definition in Vietnamese
3. Include example sentence
4. Include word type (Noun, Verb, Adjective, etc.)
5. Include category

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Here are 10 vocabulary words for your request:"",
    ""vocabularies"": [
        {{
            ""word"": ""acquire"",
            ""definition"": ""đạt được, thu được"",
            ""example"": ""The company acquired a new building last year."",
            ""typeOfWord"": ""Verb"",
            ""category"": ""Business""
        }}
    ],
    ""hasSaveOption"": true,
    ""saveAction"": ""CREATE_VOCABULARY_LIST"",
    ""conversationType"": ""vocabulary_generation""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            var response = await CallGeminiAPI(prompt);
            
            // Parse vocabularies if present
            if (response.Vocabularies != null && response.Vocabularies.Count > 0)
            {
                response.HasSaveOption = true;
                response.SaveAction = "CREATE_VOCABULARY_LIST";
            }
            
            return response;
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

            return await CallGeminiAPI(prompt);
        }

        private async Task<ChatResponseDTO> CallGeminiAPI(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    throw new Exception("Gemini API key is not configured");
                }
                
                // Create HttpClient from factory
                using var httpClient = _httpClientFactory.CreateClient();
                
                // Prepare the request
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 2048,
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make the API call using configuration from appsettings.json
                var baseUrl = _baseUrl ?? "https://generativelanguage.googleapis.com/v1beta/models";
                var model = _model ?? "gemini-1.5-flash";
                var apiUrl = $"{baseUrl}/{model}:generateContent?key={_apiKey}";
                var response = await httpClient.PostAsync(apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                // Parse the response
                dynamic geminiResponse = JsonConvert.DeserializeObject(responseText);
                var generatedText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text?.ToString();
                
                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new Exception("No content in Gemini API response");
                }

                // Clean up potential markdown formatting
                generatedText = generatedText.Trim().Replace("```json", "").Replace("```", "").Trim();
                
                // Try to parse JSON response
                ChatResponseDTO result;
                try
                {
                    result = JsonConvert.DeserializeObject<ChatResponseDTO>(generatedText);
                if (result == null)
                {
                    throw new Exception("Failed to deserialize Gemini API response");
                    }
                }
                catch
                {
                    // If JSON parsing fails, treat as plain text response
                    result = new ChatResponseDTO
                    {
                        Answer = generatedText,
                        ConversationType = "general",
                        Suggestions = new List<string> { "Thử hỏi lại", "Đặt câu hỏi khác" }
                    };
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Gemini API Error: {ex.Message}");
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
                .Where(e => e.UserId == userId)
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
                // 1. Tạo VocabularyList mới
                var vocabularyList = new VocabularyList
                {
                    MakeBy = request.UserId,
                    Name = request.FolderName,
                    CreateAt = DateTime.Now,
                    IsPublic = false,
                    IsDeleted = false,
                    Status = "Approved"
                };
                
                _context.VocabularyLists.Add(vocabularyList);
                await _context.SaveChangesAsync();
                
                // 2. Tạo các Vocabulary
                var vocabularies = new List<DataLayer.Models.Vocabulary>();
                foreach (var vocab in request.Vocabularies)
                {
                    var vocabulary = new DataLayer.Models.Vocabulary
                    {
                        VocabularyListId = vocabularyList.VocabularyListId,
                        Word = vocab.Word,
                        Definition = vocab.Definition,
                        Example = vocab.Example,
                        TypeOfWord = vocab.TypeOfWord,
                        Category = vocab.Category,
                        IsDeleted = false
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
