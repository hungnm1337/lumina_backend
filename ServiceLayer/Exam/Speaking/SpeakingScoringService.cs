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
                    return result; 
                }

                if (attempt < maxRetries - 1)
                {
                    Console.WriteLine($"[Speaking] Azure retry {attempt + 1}/{maxRetries}, waiting {delayMs}ms");
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
            }

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

            await EnsureCloudinaryAssetReady(transformedMp3Url);

            Console.WriteLine($"[Speaking] Using language model: en-GB");


            var azureResult = await RetryAzureRecognitionAsync(transformedMp3Url, question.SampleAnswer, maxRetries: 3);

            bool isTranscriptEmpty = string.IsNullOrWhiteSpace(azureResult.Transcript) || azureResult.Transcript.Trim() == ".";
            
            if (isTranscriptEmpty)
            {
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
                    SubmittedAt = DateTime.UtcNow
                };
            }

            Console.WriteLine($"[Speaking] Transcript result: {azureResult.Transcript}");

            var nlpResult = await GetNlpScoresAsync(azureResult.Transcript, question.SampleAnswer);

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

       
        private async Task<NlpResponseDTO> GetNlpScoresAsync(string transcript, string sampleAnswer)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var nlpServiceUrl = _configuration["ServiceUrls:NlpService"];

                if (string.IsNullOrEmpty(nlpServiceUrl))
                {
                    return GetFallbackNlpScores(transcript, sampleAnswer);
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
                    return GetFallbackNlpScores(transcript, sampleAnswer);
                }

                var result = await response.Content.ReadFromJsonAsync<NlpResponseDTO>();
                return result;
            }
            catch (TaskCanceledException ex)
            {
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (HttpRequestException ex)
            {
                return GetFallbackNlpScores(transcript, sampleAnswer);
            }
            catch (Exception ex)
            {
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