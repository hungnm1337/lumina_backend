using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.AI
{
    /// <summary>
    /// Request DTO for AI chatbot asking questions about lesson content
    /// </summary>
    public class ChatRequestDTO
    {
        /// <summary>
        /// User's question about the lesson
        /// </summary>
        public string UserQuestion { get; set; } = string.Empty;

        /// <summary>
        /// Lesson content or context for the chatbot
        /// </summary>
        public string LessonContent { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Previous conversation history for context
        /// </summary>
        public List<ChatMessageDTO>? ConversationHistory { get; set; }

        /// <summary>
        /// Optional: Lesson title or topic
        /// </summary>
        public string? LessonTitle { get; set; }

        /// <summary>
        /// Optional: User ID for tracking
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Optional: Article or section ID
        /// </summary>
        public int? ArticleId { get; set; }
    }

    /// <summary>
    /// Individual chat message for conversation history
    /// </summary>
    public class ChatMessageDTO
    {
        /// <summary>
        /// Role: "user" or "assistant"
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the message
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
