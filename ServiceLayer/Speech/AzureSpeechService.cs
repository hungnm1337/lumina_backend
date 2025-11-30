using DataLayer.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Options;
using ServiceLayer.Configs;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using NAudio.Wave;
using DataLayer.DTOs.Exam.Speaking;

namespace ServiceLayer.Speech
{
    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly SpeechConfig _speechConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public AzureSpeechService(IOptions<AzureSpeechSettings> config, IHttpClientFactory httpClientFactory)
        {
            _speechConfig = SpeechConfig.FromSubscription(config.Value.SubscriptionKey, config.Value.Region);
            _speechConfig.SpeechRecognitionLanguage = "en-US";
            _speechConfig.OutputFormat = OutputFormat.Detailed;
            _speechConfig.SetProperty("SpeechServiceConnection_InitialSilenceTimeoutMs", "1500");
            _speechConfig.SetProperty("SpeechServiceConnection_EndSilenceTimeoutMs", "3000");
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SpeechAnalysisDTO> AnalyzePronunciationAsync(IFormFile audioFile, string referenceText)
        {
            var mp3Format = AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MP3);
            using var audioInputStream = AudioInputStream.CreatePushStream(mp3Format);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            byte[] buffer = new byte[1024];
            int bytesRead;
            using (var stream = audioFile.OpenReadStream())
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    audioInputStream.Write(buffer, bytesRead);
                }
            }
            audioInputStream.Close();

            var pronunciationConfig = new PronunciationAssessmentConfig(
                NormalizeReferenceText(referenceText),
                GradingSystem.HundredMark,
                Granularity.Word,
                false);
            pronunciationConfig.ApplyTo(recognizer);

            var result = await recognizer.RecognizeOnceAsync();
            Console.WriteLine($"[AzureSpeech] ResultReason: {result.Reason}");
            try
            {
                var detailedJson = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                if (!string.IsNullOrWhiteSpace(detailedJson))
                {
                    Console.WriteLine($"[AzureSpeech] Detailed JSON: {detailedJson}");
                }
            }
            catch { }
            if (result.Reason != ResultReason.RecognizedSpeech)
            {
                var cd = CancellationDetails.FromResult(result);
                Console.WriteLine($"[AzureSpeech] Cancellation: {cd.Reason} - {cd.ErrorCode} - {cd.ErrorDetails}");
            }

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                var pronunciationResult = PronunciationAssessmentResult.FromResult(result);
                return new SpeechAnalysisDTO
                {
                    Transcript = result.Text,
                    AccuracyScore = pronunciationResult.AccuracyScore,
                    FluencyScore = pronunciationResult.FluencyScore,
                    CompletenessScore = pronunciationResult.CompletenessScore,
                    PronunciationScore = pronunciationResult.PronunciationScore
                };
            }
            else
            {
                var cancellationDetails = CancellationDetails.FromResult(result);
                string errorMessage = $"Reason: {result.Reason}. Details: {cancellationDetails.ErrorDetails}";
                return new SpeechAnalysisDTO { ErrorMessage = errorMessage };
            }
        }


        public async Task<SpeechAnalysisDTO> AnalyzePronunciationFromUrlAsync(string audioUrl, string referenceText, string language = null)
        {
            try
            {
                var http = _httpClientFactory.CreateClient();
                using var response = await http.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return new SpeechAnalysisDTO { ErrorMessage = $"Failed to fetch audio from URL. Status: {response.StatusCode}" };
                }

                Console.WriteLine($"[AzureSpeech] Downloading audio from URL: {audioUrl}");
                var mp3Bytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"[AzureSpeech] Audio downloaded: {mp3Bytes.Length} bytes");

                var effectiveLanguage = string.IsNullOrWhiteSpace(language) ? "en-US (default)" : language;
                Console.WriteLine($"[AzureSpeech] Using language: {effectiveLanguage}");

                string finalTranscript;
                using (var networkStream = new System.IO.MemoryStream(mp3Bytes, writable: false))
                {
                    finalTranscript = await PerformContinuousRecognition(networkStream, language);
                }

                if (string.IsNullOrWhiteSpace(finalTranscript))
                {
                    return new SpeechAnalysisDTO { ErrorMessage = "No speech recognized" };
                }

                using (var networkStream = new System.IO.MemoryStream(mp3Bytes, writable: false))
                {
                    var scores = await PerformPronunciationAssessment(networkStream, finalTranscript, language);

                    if (scores != null)
                    {
                        Console.WriteLine($"[AzureSpeech] Pass 2 completed. Scores: P={scores.PronunciationScore:F1}, A={scores.AccuracyScore:F1}, F={scores.FluencyScore:F1}, C={scores.CompletenessScore:F1}");
                        return new SpeechAnalysisDTO
                        {
                            Transcript = finalTranscript,
                            PronunciationScore = scores.PronunciationScore,
                            AccuracyScore = scores.AccuracyScore,
                            FluencyScore = scores.FluencyScore,
                            CompletenessScore = scores.CompletenessScore
                        };
                    }
                    else
                    {
                        Console.WriteLine($"[AzureSpeech] Pass 2 failed. Using default scores.");
                        return new SpeechAnalysisDTO
                        {
                            Transcript = finalTranscript,
                            PronunciationScore = 70,
                            AccuracyScore = 70,
                            FluencyScore = 70,
                            CompletenessScore = 70
                        };
                    }
                }
            }
            catch (System.IO.InvalidDataException ex)
            {
                return new SpeechAnalysisDTO { ErrorMessage = $"Invalid audio file: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new SpeechAnalysisDTO { ErrorMessage = $"Error processing audio: {ex.Message}" };
            }
        }

        private async Task<string> PerformContinuousRecognition(System.IO.MemoryStream mp3Stream, string language = null)
        {
            using var mp3Reader = new Mp3FileReader(mp3Stream);
            var pcm16kMonoFormat = new WaveFormat(16000, 16, 1);
            using var resampler = new MediaFoundationResampler(mp3Reader, pcm16kMonoFormat) { ResamplerQuality = 60 };

            var wavFormat = AudioStreamFormat.GetWaveFormatPCM((uint)pcm16kMonoFormat.SampleRate, (byte)pcm16kMonoFormat.BitsPerSample, (byte)pcm16kMonoFormat.Channels);
            using var audioInputStream = AudioInputStream.CreatePushStream(wavFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = string.IsNullOrWhiteSpace(language)
                ? new SpeechRecognizer(_speechConfig, audioConfig)
                : new SpeechRecognizer(_speechConfig, language, audioConfig);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                audioInputStream.Write(buffer, bytesRead);
            }
            audioInputStream.Close();

            var tcs = new TaskCompletionSource<string>();
            var fullTranscript = new System.Text.StringBuilder();

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    fullTranscript.Append(e.Result.Text);
                    fullTranscript.Append(" ");
                    Console.WriteLine($"[AzureSpeech] Pass 1 - Recognized chunk: {e.Result.Text}");
                }
            };

            recognizer.SessionStopped += (s, e) =>
            {
                tcs.TrySetResult(fullTranscript.ToString().Trim());
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"[AzureSpeech] Pass 1 - Recognition canceled: {e.Reason}");
                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"[AzureSpeech] Pass 1 - Error: {e.ErrorDetails}");
                }
                tcs.TrySetResult(fullTranscript.ToString().Trim());
            };

            await recognizer.StartContinuousRecognitionAsync();
            await Task.Delay(100); // Small delay to ensure recognition starts

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            await recognizer.StopContinuousRecognitionAsync();

            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            else
            {
                Console.WriteLine($"[AzureSpeech] Pass 1 - Timeout. Using partial transcript.");
                return fullTranscript.ToString().Trim();
            }
        }

        private async Task<PronunciationAssessmentResult> PerformPronunciationAssessment(System.IO.MemoryStream mp3Stream, string referenceText, string language = null)
        {
            try
            {
                using var mp3Reader = new Mp3FileReader(mp3Stream);
                var pcm16kMonoFormat = new WaveFormat(16000, 16, 1);
                using var resampler = new MediaFoundationResampler(mp3Reader, pcm16kMonoFormat) { ResamplerQuality = 60 };

                var wavFormat = AudioStreamFormat.GetWaveFormatPCM((uint)pcm16kMonoFormat.SampleRate, (byte)pcm16kMonoFormat.BitsPerSample, (byte)pcm16kMonoFormat.Channels);
                using var audioInputStream = AudioInputStream.CreatePushStream(wavFormat);
                using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
                using var recognizer = string.IsNullOrWhiteSpace(language)
                    ? new SpeechRecognizer(_speechConfig, audioConfig)
                    : new SpeechRecognizer(_speechConfig, language, audioConfig);

                var pronunciationConfig = new PronunciationAssessmentConfig(
                    referenceText: referenceText,
                    gradingSystem: GradingSystem.HundredMark,
                    granularity: Granularity.Phoneme,
                    enableMiscue: false);
                pronunciationConfig.ApplyTo(recognizer);

                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    audioInputStream.Write(buffer, bytesRead);
                }
                audioInputStream.Close();

                var result = await recognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    return PronunciationAssessmentResult.FromResult(result);
                }
                else
                {
                    Console.WriteLine($"[AzureSpeech] Pass 2 - Recognition failed: {result.Reason}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AzureSpeech] Pass 2 - Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<string> RecognizeFromUrlAsync(string audioUrl, string language = null)
        {
            var http = _httpClientFactory.CreateClient();
            using var response = await http.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var mp3Bytes = await response.Content.ReadAsByteArrayAsync();
            using var networkStream = new System.IO.MemoryStream(mp3Bytes, writable: false);
            using var mp3Reader = new NAudio.Wave.Mp3FileReader(networkStream);
            var pcm16kMonoFormat = new NAudio.Wave.WaveFormat(16000, 16, 1);
            using var resampler = new NAudio.Wave.MediaFoundationResampler(mp3Reader, pcm16kMonoFormat) { ResamplerQuality = 60 };

            var wavFormat = AudioStreamFormat.GetWaveFormatPCM((uint)pcm16kMonoFormat.SampleRate, (byte)pcm16kMonoFormat.BitsPerSample, (byte)pcm16kMonoFormat.Channels);
            using var audioInputStream = AudioInputStream.CreatePushStream(wavFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = string.IsNullOrWhiteSpace(language)
                ? new SpeechRecognizer(_speechConfig, audioConfig)
                : new SpeechRecognizer(_speechConfig, language, audioConfig);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                audioInputStream.Write(buffer, bytesRead);
            }
            audioInputStream.Close();

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text ?? string.Empty;
        }

        public async Task<string> RecognizeFromFileAsync(IFormFile audioFile, string language = null)
        {
            var mp3Format = AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MP3);
            using var audioInputStream = AudioInputStream.CreatePushStream(mp3Format);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = string.IsNullOrWhiteSpace(language)
                ? new SpeechRecognizer(_speechConfig, audioConfig)
                : new SpeechRecognizer(_speechConfig, language, audioConfig);

            byte[] buffer = new byte[1024];
            int bytesRead;
            using (var stream = audioFile.OpenReadStream())
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    audioInputStream.Write(buffer, bytesRead);
                }
            }
            audioInputStream.Close();

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text ?? string.Empty;
        }
        private static string NormalizeReferenceText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var lower = text.ToLowerInvariant().Trim();
            // Remove punctuation that can confuse alignment
            var chars = lower.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsPunctuation(chars[i]))
                {
                    chars[i] = ' ';
                }
            }
            var normalized = new string(chars);
            // Collapse multiple spaces
            return System.Text.RegularExpressions.Regex.Replace(normalized, "\\s+", " ").Trim();
        }

    }
}