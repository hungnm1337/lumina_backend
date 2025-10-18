using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Prompt
{
    public class PromptEditDto
    {
        public int PromptId { get; set; }

        public string Skill { get; set; } = null!;

        public string ContentText { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? ReferenceImageUrl { get; set; }

        public string? ReferenceAudioUrl { get; set; }
    }
}
