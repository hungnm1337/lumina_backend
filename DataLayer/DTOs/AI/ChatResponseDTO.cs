using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.AI
{
    /// <summary>
    /// Response DTO from AI chatbot
    /// </summary>
    public class ChatResponseDTO
    {
        /// <summary>
        /// AI's response to the user's question
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0-100)
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// Suggested follow-up questions
        /// </summary>
        public List<string> SuggestedQuestions { get; set; } = new List<string>();

        /// <summary>
        /// Related topics or concepts mentioned
        /// </summary>
        public List<string> RelatedTopics { get; set; } = new List<string>();

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if any
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Extended response with conversation context
    /// </summary>
    public class ChatConversationResponseDTO
    {
        /// <summary>
        /// Current response
        /// </summary>
        public ChatResponseDTO CurrentResponse { get; set; } = new ChatResponseDTO();

        /// <summary>
        /// Updated conversation history
        /// </summary>
        public List<ChatMessageDTO> ConversationHistory { get; set; } = new List<ChatMessageDTO>();

        /// <summary>
        /// Session ID for tracking conversation
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
    }
}
