using DataLayer.DTOs;
using DataLayer.DTOs.Exam;
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

namespace ServiceLayer.Speaking
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
            var nlpResult = await GetNlpScoresAsync(azureResult.Transcript, question.SampleAnswer);
            var overallScore = CalculateOverallScore(question.QuestionType, azureResult, nlpResult);

            var userAnswer = new UserAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                AnswerContent = azureResult.Transcript,
                AudioUrl = transformedMp3Url,
                Score = overallScore
            };

            // Sử dụng đúng tên thuộc tính 'UserAnswers' và hàm
            await _unitOfWork.UserAnswers.AddAsync(userAnswer);
            await _unitOfWork.CompleteAsync(); // Sử dụng SaveChangesAsync

            var speakingResult = new SpeakingResult
            {
                UserAnswerId = userAnswer.UserAnswerId,
                PronunciationScore = (float?)azureResult.PronunciationScore,
                AccuracyScore = (float?)azureResult.AccuracyScore,
                FluencyScore = (float?)azureResult.FluencyScore,
                CompletenessScore = (float?)azureResult.CompletenessScore,
                GrammarScore = (float?)nlpResult.Grammar_score,
                VocabularyScore = (float?)nlpResult.Vocabulary_score,
                ContentScore = (float?)nlpResult.Content_score
            };

            await _unitOfWork.SpeakingResults.AddAsync(speakingResult);
            await _unitOfWork.CompleteAsync(); // Sử dụng SaveChangesAsync

            // Trả về DTO đầy đủ cho frontend
            return new SpeakingScoringResultDTO
            {
                Transcript = azureResult.Transcript,
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
            // (Hàm này giữ nguyên, không cần sửa)
            float pronWeight = 0.25f;
            float accWeight = 0.15f;
            float fluWeight = 0.20f;
            float contWeight = 0.20f;
            float gramWeight = 0.10f;
            float vocabWeight = 0.10f;

            double totalScore =
                (azureResult.PronunciationScore * pronWeight) +
                (azureResult.AccuracyScore * accWeight) +
                (azureResult.FluencyScore * fluWeight) +
                (nlpResult.Content_score * contWeight) +
                (nlpResult.Grammar_score * gramWeight) +
                (nlpResult.Vocabulary_score * vocabWeight);

            double totalWeight = pronWeight + accWeight + fluWeight + contWeight + gramWeight + vocabWeight;
            if (totalWeight > 0)
            {
                totalScore /= totalWeight;
            }

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