using DataLayer.DTOs;

namespace ServiceLayer.TextToSpeech
{
    public interface ITextToSpeechService
    {
        Task<UploadResultDTO> GenerateAudioAsync(string text, string languageCode = "en-US");
    }
}
