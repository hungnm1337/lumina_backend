using DataLayer.DTOs.AI;
using System.Threading.Tasks;

namespace ServiceLayer.UserNoteAI
{
    /// <summary>
    /// Interface for AI-powered chatbot service for lesson Q&A
    /// </summary>
    public interface IAIChatService
    {
        /// <summary>
        /// Ask a question about lesson content and get AI response
        /// </summary>
        /// <param name="request">Chat request with user question and lesson content</param>
        /// <returns>AI generated response</returns>
        Task<ChatResponseDTO> AskQuestionAsync(ChatRequestDTO request);

        /// <summary>
        /// Continue a conversation with context from previous messages
        /// </summary>
        /// <param name="request">Chat request with conversation history</param>
        /// <returns>AI response with updated conversation history</returns>
        Task<ChatConversationResponseDTO> ContinueConversationAsync(ChatRequestDTO request);

        /// <summary>
        /// Generate follow-up questions based on lesson content
        /// </summary>
        /// <param name="lessonContent">Content of the lesson</param>
        /// <param name="lessonTitle">Optional title of the lesson</param>
        /// <returns>List of suggested questions</returns>
        Task<ChatResponseDTO> GenerateSuggestedQuestionsAsync(string lessonContent, string? lessonTitle = null);

        /// <summary>
        /// Explain a specific concept or term from the lesson
        /// </summary>
        /// <param name="concept">Concept or term to explain</param>
        /// <param name="lessonContext">Context from the lesson</param>
        /// <returns>Detailed explanation</returns>
        Task<ChatResponseDTO> ExplainConceptAsync(string concept, string lessonContext);
    }
}
