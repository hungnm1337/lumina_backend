using DataLayer.DTOs;
using DataLayer.DTOs.Exam.Speaking;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Speaking
{
    public interface ISpeakingScoringService
    {
        Task<SpeakingScoringResultDTO> ProcessAndScoreAnswerAsync(IFormFile audioFile, int questionId, int attemptId);
    }
}