using DataLayer.DTOs.UserAnswer;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Listening
{
    public interface IListeningService
    {
       
        Task<SubmitAnswerResponseDTO> SubmitAnswerAsync(SubmitAnswerRequestDTO request);
    }
}