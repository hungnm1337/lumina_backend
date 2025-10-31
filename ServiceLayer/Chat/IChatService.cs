using DataLayer.DTOs.Chat;

namespace ServiceLayer.Chat
{
    public interface IChatService
    {
        Task<ChatResponseDTO> ProcessMessage(ChatRequestDTO request);
        Task<SaveVocabularyResponseDTO> SaveGeneratedVocabularies(SaveVocabularyRequestDTO request);
    }
}
