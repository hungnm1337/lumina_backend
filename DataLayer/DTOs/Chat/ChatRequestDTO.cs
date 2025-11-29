using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Chat
{
    public class ChatRequestDTO
    {
        public string Message { get; set; } = null!;
        public int UserId { get; set; }
        public string ConversationType { get; set; } = "general"; 
    }
}
