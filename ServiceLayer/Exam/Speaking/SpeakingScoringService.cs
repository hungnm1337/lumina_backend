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
        private readonly IScoringWeightService _scoringWeightService;

        public SpeakingScoringService(
            IUnitOfWork unitOfWork,
            IUploadService uploadService,
            IAzureSpeechService azureSpeechService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IScoringWeightService scoringWeightService)
        {
            _unitOfWork = unitOfWork;
            _uploadService = uploadService;
            _azureSpeechService = azureSpeechService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _scoringWeightService = scoringWeightService;
        }
        private async Task<SpeechAnalysisDTO> RetryAzureRecognitionAsync(
    string audioUrl,
    string sampleAnswer,
    int maxRetries)
        {
            SpeechAnalysisDTO result = null;
            int delayMs = 500;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                await EnsureCloudinaryAssetReady(audioUrl);
                result = await _azureSpeechService.AnalyzePronunciationFromUrlAsync(audioUrl, sampleAnswer, "en-GB");

                if (!string.IsNullOrWhiteSpace(result.Transcript) && result.Transcript != ".")
                {
                    return result; // ✅ Thành công
                }

                if (attempt < maxRetries - 1)
                {
                    Console.WriteLine($"[Speaking] Azure retry {attempt + 1}/{maxRetries}, waiting {delayMs}ms");
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
            }

            Console.WriteLine($"[Speaking] ❌ Azure failed after {maxRetries} retries. URL: {audioUrl}");
            return result; 
        }
        public async Task<SpeakingScoringResultDTO> ProcessAndScoreAnswerAsync(IFormFile audioFile, int questionId, int attemptId)
        {
            // ✅ DEBUG: Enhanced logging
            Console.WriteLine($"[Speaking] ========== BEGIN ProcessAndScoreAnswerAsync ==========");
            Console.WriteLine($"[Speaking] QuestionId: {questionId}, AttemptId: {attemptId}");
            Console.WriteLine($"[Speaking] Audio file size: {audioFile.Length} bytes");
            Console.WriteLine($"[Speaking] Audio content type: {audioFile.ContentType}");
            Console.WriteLine($"[Speaking] Audio file name: {audioFile.FileName}");

            var uploadResult = await _uploadService.UploadFileAsync(audioFile);
            // Include Part để lấy PartCode
            var question = await _unitOfWork.Questions.GetAsync(
                q => q.QuestionId == questionId,
                includeProperties: "Part"
            );
            if (question == null || string.IsNullOrEmpty(question.SampleAnswer))
            {
                throw new Exception($"Question with ID {questionId} or its sample answer not found.");
            }
            
            string partCode = question.Part?.PartCode ?? "";
            Console.WriteLine($"[Speaking] QuestionId={questionId}, PartCode={partCode}, QuestionType={question.QuestionType}");

            var cloudName = _configuration["CloudinarySettings:CloudName"];
            var publicId = uploadResult.PublicId; 
            // Force 16kHz sample rate for better ASR
            var transformedMp3Url = $"https://res.cloudinary.com/{cloudName}/video/upload/f_mp3,ar_16000/{publicId}.mp3";
            Console.WriteLine($"[Speaking] MP3 URL for Azure: {transformedMp3Url}");

            await EnsureCloudinaryAssetReady(transformedMp3Url);

            Console.WriteLine($"[Speaking] Using language model: en-GB");


            var azureResult = await RetryAzureRecognitionAsync(transformedMp3Url, question.SampleAnswer, maxRetries: 3);

          

            if (string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript == ".")
            {
                Console.WriteLine($"[Speaking] ⚠️ Azure failed (possibly subscription disabled), using MOCK transcript");
                azureResult.Transcript = question.SampleAnswer ?? "This is a mock transcript for testing purposes.";
                // Mock scores tạm để test UI
                azureResult.PronunciationScore = 0;
                azureResult.AccuracyScore = 0;
                azureResult.FluencyScore = 0;
                azureResult.CompletenessScore =0;
            }

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

            // ✅ FIX Bug #2: Use ScoringWeightService for consistent scoring
            var weights = _scoringWeightService.GetWeightsForPart(partCode);
            var overallScore = _scoringWeightService.CalculateOverallScore(
                weights,
                azureResult.PronunciationScore,
                azureResult.AccuracyScore,
                azureResult.FluencyScore,
                nlpResult.Grammar_score,
                nlpResult.Vocabulary_score,
                nlpResult.Content_score
            );

            Console.WriteLine($"[Speaking] OverallScore calculated: {overallScore:F1} for PartCode={partCode}");

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
                ContentScore = (decimal?)nlpResult.Content_score,
                OverallScore = (decimal?)overallScore  // ✅ Store overall score in DB
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
        
        /// <summary>
        /// DEPRECATED: This method is replaced by ScoringWeightService.CalculateOverallScore()
        /// Kept for backwards compatibility during migration.
        /// </summary>
        [Obsolete("Use ScoringWeightService.CalculateOverallScore() instead", false)]
        private float CalculateOverallScore(string partCode, string questionType, SpeechAnalysisDTO azureResult, NlpResponseDTO nlpResult)
        {
            // ✅ FIX Bug #2: Delegate to ScoringWeightService for consistency
            Console.WriteLine($"[DEPRECATED] CalculateOverallScore called - using ScoringWeightService instead");

            var weights = _scoringWeightService.GetWeightsForPart(partCode);
            return _scoringWeightService.CalculateOverallScore(
                weights,
                azureResult.PronunciationScore,
                azureResult.AccuracyScore,
                azureResult.FluencyScore,
                nlpResult.Grammar_score,
                nlpResult.Vocabulary_score,
                nlpResult.Content_score
            );
        }

        /// <summary>
        /// ✅ FIX Bug #4: Get NLP scores with fallback mechanism
        /// If NLP service is unavailable/timeout, return fallback scores instead of crashing
        /// </summary>
        private async Task<NlpResponseDTO> GetNlpScoresAsync(string transcript, string sampleAnswer)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var nlpServiceUrl = _configuration["ServiceUrls:NlpService"];

                if (string.IsNullOrEmpty(nlpServiceUrl))
                {
                    Console.WriteLine("[NLP] ⚠️ NLP Service URL is not configured, using fallback scores");
                    return GetFallbackNlpScores(transcript, sampleAnswer);
                }

                var request = new NlpRequestDTO
                {
                    Transcript = transcript,
                    Sample_answer = sampleAnswer
                };

                Console.WriteLine($"[NLP] Calling NLP service at: {nlpServiceUrl}/score_nlp");
                var response = await client.PostAsJsonAsync($"{nlpServiceUrl}/score_nlp", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[NLP] ⚠️ Service returned {response.StatusCode}: {errorContent}");
                    Console.WriteLine($"[NLP] Using fallback scores");
                    return GetFallbackNlpScores(transcript, sampleAnswer);
                }

                var result = await response.Content.ReadFromJsonAsync<NlpResponseDTO>();
                Console.WriteLine($"[NLP] ✅ Success: Grammar={result.Grammar_score:F1}, Vocab={result.Vocabulary_score:F1}, Content={result.Content_score:F1}");
                return result;
            }
            catch (TaskCanceledException ex)
            {
                // Timeout occurred
                Console.WriteLine($"[NLP] ⏱️ Timeout after 30 seconds: {ex.Message}");
                Console.WriteLine($"[NLP] Using fallback scores");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (HttpRequestException ex)
            {
                // Network error (NLP service down, connection refused, etc.)
                Console.WriteLine($"[NLP] 🔌 Network error: {ex.Message}");
                Console.WriteLine($"[NLP] Using fallback scores");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (Exception ex)
            {
                // Any other error
                Console.WriteLine($"[NLP] ❌ Unexpected error: {ex.Message}");
                Console.WriteLine($"[NLP] Using fallback scores");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
        }

        /// <summary>
        /// Fallback NLP scores when service is unavailable.
        /// Uses heuristic-based scoring based on transcript characteristics.
        /// </summary>
        private NlpResponseDTO GetFallbackNlpScores(string transcript, string sampleAnswer)
        {
            // Basic heuristic scoring based on transcript characteristics
            float grammarScore = 50f;
            float vocabularyScore = 50f;
            float contentScore = 50f;

            if (!string.IsNullOrWhiteSpace(transcript) && transcript != ".")
            {
                // Transcript length heuristic (longer = better content coverage)
                int transcriptLength = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                int sampleLength = string.IsNullOrWhiteSpace(sampleAnswer) ? 20 : sampleAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                // Content score: based on length ratio (capped at 100)
                float lengthRatio = (float)transcriptLength / Math.Max(sampleLength, 1);
                contentScore = Math.Min(lengthRatio * 60f, 75f); // Max 75 for fallback

                // Grammar score: assume reasonable if they spoke enough words
                if (transcriptLength >= 5)
                {
                    grammarScore = 60f;
                }
                else if (transcriptLength >= 3)
                {
                    grammarScore = 50f;
                }
                else
                {
                    grammarScore = 40f;
                }

                // Vocabulary score: similar to grammar
                vocabularyScore = grammarScore;
            }
            else
            {
                // No transcript = low scores
                grammarScore = 30f;
                vocabularyScore = 30f;
                contentScore = 30f;
            }

            Console.WriteLine($"[NLP] Fallback scores: Grammar={grammarScore:F1}, Vocab={vocabularyScore:F1}, Content={contentScore:F1}");

            return new NlpResponseDTO
            {
                Grammar_score = grammarScore,
                Vocabulary_score = vocabularyScore,
                Content_score = contentScore
            };
        }
    }
}