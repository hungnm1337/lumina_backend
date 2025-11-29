using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.AI
{
    
    public class ChatResponseDTO
    {
        public string Answer { get; set; } = string.Empty;

        public int ConfidenceScore { get; set; }

        public List<string> SuggestedQuestions { get; set; } = new List<string>();

        public List<string> RelatedTopics { get; set; } = new List<string>();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; } = true;

        public string? ErrorMessage { get; set; }
    }

    
    public class ChatConversationResponseDTO
    {
        public ChatResponseDTO CurrentResponse { get; set; } = new ChatResponseDTO();

        public List<ChatMessageDTO> ConversationHistory { get; set; } = new List<ChatMessageDTO>();

        public string SessionId { get; set; } = Guid.NewGuid().ToString();
    }
}