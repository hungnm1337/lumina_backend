using DataLayer.DTOs.AI;
using System.Threading.Tasks;

namespace ServiceLayer.UserNoteAI
{
    
    public interface IAIChatService
    {
        
        Task<ChatResponseDTO> AskQuestionAsync(ChatRequestDTO request);

       
        Task<ChatConversationResponseDTO> ContinueConversationAsync(ChatRequestDTO request);

        
        Task<ChatResponseDTO> GenerateSuggestedQuestionsAsync(string lessonContent, string? lessonTitle = null);

       
        Task<ChatResponseDTO> ExplainConceptAsync(string concept, string lessonContext);
    }
}