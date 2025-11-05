using DataLayer.DTOs.AI;
using GenerativeAI;
using GenerativeAI.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.UserNoteAI
{
    /// <summary>
    /// AI-powered chatbot service using Gemini 2.5 for lesson Q&A
    /// </summary>
    public class AIChatService : IAIChatService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIChatService> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;

        public AIChatService(IConfiguration configuration, ILogger<AIChatService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key is not configured.");
            _modelName = _configuration["Gemini:ModelName"] ?? "gemini-2.5-flash";
        }

        public async Task<ChatResponseDTO> AskQuestionAsync(ChatRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserQuestion))
                {
                    return new ChatResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "User question cannot be empty."
                    };
                }

                if (string.IsNullOrWhiteSpace(request.LessonContent))
                {
                    return new ChatResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "Lesson content cannot be empty."
                    };
                }

                // Create prompt for Gemini
                var prompt = CreateLessonQuestionPrompt(request);

                // Initialize Gemini model
                var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = _modelName });

                // Call Gemini API
                var response = await generativeModel.GenerateContentAsync(prompt);
                var responseText = response.Text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                // Parse and structure the response
                var chatResponse = ParseAIResponse(responseText);
                
                _logger.LogInformation("AI Chat response generated successfully for question: {Question}", 
                    request.UserQuestion.Substring(0, Math.Min(50, request.UserQuestion.Length)));

                return chatResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing AI chat question");
                return new ChatResponseDTO
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}",
                    Answer = "Xin lỗi, tôi gặp lỗi khi xử lý câu hỏi của bạn. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<ChatConversationResponseDTO> ContinueConversationAsync(ChatRequestDTO request)
        {
            try
            {
                // Build conversation context
                var conversationContext = BuildConversationContext(request);

                // Get AI response
                var prompt = CreateConversationPrompt(request, conversationContext);
                
                var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = _modelName });
                var response = await generativeModel.GenerateContentAsync(prompt);
                var responseText = response.Text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                var chatResponse = ParseAIResponse(responseText);

                // Update conversation history
                var updatedHistory = request.ConversationHistory ?? new List<ChatMessageDTO>();
                updatedHistory.Add(new ChatMessageDTO
                {
                    Role = "user",
                    Content = request.UserQuestion,
                    Timestamp = DateTime.UtcNow
                });
                updatedHistory.Add(new ChatMessageDTO
                {
                    Role = "assistant",
                    Content = chatResponse.Answer,
                    Timestamp = DateTime.UtcNow
                });

                return new ChatConversationResponseDTO
                {
                    CurrentResponse = chatResponse,
                    ConversationHistory = updatedHistory,
                    SessionId = Guid.NewGuid().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while continuing conversation");
                return new ChatConversationResponseDTO
                {
                    CurrentResponse = new ChatResponseDTO
                    {
                        Success = false,
                        ErrorMessage = $"Error: {ex.Message}",
                        Answer = "Xin lỗi, tôi gặp lỗi khi tiếp tục cuộc trò chuyện. Vui lòng thử lại."
                    }
                };
            }
        }

        public async Task<ChatResponseDTO> GenerateSuggestedQuestionsAsync(string lessonContent, string? lessonTitle = null)
        {
            try
            {
                var prompt = CreateSuggestedQuestionsPrompt(lessonContent, lessonTitle);
                
                var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = _modelName });
                var response = await generativeModel.GenerateContentAsync(prompt);
                var responseText = response.Text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                // Parse suggested questions
                var questions = ParseSuggestedQuestions(responseText);

                return new ChatResponseDTO
                {
                    Success = true,
                    Answer = "Đây là một số câu hỏi gợi ý dựa trên nội dung bài học:",
                    SuggestedQuestions = questions,
                    ConfidenceScore = 95
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating suggested questions");
                return new ChatResponseDTO
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<ChatResponseDTO> ExplainConceptAsync(string concept, string lessonContext)
        {
            try
            {
                var prompt = CreateConceptExplanationPrompt(concept, lessonContext);
                
                var generativeModel = new GenerativeModel(_apiKey, new ModelParams { Model = _modelName });
                var response = await generativeModel.GenerateContentAsync(prompt);
                var responseText = response.Text;

                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                return new ChatResponseDTO
                {
                    Success = true,
                    Answer = responseText,
                    ConfidenceScore = 90,
                    RelatedTopics = ExtractRelatedTopics(responseText)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while explaining concept");
                return new ChatResponseDTO
                {
                    Success = false,
                    ErrorMessage = $"Error: {ex.Message}"
                };
            }
        }

        #region Private Helper Methods

        private string CreateLessonQuestionPrompt(ChatRequestDTO request)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bạn là một trợ lý AI giáo dục chuyên về TOEIC và tiếng Anh. Nhiệm vụ của bạn là trả lời các câu hỏi của học sinh dựa trên nội dung bài học.");
            promptBuilder.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(request.LessonTitle))
            {
                promptBuilder.AppendLine($"**Tiêu đề bài học:** {request.LessonTitle}");
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("**Nội dung bài học:**");
            promptBuilder.AppendLine(request.LessonContent);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("---");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"**Câu hỏi của học sinh:** {request.UserQuestion}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Hướng dẫn:**");
            promptBuilder.AppendLine("1. Trả lời câu hỏi một cách rõ ràng, dễ hiểu và chi tiết");
            promptBuilder.AppendLine("2. Sử dụng ví dụ từ nội dung bài học nếu có thể");
            promptBuilder.AppendLine("3. Đưa ra ví dụ bổ sung để minh họa nếu cần");
            promptBuilder.AppendLine("4. Giải thích bằng tiếng Việt, nhưng giữ các thuật ngữ tiếng Anh quan trọng");
            promptBuilder.AppendLine("5. Nếu câu hỏi không liên quan đến nội dung bài học, hãy lịch sự hướng dẫn học sinh quay lại nội dung chính");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Hãy trả lời câu hỏi một cách thân thiện và khuyến khích học sinh tiếp tục học tập.");

            return promptBuilder.ToString();
        }

        private string CreateConversationPrompt(ChatRequestDTO request, string conversationContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bạn là một trợ lý AI giáo dục đang tiếp tục cuộc trò chuyện với học sinh về bài học TOEIC.");
            promptBuilder.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(request.LessonTitle))
            {
                promptBuilder.AppendLine($"**Tiêu đề bài học:** {request.LessonTitle}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Nội dung bài học:**");
            promptBuilder.AppendLine(request.LessonContent);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Lịch sử cuộc trò chuyện:**");
            promptBuilder.AppendLine(conversationContext);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"**Câu hỏi tiếp theo:** {request.UserQuestion}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Hãy trả lời dựa trên ngữ cảnh cuộc trò chuyện và nội dung bài học. Giữ sự liên tục và nhất quán trong câu trả lời.");

            return promptBuilder.ToString();
        }

        private string CreateSuggestedQuestionsPrompt(string lessonContent, string? lessonTitle)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Dựa trên nội dung bài học sau, hãy tạo 5 câu hỏi gợi ý mà học sinh có thể hỏi để hiểu rõ hơn về bài học.");
            promptBuilder.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(lessonTitle))
            {
                promptBuilder.AppendLine($"**Tiêu đề:** {lessonTitle}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Nội dung bài học:**");
            promptBuilder.AppendLine(lessonContent);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Yêu cầu:**");
            promptBuilder.AppendLine("- Tạo 5 câu hỏi khác nhau");
            promptBuilder.AppendLine("- Câu hỏi nên đa dạng (từ cơ bản đến nâng cao)");
            promptBuilder.AppendLine("- Mỗi câu hỏi trên một dòng");
            promptBuilder.AppendLine("- Đánh số thứ tự: 1., 2., 3., 4., 5.");
            promptBuilder.AppendLine("- Chỉ trả về danh sách câu hỏi, không cần giải thích thêm");

            return promptBuilder.ToString();
        }

        private string CreateConceptExplanationPrompt(string concept, string lessonContext)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Hãy giải thích khái niệm hoặc thuật ngữ sau một cách chi tiết và dễ hiểu: **{concept}**");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Ngữ cảnh từ bài học:**");
            promptBuilder.AppendLine(lessonContext);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Yêu cầu:**");
            promptBuilder.AppendLine("1. Định nghĩa rõ ràng");
            promptBuilder.AppendLine("2. Giải thích cách sử dụng");
            promptBuilder.AppendLine("3. Đưa ra ví dụ cụ thể (ít nhất 2 ví dụ)");
            promptBuilder.AppendLine("4. Lưu ý các lỗi thường gặp (nếu có)");
            promptBuilder.AppendLine("5. Giải thích bằng tiếng Việt, nhưng giữ các thuật ngữ tiếng Anh quan trọng");

            return promptBuilder.ToString();
        }

        private string BuildConversationContext(ChatRequestDTO request)
        {
            if (request.ConversationHistory == null || !request.ConversationHistory.Any())
            {
                return "Đây là câu hỏi đầu tiên trong cuộc trò chuyện.";
            }

            var contextBuilder = new StringBuilder();
            foreach (var message in request.ConversationHistory.TakeLast(5)) // Keep last 5 messages for context
            {
                var role = message.Role == "user" ? "Học sinh" : "Trợ lý AI";
                contextBuilder.AppendLine($"{role}: {message.Content}");
                contextBuilder.AppendLine();
            }

            return contextBuilder.ToString();
        }

        private ChatResponseDTO ParseAIResponse(string responseText)
        {
            // Try to extract structured information from response
            var response = new ChatResponseDTO
            {
                Answer = responseText.Trim(),
                Success = true,
                ConfidenceScore = 85,
                Timestamp = DateTime.UtcNow
            };

            // Extract suggested follow-up questions if present
            response.SuggestedQuestions = ExtractSuggestedQuestions(responseText);
            
            // Extract related topics if present
            response.RelatedTopics = ExtractRelatedTopics(responseText);

            return response;
        }

        private List<string> ExtractSuggestedQuestions(string text)
        {
            var questions = new List<string>();
            
            // Simple extraction of questions (lines ending with ?)
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.EndsWith('?') && trimmed.Length > 10)
                {
                    // Remove numbering if present
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^\d+\.\s*", "");
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        questions.Add(cleaned);
                    }
                }
            }

            return questions.Take(3).ToList(); // Return top 3 questions
        }

        private List<string> ParseSuggestedQuestions(string responseText)
        {
            var questions = new List<string>();
            var lines = responseText.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Remove numbering (1. 2. etc.)
                var cleaned = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^\d+\.\s*", "");
                
                // Remove markdown formatting
                cleaned = cleaned.Replace("**", "").Replace("*", "").Trim();
                
                if (!string.IsNullOrWhiteSpace(cleaned) && cleaned.Length > 10)
                {
                    questions.Add(cleaned);
                }
            }

            return questions.Take(5).ToList();
        }

        private List<string> ExtractRelatedTopics(string text)
        {
            var topics = new List<string>();
            
            // Simple keyword extraction - can be enhanced with NLP
            var keywords = new[] { "grammar", "vocabulary", "tense", "structure", "usage", 
                                  "ngữ pháp", "từ vựng", "thì", "cấu trúc", "cách dùng" };
            
            foreach (var keyword in keywords)
            {
                if (text.ToLower().Contains(keyword.ToLower()) && !topics.Contains(keyword))
                {
                    topics.Add(keyword);
                }
            }

            return topics.Take(5).ToList();
        }

        #endregion
    }
}
