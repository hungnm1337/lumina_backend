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
            int delayMs = 1000;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                await EnsureCloudinaryAssetReady(audioUrl);

                Console.WriteLine($"[Speaking] Azure recognition attempt {attempt + 1}/{maxRetries}");
                result = await _azureSpeechService.AnalyzePronunciationFromUrlAsync(audioUrl, sampleAnswer, "en-GB");

                // Check if we got valid transcript
                if (!string.IsNullOrWhiteSpace(result.Transcript) && result.Transcript != ".")
                {
                    Console.WriteLine($"[Speaking] Azure recognition successful on attempt {attempt + 1}");
                    return result;
                }

                // Check if error indicates we should NOT retry
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    if (result.ErrorMessage.Contains("Invalid audio") || result.ErrorMessage.Contains("Invalid data"))
                    {
                        Console.WriteLine($"[Speaking] Invalid audio detected - stopping retries");
                        return result;
                    }

                    // EARLY EXIT: If Azure explicitly says "no result", don't waste time retrying
                    if (result.ErrorMessage.Contains("Recognition failed - no result"))
                    {
                        Console.WriteLine($"[Speaking] EARLY STOP: Azure detected unrecognizable audio (empty segments) - skipping remaining {maxRetries - attempt - 1} retries");
                        return result;
                    }
                }

                if (attempt < maxRetries - 1)
                {
                    Console.WriteLine($"[Speaking] Empty transcript on attempt {attempt + 1}, retrying in {delayMs}ms");
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 5000);
                }
            }

            Console.WriteLine($"[Speaking] All {maxRetries} Azure recognition attempts failed");
            return result;
        }
        public async Task<SpeakingScoringResultDTO> ProcessAndScoreAnswerAsync(IFormFile audioFile, int questionId, int attemptId)
        {
            var uploadResult = await _uploadService.UploadFileAsync(audioFile);
            var question = await _unitOfWork.Questions.GetAsync(
                q => q.QuestionId == questionId,
                includeProperties: "Part"
            );
            if (question == null || string.IsNullOrEmpty(question.SampleAnswer))
            {
                throw new Exception($"Question with ID {questionId} or its sample answer not found.");
            }

            string partCode = question.Part?.PartCode ?? "";

            var cloudName = _configuration["CloudinarySettings:CloudName"];
            var publicId = uploadResult.PublicId;
            // Force 16kHz sample rate for better ASR
            var transformedMp3Url = $"https://res.cloudinary.com/{cloudName}/video/upload/f_mp3,ar_16000/{publicId}.mp3";
            Console.WriteLine($"[Speaking] MP3 URL for Azure: {transformedMp3Url}");

            // Enhanced Cloudinary readiness check
            await EnsureCloudinaryAssetReady(transformedMp3Url);

            Console.WriteLine($"[Speaking] Using language model: en-GB");

            // OPTIMIZATION: Start Azure recognition and NLP scoring in parallel
            // Azure takes 15-25s, we can start preparing NLP while Azure is running
            var azureTask = RetryAzureRecognitionAsync(transformedMp3Url, question.SampleAnswer, maxRetries: 5);

            // Wait for Azure to complete
            var azureResult = await azureTask;

            bool isTranscriptEmpty = string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".";

            if (isTranscriptEmpty)
            {
                Console.WriteLine($"[Speaking] Empty transcript after {5} retries - returning zero score");
                var zeroAnswer = new UserAnswerSpeaking
                {
                    AttemptID = attemptId,
                    QuestionId = questionId,
                    Transcript = "[Không nhận diện được giọng nói]",
                    AudioUrl = transformedMp3Url,
                    PronunciationScore = 0,
                    AccuracyScore = 0,
                    FluencyScore = 0,
                    CompletenessScore = 0,
                    GrammarScore = 0,
                    VocabularyScore = 0,
                    ContentScore = 0,
                    OverallScore = 0
                };

                await _unitOfWork.UserAnswersSpeaking.AddAsync(zeroAnswer);
                await _unitOfWork.CompleteAsync();

                return new SpeakingScoringResultDTO
                {
                    QuestionId = questionId,
                    Transcript = "[Không nhận diện được giọng nói]",
                    SavedAudioUrl = transformedMp3Url,
                    AudioUrl = transformedMp3Url,
                    OverallScore = 0,
                    PronunciationScore = 0,
                    AccuracyScore = 0,
                    FluencyScore = 0,
                    CompletenessScore = 0,
                    GrammarScore = 0,
                    VocabularyScore = 0,
                    ContentScore = 0,
                    SubmittedAt = DateTime.UtcNow,
                    SampleAnswer = question.SampleAnswer
                };
            }

            Console.WriteLine($"[Speaking] Transcript result: {azureResult.Transcript}");

            // Now get NLP scores (already optimized with increased timeout)
            var nlpResult = await GetNlpScoresAsync(azureResult.Transcript, question.SampleAnswer, question.StemText, partCode);

            // Calculate actual text coverage for Part 1
            double actualCoveragePercent = 100.0;

            if (partCode?.ToUpper() == "SPEAKING_PART_1")
            {
                var transcriptWords = azureResult.Transcript?
                    .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Length ?? 0;

                var referenceWords = question.SampleAnswer
                    .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Length;

                if (referenceWords > 0)
                {
                    actualCoveragePercent = (double)transcriptWords / referenceWords * 100.0;
                }

                Console.WriteLine(
                    $"[Speaking] Part 1 Coverage: {transcriptWords}/{referenceWords} words = {actualCoveragePercent:F1}%");
            }

            var weights = _scoringWeightService.GetWeightsForPart(partCode);
            var overallScore = _scoringWeightService.CalculateOverallScore(
                weights,
                azureResult.PronunciationScore,
                azureResult.AccuracyScore,
                azureResult.FluencyScore,
                nlpResult.Grammar_score,
                nlpResult.Vocabulary_score,
                nlpResult.Content_score,
                actualCoveragePercent
            );

            Console.WriteLine($"[Speaking] OverallScore calculated: {overallScore:F1} for PartCode={partCode}");

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
                OverallScore = (decimal?)overallScore
            };

            await _unitOfWork.UserAnswersSpeaking.AddAsync(userAnswerSpeaking);
            await _unitOfWork.CompleteAsync();

            Console.WriteLine($"[Speaking] Saved answer to database: UserAnswerSpeakingId={userAnswerSpeaking.UserAnswerSpeakingId}, QuestionId={questionId}, AttemptId={attemptId}");

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
                ContentScore = nlpResult.Content_score,
                SampleAnswer = question.SampleAnswer
            };
        }

        /// <summary>
        /// ENHANCED: Cloudinary asset readiness verification with exponential backoff
        /// Ensures asset is fully processed before Azure Speech attempts to download it
        /// </summary>
        private async Task EnsureCloudinaryAssetReady(string url)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // 5s timeout per request

                int delayMs = 500;
                const int maxRetries = 15; // Up to 15 retries

                for (int i = 0; i < maxRetries; i++)
                {
                    var req = new HttpRequestMessage(HttpMethod.Head, url);
                    var resp = await client.SendAsync(req);

                    if ((int)resp.StatusCode == 200)
                    {
                        if (resp.Content.Headers.ContentLength.HasValue && resp.Content.Headers.ContentLength.Value > 2048)
                        {
                            Console.WriteLine($"[Speaking] Cloudinary asset ready after {i + 1} attempts: {resp.Content.Headers.ContentLength.Value} bytes");
                            return;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Speaking] Asset not ready (HTTP {resp.StatusCode}), retry {i + 1}/{maxRetries}");
                    }

                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs = Math.Min(delayMs + 500, 2000); // Gradual increase, max 2s
                    }
                }

                Console.WriteLine($"[Speaking] WARNING: Cloudinary asset verification timeout after {maxRetries} attempts - proceeding anyway");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Speaking] Cloudinary verification error: {ex.Message} - proceeding anyway");
            }
        }


        [Obsolete("Use ScoringWeightService.CalculateOverallScore() instead", false)]
        private float CalculateOverallScore(string partCode, string questionType, SpeechAnalysisDTO azureResult, NlpResponseDTO nlpResult)
        {
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


        private async Task<NlpResponseDTO> GetNlpScoresAsync(string transcript, string sampleAnswer, string questionText, string partCode = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(60); // Increased from 30s to 60s
                var nlpServiceUrl = _configuration["ServiceUrls:NlpService"];

                if (string.IsNullOrEmpty(nlpServiceUrl))
                {
                    Console.WriteLine("[NLP] Service URL not configured - using fallback scoring");
                    return GetFallbackNlpScores(transcript, sampleAnswer);
                }

                var request = new NlpRequestDTO
                {
                    Transcript = transcript,
                    Sample_answer = sampleAnswer,
                    Question = questionText,
                    Part_code = partCode
                };

                Console.WriteLine($"[NLP] Sending request to: {nlpServiceUrl}/score_nlp");
                var response = await client.PostAsJsonAsync($"{nlpServiceUrl}/score_nlp", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[NLP] Service error (HTTP {response.StatusCode}): {errorContent} - using fallback");
                    return GetFallbackNlpScores(transcript, sampleAnswer);
                }

                var result = await response.Content.ReadFromJsonAsync<NlpResponseDTO>();
                Console.WriteLine($"[NLP] Scores received - Grammar: {result.Grammar_score:F1}, Vocab: {result.Vocabulary_score:F1}, Content: {result.Content_score:F1}");
                return result;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[NLP] Request timeout after 60s - using fallback scoring");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[NLP] HTTP request failed: {ex.Message} - using fallback scoring");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NLP] Unexpected error: {ex.Message} - using fallback scoring");
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
        }

        private NlpResponseDTO GetFallbackNlpScores(string transcript, string sampleAnswer)
        {
            if (string.IsNullOrWhiteSpace(transcript) || transcript.Trim() == ".")
            {
                Console.WriteLine("[NLP] Fallback: Empty transcript - returning zero scores");
                return new NlpResponseDTO
                {
                    Grammar_score = 0f,
                    Vocabulary_score = 0f,
                    Content_score = 0f
                };
            }

            float grammarScore = 50f;
            float vocabularyScore = 50f;
            float contentScore = 50f;

            int transcriptLength = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            int sampleLength = string.IsNullOrWhiteSpace(sampleAnswer) ? 20 : sampleAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            float lengthRatio = (float)transcriptLength / Math.Max(sampleLength, 1);
            contentScore = Math.Min(lengthRatio * 60f, 75f);

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

            vocabularyScore = grammarScore;

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