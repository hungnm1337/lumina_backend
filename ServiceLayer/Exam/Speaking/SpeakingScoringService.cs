using DataLayer.DTOs;
using DataLayer.DTOs.Exam.Speaking;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Speech;
using ServiceLayer.UploadFile;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Speaking
{
    public class SpeakingScoringService : ISpeakingScoringService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadService _uploadService;
        private readonly IAzureSpeechService _azureSpeechService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SpeakingScoringService(
            IUnitOfWork unitOfWork,
            IUploadService uploadService,
            IAzureSpeechService azureSpeechService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _uploadService = uploadService;
            _azureSpeechService = azureSpeechService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<SpeakingScoringResultDTO> ProcessAndScoreAnswerAsync(IFormFile audioFile, int questionId, int attemptId)
        {
            var uploadResult = await _uploadService.UploadFileAsync(audioFile);
            var question = await _unitOfWork.Questions.GetAsync(q => q.QuestionId == questionId);
            if (question == null || string.IsNullOrEmpty(question.SampleAnswer))
            {
                throw new Exception($"Question with ID {questionId} or its sample answer not found.");
            }

            // Prefer analyzing from Cloudinary MP3 transformation (Python also expects MP3)
            // Build a deterministic MP3 URL using Cloudinary cloud name and public id
            var cloudName = _configuration["CloudinarySettings:CloudName"];
            var publicId = uploadResult.PublicId; // e.g., lumina/audio/file_uuid
            // Force 16kHz sample rate for better ASR
            var transformedMp3Url = $"https://res.cloudinary.com/{cloudName}/video/upload/f_mp3,ar_16000/{publicId}.mp3";
            Console.WriteLine($"[Speaking] MP3 URL for Azure: {transformedMp3Url}");

            // Wait briefly for Cloudinary to finish the MP3 transform
            await EnsureCloudinaryAssetReady(transformedMp3Url);

            // Use en-GB for better Vietnamese-accented English recognition
            Console.WriteLine($"[Speaking] Using language model: en-GB");
            var azureResult = await _azureSpeechService.AnalyzePronunciationFromUrlAsync(transformedMp3Url, question.SampleAnswer, "en-GB");
            Console.WriteLine($"[Speaking] Transcript result: {azureResult.Transcript}");
            if (!string.IsNullOrEmpty(azureResult.ErrorMessage) || string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".")
            {
                Console.WriteLine($"[Speaking] Azure URL analysis failed: {azureResult.ErrorMessage}. Retrying MP3 URL once more.");
                // Retry once more after short delay
                await Task.Delay(800);
                await EnsureCloudinaryAssetReady(transformedMp3Url);
                azureResult = await _azureSpeechService.AnalyzePronunciationFromUrlAsync(transformedMp3Url, question.SampleAnswer, "en-GB");
            }

            // Validate transcript after retry - but don't throw exception
            if (string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".")
            {
                Console.WriteLine("[Speaking] Azure transcription failed after retries, using fallback transcript");
                azureResult.Transcript = "."; // Fallback for NLP processing
            }

            var nlpResult = await GetNlpScoresAsync(azureResult.Transcript, question.SampleAnswer);
            var overallScore = CalculateOverallScore(question.QuestionType, azureResult, nlpResult);

            // Save speaking answer to database
            var userAnswerSpeaking = new UserAnswerSpeaking
            {
                AttemptID = attemptId,
                QuestionId = questionId,
                Transcript = azureResult.Transcript,
                AudioUrl = transformedMp3Url,
                PronunciationScore = (decimal?)azureResult.PronunciationScore,
                AccuracyScore = (decimal?)azureResult.AccuracyScore,
                FluencyScore = (decimal?)azureResult.FluencyScore,
                CompletenessScore = (decimal?)azureResult.CompletenessScore,
                GrammarScore = (decimal?)nlpResult.Grammar_score,
                VocabularyScore = (decimal?)nlpResult.Vocabulary_score,
                ContentScore = (decimal?)nlpResult.Content_score
            };

            await _unitOfWork.UserAnswersSpeaking.AddAsync(userAnswerSpeaking);
            await _unitOfWork.CompleteAsync();

            Console.WriteLine($"[Speaking] Saved answer to database: UserAnswerSpeakingId={userAnswerSpeaking.UserAnswerSpeakingId}, QuestionId={questionId}, AttemptId={attemptId}");

            // Trả về DTO đầy đủ cho frontend
            return new SpeakingScoringResultDTO
            {
                Transcript = azureResult.Transcript == "." ? "[Không nhận diện được giọng nói]" : azureResult.Transcript,
                SavedAudioUrl = transformedMp3Url,
                OverallScore = Math.Round(overallScore, 1),
                PronunciationScore = azureResult.PronunciationScore,
                AccuracyScore = azureResult.AccuracyScore,
                FluencyScore = azureResult.FluencyScore,
                CompletenessScore = azureResult.CompletenessScore,
                GrammarScore = nlpResult.Grammar_score,
                VocabularyScore = Math.Round(nlpResult.Vocabulary_score, 1),
                ContentScore = nlpResult.Content_score
            };
        }

        private async Task EnsureCloudinaryAssetReady(string url)
        {
            try
            {
                using var client = new HttpClient();
                for (int i = 0; i < 5; i++)
                {
                    var req = new HttpRequestMessage(HttpMethod.Head, url);
                    var resp = await client.SendAsync(req);
                    if ((int)resp.StatusCode == 200)
                    {
                        if (resp.Content.Headers.ContentLength.HasValue && resp.Content.Headers.ContentLength.Value > 2048)
                        {
                            return;
                        }
                    }
                    await Task.Delay(500);
                }
            }
            catch { }
        }
        private float CalculateOverallScore(string questionType, SpeechAnalysisDTO azureResult, NlpResponseDTO nlpResult)
        {
            // Weights dựa trên TOEIC Speaking Rubric chuẩn IIG - UPDATED với Content ưu tiên cao hơn
            float pronWeight, accWeight, fluWeight, gramWeight, vocabWeight, contentWeight;

            // Phân loại theo task type để áp dụng weights phù hợp
            switch (questionType?.ToUpper())
            {
                case "READ_ALOUD": // Q1-2: Đọc to - Trọng tâm pronunciation, fluency, accuracy
                    pronWeight = 0.40f;
                    accWeight = 0.25f;
                    fluWeight = 0.20f;
                    gramWeight = 0.05f;
                    vocabWeight = 0.05f;
                    contentWeight = 0.05f; // Không quan trọng (đọc theo script)
                    break;

                case "DESCRIBE_PICTURE": // Q3: Miêu tả hình - Content QUAN TRỌNG
                    pronWeight = 0.10f;
                    accWeight = 0.08f;
                    fluWeight = 0.12f;
                    gramWeight = 0.20f;
                    vocabWeight = 0.20f;
                    contentWeight = 0.30f; // TĂNG: Miêu tả đúng nội dung là quan trọng nhất
                    break;

                case "RESPOND_QUESTIONS": // Q4-6: Trả lời đúng câu hỏi - Content QUAN TRỌNG
                    pronWeight = 0.10f;
                    accWeight = 0.10f;
                    fluWeight = 0.20f;
                    gramWeight = 0.15f;
                    vocabWeight = 0.10f;
                    contentWeight = 0.35f; // TĂNG: Trả lời đúng câu hỏi là quan trọng nhất
                    break;

                case "RESPOND_WITH_INFO": // Q7-9: Trả lời dựa vào thông tin - Content QUAN TRỌNG
                    pronWeight = 0.08f;
                    accWeight = 0.07f;
                    fluWeight = 0.15f;
                    gramWeight = 0.20f;
                    vocabWeight = 0.15f;
                    contentWeight = 0.35f; // TĂNG: Sử dụng đúng thông tin là quan trọng nhất
                    break;

                case "EXPRESS_OPINION": // Q10-11: Diễn đạt ý kiến - Content, Grammar, Vocabulary
                    pronWeight = 0.08f;
                    accWeight = 0.07f;
                    fluWeight = 0.15f;
                    gramWeight = 0.20f;
                    vocabWeight = 0.20f;
                    contentWeight = 0.30f; // TĂNG: Ý kiến rõ ràng, liên quan
                    break;

                default: // SPEAKING hoặc default - Content ưu tiên cao
                    pronWeight = 0.10f;
                    accWeight = 0.10f;
                    fluWeight = 0.15f;
                    gramWeight = 0.15f;
                    vocabWeight = 0.15f;
                    contentWeight = 0.35f; // TĂNG: Content là quan trọng nhất
                    break;
            }

            double totalScore =
                azureResult.PronunciationScore * pronWeight +
                azureResult.AccuracyScore * accWeight +
                azureResult.FluencyScore * fluWeight +
                nlpResult.Grammar_score * gramWeight +
                nlpResult.Vocabulary_score * vocabWeight +
                nlpResult.Content_score * contentWeight;

            double totalWeight = pronWeight + accWeight + fluWeight + gramWeight + vocabWeight + contentWeight;
            if (totalWeight > 0)
            {
                totalScore /= totalWeight;
            }

            // ⚠️ STRICT RELEVANCE CHECK - Theo ETS Official Rubric
            // ETS: "Responses not related to the question" → Score 0-1 (0-20 điểm trên thang 100)
            // Nguồn: https://www.ets.org/toeic/test-takers/scores/understand/
            // Nguồn: https://www.iibc-global.org/english/toeic/test/sw/guide05/guide05_01/score_descriptor.html
            if (questionType?.ToUpper() != "READ_ALOUD")
            {
                if (nlpResult.Content_score < 15)
                {
                    // ETS Level 0-1: "Response not related to question" or "Cannot be understood"
                    // → HARD CAP at 15 điểm (tương đương TOEIC Speaking score 30/200)
                    double maxAllowedScore = 15.0;
                    if (totalScore > maxAllowedScore)
                    {
                        Console.WriteLine($"[Scoring] 🚫 STRICT RELEVANCE PENALTY: Content={nlpResult.Content_score:F1} < 15 → HARD CAP from {totalScore:F1} to {maxAllowedScore}");
                        Console.WriteLine($"[Scoring] ETS Rubric: 'Response not related to question' → Score 0-1 (Level 1)");
                        totalScore = maxAllowedScore;
                    }
                }
                else if (nlpResult.Content_score < 25)
                {
                    // ETS Level 2: "Barely related to prompts" or "Significantly limited"
                    // → MAX = Content × 1.3 (nghiêm khắc hơn trước)
                    double contentPenaltyFactor = nlpResult.Content_score * 1.3;
                    if (totalScore > contentPenaltyFactor)
                    {
                        Console.WriteLine($"[Scoring] ⚠️ RELEVANCE PENALTY: Content={nlpResult.Content_score:F1} < 25 → Score reduced from {totalScore:F1} to {contentPenaltyFactor:F1}");
                        Console.WriteLine($"[Scoring] ETS Rubric: 'Barely related to prompts' → Low score required");
                        totalScore = contentPenaltyFactor;
                    }
                }
                else if (nlpResult.Content_score < 40)
                {
                    // ETS Level 3-4: "Limited content relevance"
                    // → MAX = Content × 1.5
                    double contentPenaltyFactor = nlpResult.Content_score * 1.5;
                    if (totalScore > contentPenaltyFactor)
                    {
                        Console.WriteLine($"[Scoring] ⚠️ CONTENT PENALTY: Content={nlpResult.Content_score:F1} < 40 → Score reduced from {totalScore:F1} to {contentPenaltyFactor:F1}");
                        totalScore = contentPenaltyFactor;
                    }
                }
                // Content >= 40: Không penalty (đủ liên quan)
            }

            Console.WriteLine($"[Scoring] Task: {questionType}, Weights: P={pronWeight:P0}, A={accWeight:P0}, F={fluWeight:P0}, G={gramWeight:P0}, V={vocabWeight:P0}, C={contentWeight:P0}, Content={nlpResult.Content_score:F1}, Final={totalScore:F1}");

            return (float)Math.Round(totalScore, 1);
        }

        private async Task<NlpResponseDTO> GetNlpScoresAsync(string transcript, string sampleAnswer)
        {
            // (Hàm này giữ nguyên, không cần sửa)
            var client = _httpClientFactory.CreateClient();
            var nlpServiceUrl = _configuration["ServiceUrls:NlpService"];

            if (string.IsNullOrEmpty(nlpServiceUrl))
            {
                throw new Exception("NLP Service URL is not configured in appsettings.json.");
            }

            var request = new NlpRequestDTO
            {
                Transcript = transcript,
                Sample_answer = sampleAnswer
            };

            var response = await client.PostAsJsonAsync($"{nlpServiceUrl}/score_nlp", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get scores from NLP service. Status: {response.StatusCode}, Details: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<NlpResponseDTO>();
        }
    }
}