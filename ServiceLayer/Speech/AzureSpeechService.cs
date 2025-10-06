
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
using DataLayer.DTOs.Exam;

namespace ServiceLayer.Speech
{
    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly SpeechConfig _speechConfig;

        public AzureSpeechService(IOptions<AzureSpeechSettings> config)
        {
            _speechConfig = SpeechConfig.FromSubscription(config.Value.SubscriptionKey, config.Value.Region);
            _speechConfig.SpeechRecognitionLanguage = "en-US";
            // Provide richer recognition results for debugging and assessment alignment
            _speechConfig.OutputFormat = OutputFormat.Detailed;
            // Make endpointing less aggressive to avoid chopping short phrases into fragments
            _speechConfig.SetProperty("SpeechServiceConnection_InitialSilenceTimeoutMs", "1500");
            _speechConfig.SetProperty("SpeechServiceConnection_EndSilenceTimeoutMs", "3000");
        }

        public async Task<SpeechAnalysisDTO> AnalyzePronunciationAsync(IFormFile audioFile, string referenceText)
        {
            // Use MP3 compressed format (Cloudinary transforms to MP3 for Azure)
            var mp3Format = AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MP3);
            using var audioInputStream = AudioInputStream.CreatePushStream(mp3Format);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            // 2. Đọc file từ IFormFile và ghi vào PushAudioInputStream bằng phương thức Write()
            byte[] buffer = new byte[1024];
            int bytesRead;
            using (var stream = audioFile.OpenReadStream())
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    audioInputStream.Write(buffer, bytesRead);
                }
            }
            // 3. Báo cho SDK biết là đã hết dữ liệu
            audioInputStream.Close();

            // Cấu hình Pronunciation Assessment (giữ nguyên)
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
                // Thêm thông tin chi tiết về lỗi nếu có
                var cancellationDetails = CancellationDetails.FromResult(result);
                string errorMessage = $"Reason: {result.Reason}. Details: {cancellationDetails.ErrorDetails}";
                return new SpeechAnalysisDTO { ErrorMessage = errorMessage };
            }
        }

        public async Task<SpeechAnalysisDTO> AnalyzePronunciationFromUrlAsync(string audioUrl, string referenceText, string language = null)
        {
            using var http = new HttpClient();
            using var response = await http.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return new SpeechAnalysisDTO { ErrorMessage = $"Failed to fetch audio from URL. Status: {response.StatusCode}" };
            }

            // Download to memory to get a seekable stream for NAudio
            var mp3Bytes = await response.Content.ReadAsByteArrayAsync();
            using var networkStream = new System.IO.MemoryStream(mp3Bytes, writable: false);
            // Decode MP3 to 16kHz mono PCM using NAudio, then send PCM to Azure
            using var mp3Reader = new Mp3FileReader(networkStream);
            var pcm16kMonoFormat = new WaveFormat(16000, 16, 1);
            using var resampler = new MediaFoundationResampler(mp3Reader, pcm16kMonoFormat) { ResamplerQuality = 60 };

            // Azure expects PCM (RIFF/WAV) content; use WAV container in the push stream
            var wavFormat = AudioStreamFormat.GetWaveFormatPCM((uint)pcm16kMonoFormat.SampleRate, (byte)pcm16kMonoFormat.BitsPerSample, (byte)pcm16kMonoFormat.Channels);
            using var audioInputStream = AudioInputStream.CreatePushStream(wavFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            var effectiveLanguage = string.IsNullOrWhiteSpace(language) ? "en-US (default)" : language;
            Console.WriteLine($"[AzureSpeech] AnalyzePronunciation using language: {effectiveLanguage}");
            Console.WriteLine($"[AzureSpeech] Reference text from DB: {referenceText}");
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

            // TEMPORARY FIX: Use simple recognition WITHOUT reference text alignment
            // This allows us to see what the user actually said
            // TODO: Later, use pronunciation assessment with user's actual transcript as reference
            var result = await recognizer.RecognizeOnceAsync();
            Console.WriteLine($"[AzureSpeech] Actual transcript (no reference): {result.Text}");
            try
            {
                var detailedJson = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                if (!string.IsNullOrWhiteSpace(detailedJson))
                {
                    Console.WriteLine($"[AzureSpeech] Detailed JSON: {detailedJson}");
                }
            }
            catch { }
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                // TEMPORARY: Return transcript without pronunciation scores since we're not using assessment
                // This allows accurate transcript recognition
                return new SpeechAnalysisDTO
                {
                    Transcript = result.Text,
                    AccuracyScore = 70,  // Placeholder scores for now
                    FluencyScore = 70,
                    CompletenessScore = 70,
                    PronunciationScore = 70
                };
            }
            else
            {
                var cancellationDetails = CancellationDetails.FromResult(result);
                string errorMessage = $"Reason: {result.Reason}. Details: {cancellationDetails.ErrorDetails}";
                return new SpeechAnalysisDTO { ErrorMessage = errorMessage };
            }
        }

        public async Task<string> RecognizeFromUrlAsync(string audioUrl, string language = null)
        {
            using var http = new HttpClient();
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