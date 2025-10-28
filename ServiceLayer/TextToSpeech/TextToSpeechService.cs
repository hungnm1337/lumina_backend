using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using DataLayer.DTOs;
using Microsoft.AspNetCore.Http;
using ServiceLayer.UploadFile;

namespace ServiceLayer.TextToSpeech
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly IConfiguration _configuration;
        private readonly IUploadService _uploadService;
        private readonly SpeechConfig _speechConfig;

        public TextToSpeechService(IConfiguration configuration, IUploadService uploadService)
        {
            _configuration = configuration;
            _uploadService = uploadService;

            // Lấy cấu hình Azure Speech - Sửa từ AzureSpeech sang AzureSpeechSettings
            var subscriptionKey = _configuration["AzureSpeechSettings:SubscriptionKey"];
            var region = _configuration["AzureSpeechSettings:Region"];
            var voiceName = _configuration["AzureSpeechSettings:VoiceName"] ?? "en-US-JennyNeural";

            // Validate trước khi khởi tạo
            Console.WriteLine($"[TextToSpeechService] Initializing...");
            Console.WriteLine($"[TextToSpeechService] Region: {region}");
            Console.WriteLine($"[TextToSpeechService] Voice: {voiceName}");
            Console.WriteLine($"[TextToSpeechService] Key present: {!string.IsNullOrWhiteSpace(subscriptionKey)}");

            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentException("AzureSpeechSettings:SubscriptionKey is missing in appsettings.json");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentException("AzureSpeechSettings:Region is missing in appsettings.json");
            }

            // Cấu hình Azure Speech
            _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            _speechConfig.SpeechSynthesisVoiceName = voiceName;
            _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
            
            Console.WriteLine("[TextToSpeechService] Initialization successful");
        }

        public async Task<UploadResultDTO> GenerateAudioAsync(string text, string languageCode = "en-US")
        {
            try
            {
                Console.WriteLine($"[TextToSpeechService] Generating audio for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                
                // Tạo synthesizer
                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);

                // Tạo SSML để có giọng đọc tự nhiên hơn - Sửa từ AzureSpeech sang AzureSpeechSettings
                var voiceName = _configuration["AzureSpeechSettings:VoiceName"] ?? "en-US-JennyNeural";
                var ssml = $@"
                    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{languageCode}'>
                        <voice name='{voiceName}'>
                            <prosody rate='1.3' pitch='+0Hz'>
                                {System.Security.SecurityElement.Escape(text)}
                            </prosody>
                        </voice>
                    </speak>";

                // Tạo audio
                var result = await synthesizer.SpeakSsmlAsync(ssml);
                
                Console.WriteLine($"[TextToSpeechService] Synthesis result: {result.Reason}");

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    Console.WriteLine($"[TextToSpeechService] Audio generated successfully, size: {result.AudioData.Length} bytes");
                    
                    // Sử dụng MemoryStream thay vì file tạm để tránh file lock
                    var tempFileName = $"tts_{Guid.NewGuid()}.mp3";
                    
                    using var memoryStream = new MemoryStream(result.AudioData);
                    var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "audio", tempFileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "audio/mpeg"
                    };

                    var uploadResult = await _uploadService.UploadFileAsync(formFile);
                    
                    Console.WriteLine($"[TextToSpeechService] Upload successful: {uploadResult.Url}");
                    
                    return uploadResult;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    var errorMsg = $"TTS canceled: {cancellation.Reason} - {cancellation.ErrorDetails}";
                    Console.WriteLine($"[TextToSpeechService] {errorMsg}");
                    throw new Exception(errorMsg);
                }
                else
                {
                    throw new Exception($"Lỗi tạo audio: {result.Reason}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TextToSpeechService] Error: {ex.Message}");
                Console.WriteLine($"[TextToSpeechService] Stack trace: {ex.StackTrace}");
                throw new Exception($"Lỗi khi tạo audio từ text: {ex.Message}", ex);
            }
        }
    }
}
