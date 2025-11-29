using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.AI
{
   
    public class ChatRequestDTO
    {
        public string UserQuestion { get; set; } = string.Empty;

        public string LessonContent { get; set; } = string.Empty;

        public List<ChatMessageDTO>? ConversationHistory { get; set; }

        public string? LessonTitle { get; set; }

        public int? UserId { get; set; }

        public int? ArticleId { get; set; }
    }

    
    public class ChatMessageDTO
    {
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}