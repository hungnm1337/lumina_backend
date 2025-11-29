using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class AIGeneratedOptionDTO
    {
        
        public string Label { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool? IsCorrect { get; set; }
    }
}
