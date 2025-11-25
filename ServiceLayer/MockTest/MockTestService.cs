using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.DTOs;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.MockTest;
using GenerativeAI;
using GenerativeAI.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RepositoryLayer;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.MockTest;

namespace ServiceLayer.MockTest
{
    public class MockTestService : IMockTestService
    {
        private readonly IMockTestRepository _mockTestRepository;
        private readonly IExamAttemptRepository _examAttemptRepository;
        private readonly IArticleRepository _articleRepository;
        private readonly IConfiguration _configuration;
        private readonly string _geminiApiKey;
        private readonly string _geminiModel;

        public MockTestService(
            IMockTestRepository mockTestRepository,
            IExamAttemptRepository examAttemptRepository,
            IArticleRepository articleRepository,
            IConfiguration configuration)
        {
            _mockTestRepository = mockTestRepository;
            _examAttemptRepository = examAttemptRepository;
            _articleRepository = articleRepository;
            _configuration = configuration;

            _geminiApiKey = _configuration["GeminiAI:ApiKey"]
                ?? _configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini API key is not configured.");

            _geminiModel = _configuration["GeminiAI:Model"]
                ?? _configuration["Gemini:Model"]
                ?? "gemini-2.5-flash";
        }

        public async Task<List<ExamPartDTO>> GetMocktestAsync()
        {
            int[] examPartids = new int[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };
            return await _mockTestRepository.GetMocktestAsync(examPartids);
        }

        public async Task<MocktestFeedbackDTO> GetMocktestFeedbackAsync(int examAttemptId)
        {
            var examAttempt = await _examAttemptRepository.GetExamAttemptById(examAttemptId);
            if (examAttempt == null)
            {
                throw new Exception($"Exam attempt with ID {examAttemptId} not found.");
            }

            var articleNames = await _articleRepository.GetArticleName();

            // Tạo prompt chi tiết để gửi lên AI
            var prompt = BuildAnalysisPrompt(examAttempt, articleNames);

            // Gọi AI API
            var aiResponse = await GenerateFeedbackFromAIAsync(prompt);

            // Parse response thành MocktestFeedbackDTO
            var feedback = ParseAIResponse(aiResponse);

            return feedback;
        }

        private string BuildAnalysisPrompt(DataLayer.DTOs.UserAnswer.ExamAttemptDetailResponseDTO examAttempt, List<string> articleNames)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Bạn là một chuyên gia phân tích kết quả thi TOEIC. Hãy phân tích kết quả bài làm của học viên và đưa ra phản hồi chi tiết.");
            sb.AppendLine();
            sb.AppendLine("=== THÔNG TIN BÀI LÀM ===");
            sb.AppendLine($"Tên bài thi: {examAttempt.ExamAttemptInfo?.ExamName ?? "N/A"}");
            sb.AppendLine($"Điểm số: {examAttempt.ExamAttemptInfo?.Score ?? 0}");
            sb.AppendLine($"Trạng thái: {examAttempt.ExamAttemptInfo?.Status ?? "N/A"}");
            sb.AppendLine($"Thời gian bắt đầu: {examAttempt.ExamAttemptInfo?.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Thời gian kết thúc: {examAttempt.ExamAttemptInfo?.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
            sb.AppendLine();

            // Phân tích Listening
            if (examAttempt.ListeningAnswers != null && examAttempt.ListeningAnswers.Any())
            {
                sb.AppendLine("=== KẾT QUẢ LISTENING ===");
                var listeningCorrect = examAttempt.ListeningAnswers.Count(a => a.IsCorrect);
                var listeningTotal = examAttempt.ListeningAnswers.Count;
                var listeningScore = examAttempt.ListeningAnswers.Sum(a => a.Score ?? 0);
                sb.AppendLine($"Số câu đúng: {listeningCorrect}/{listeningTotal}");
                sb.AppendLine($"Tổng điểm: {listeningScore}");
                
                // Chi tiết các câu sai
                var listeningWrong = examAttempt.ListeningAnswers.Where(a => !a.IsCorrect).ToList();
                if (listeningWrong.Any())
                {
                    sb.AppendLine("Các câu sai:");
                    foreach (var answer in listeningWrong.Take(10)) // Giới hạn 10 câu để không quá dài
                    {
                        sb.AppendLine($"- Câu {answer.Question?.QuestionNumber}: {answer.Question?.StemText?.Substring(0, Math.Min(100, answer.Question?.StemText?.Length ?? 0))}...");
                    }
                }
                sb.AppendLine();
            }

            // Phân tích Reading
            if (examAttempt.ReadingAnswers != null && examAttempt.ReadingAnswers.Any())
            {
                sb.AppendLine("=== KẾT QUẢ READING ===");
                var readingCorrect = examAttempt.ReadingAnswers.Count(a => a.IsCorrect);
                var readingTotal = examAttempt.ReadingAnswers.Count;
                var readingScore = examAttempt.ReadingAnswers.Sum(a => a.Score ?? 0);
                sb.AppendLine($"Số câu đúng: {readingCorrect}/{readingTotal}");
                sb.AppendLine($"Tổng điểm: {readingScore}");
                
                var readingWrong = examAttempt.ReadingAnswers.Where(a => !a.IsCorrect).ToList();
                if (readingWrong.Any())
                {
                    sb.AppendLine("Các câu sai:");
                    foreach (var answer in readingWrong.Take(10))
                    {
                        sb.AppendLine($"- Câu {answer.Question?.QuestionNumber}: {answer.Question?.StemText?.Substring(0, Math.Min(100, answer.Question?.StemText?.Length ?? 0))}...");
                    }
                }
                sb.AppendLine();
            }

            // Phân tích Writing
            if (examAttempt.WritingAnswers != null && examAttempt.WritingAnswers.Any())
            {
                sb.AppendLine("=== KẾT QUẢ WRITING ===");
                sb.AppendLine($"Số câu đã làm: {examAttempt.WritingAnswers.Count}");
                foreach (var answer in examAttempt.WritingAnswers.Take(5))
                {
                    sb.AppendLine($"Câu {answer.Question?.QuestionNumber}:");
                    sb.AppendLine($"- Câu hỏi: {answer.Question?.StemText?.Substring(0, Math.Min(150, answer.Question?.StemText?.Length ?? 0))}...");
                    sb.AppendLine($"- Câu trả lời: {answer.UserAnswerContent?.Substring(0, Math.Min(200, answer.UserAnswerContent?.Length ?? 0))}...");
                    if (!string.IsNullOrEmpty(answer.FeedbackFromAI))
                    {
                        sb.AppendLine($"- Feedback: {answer.FeedbackFromAI.Substring(0, Math.Min(150, answer.FeedbackFromAI.Length))}...");
                    }
                    sb.AppendLine();
                }
            }

            // Phân tích Speaking
            if (examAttempt.SpeakingAnswers != null && examAttempt.SpeakingAnswers.Any())
            {
                sb.AppendLine("=== KẾT QUẢ SPEAKING ===");
                sb.AppendLine($"Số câu đã làm: {examAttempt.SpeakingAnswers.Count}");
                foreach (var answer in examAttempt.SpeakingAnswers.Take(5))
                {
                    sb.AppendLine($"Câu {answer.Question?.QuestionNumber}:");
                    sb.AppendLine($"- Overall Score: {answer.OverallScore}");
                    sb.AppendLine($"- Pronunciation: {answer.PronunciationScore}, Accuracy: {answer.AccuracyScore}, Fluency: {answer.FluencyScore}");
                    if (!string.IsNullOrEmpty(answer.Transcript))
                    {
                        sb.AppendLine($"- Transcript: {answer.Transcript.Substring(0, Math.Min(200, answer.Transcript.Length))}...");
                    }
                    sb.AppendLine();
                }
            }

            // Danh sách bài học
            if (articleNames != null && articleNames.Any())
            {
                sb.AppendLine("=== DANH SÁCH BÀI HỌC CÓ SẴN ===");
                sb.AppendLine(string.Join(", ", articleNames));
                sb.AppendLine();
            }

            sb.AppendLine("=== YÊU CẦU ===");
            sb.AppendLine("Hãy phân tích và trả về JSON với định dạng sau (chỉ trả về JSON, không có text thêm):");
            sb.AppendLine("{");
            sb.AppendLine("  \"Overview\": \"Tổng quan về bài làm của học viên (2-3 câu)\",");
            sb.AppendLine("  \"ToeicScore\": <điểm TOEIC từ 0-990, tính dựa trên điểm số hiện tại>, ");
            sb.AppendLine("  \"Strengths\": [\"điểm mạnh 1\", \"điểm mạnh 2\", \"điểm mạnh 3\"],");
            sb.AppendLine("  \"Weaknesses\": [\"điểm yếu 1\", \"điểm yếu 2\", \"điểm yếu 3\"],");
            sb.AppendLine("  \"ActionPlan\": \"Kế hoạch học tập chi tiết, bao gồm đề xuất các bài học phù hợp từ danh sách bài học (nếu có). Nếu có articleNames, hãy đề xuất cụ thể tên các bài học.\"");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Lưu ý:");
            sb.AppendLine("- ToeicScore: Quy đổi điểm số hiện tại sang thang điểm TOEIC 0-990");
            sb.AppendLine("- Strengths và Weaknesses: Mỗi danh sách có 3-5 mục");
            sb.AppendLine("- ActionPlan: Phải cụ thể, có thể thực hiện được, và nếu có articleNames thì phải đề xuất tên bài học cụ thể");

            return sb.ToString();
        }

        private async Task<string> GenerateFeedbackFromAIAsync(string prompt)
        {
            try
            {
                var generativeModel = new GenerativeModel(_geminiApiKey, new ModelParams { Model = _geminiModel });
                var response = await generativeModel.GenerateContentAsync(prompt);
                var responseText = response.Text;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new Exception("No response received from Gemini API");
                }

                responseText = responseText.Trim().Replace("```json", "").Replace("```", "");
                return responseText;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini API error: {ex.Message}");
            }
        }

        private MocktestFeedbackDTO ParseAIResponse(string aiResponse)
        {
            try
            {
                // Làm sạch response - loại bỏ markdown code blocks nếu có
                var cleanedResponse = aiResponse.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }
                cleanedResponse = cleanedResponse.Trim();

                // Parse JSON
                var feedback = JsonConvert.DeserializeObject<MocktestFeedbackDTO>(cleanedResponse);
                
                if (feedback == null)
                {
                    throw new Exception("Failed to parse AI response.");
                }

                // Đảm bảo các list không null
                feedback.Strengths = feedback.Strengths ?? new List<string>();
                feedback.Weaknesses = feedback.Weaknesses ?? new List<string>();
                feedback.Overview = feedback.Overview ?? "Không có thông tin tổng quan.";
                feedback.ActionPlan = feedback.ActionPlan ?? "Không có kế hoạch học tập.";

                return feedback;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing AI response: {ex.Message}. Response: {aiResponse.Substring(0, Math.Min(500, aiResponse.Length))}");
            }
        }
    }
}
