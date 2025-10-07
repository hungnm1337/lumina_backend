using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using DataLayer.DTOs;
using Services.Upload;
using Microsoft.AspNetCore.Http;

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

            // Cấu hình Azure Speech
            var subscriptionKey = _configuration["AzureSpeech:SubscriptionKey"];
            var region = _configuration["AzureSpeech:Region"];

            _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            _speechConfig.SpeechSynthesisVoiceName = _configuration["AzureSpeech:VoiceName"];
            _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);
        }

        public async Task<UploadResultDTO> GenerateAudioAsync(string text, string languageCode = "en-US")
        {
            try
            {
                // Cấu hình giọng đọc theo ngôn ngữ
                _speechConfig.SpeechSynthesisLanguage = languageCode;
                
                // Tạo synthesizer
                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);

                // Tạo SSML để có giọng đọc tự nhiên hơn
                var ssml = $@"
                    <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{languageCode}'>
                        <voice name='{_configuration["AzureSpeech:VoiceName"]}'>
                            <prosody rate='0.9' pitch='+0Hz'>
                                {text}
                            </prosody>
                        </voice>
                    </speak>";

                // Tạo audio
                var result = await synthesizer.SpeakSsmlAsync(ssml);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    // Sử dụng MemoryStream thay vì file tạm để tránh file lock
                    var tempFileName = $"tts_{Guid.NewGuid()}.mp3";
                    
                    using var memoryStream = new MemoryStream(result.AudioData);
                    var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "audio", tempFileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "audio/mpeg"
                    };

                    var uploadResult = await _uploadService.UploadFile(formFile);
                    return uploadResult;
                }
                else
                {
                    throw new Exception($"Lỗi tạo audio: {result.Reason}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo audio từ text: {ex.Message}");
            }
        }

    }
}
