using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using DataLayer.DTOs;
using DataLayer.DTOs.Exam;

namespace ServiceLayer.Speech
{
    public interface IAzureSpeechService
    {
        Task<SpeechAnalysisDTO> AnalyzePronunciationAsync(IFormFile audioFile, string referenceText);
        Task<SpeechAnalysisDTO> AnalyzePronunciationFromUrlAsync(string audioUrl, string referenceText, string language = null);
        Task<string> RecognizeFromUrlAsync(string audioUrl, string language = null);
        Task<string> RecognizeFromFileAsync(IFormFile audioFile, string language = null);
    }
}