using DataLayer.DTOs;
using DataLayer.DTOs.Exam;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ServiceLayer.Speaking
{
    public interface ISpeakingScoringService
    {
        Task<SpeakingScoringResultDTO> ProcessAndScoreAnswerAsync(IFormFile audioFile, int questionId, int attemptId);
    }
}