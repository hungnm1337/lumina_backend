using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Passage
{
    public class PassageEditDto
    {
        public int PassageId { get; set; }
        public string Title { get; set; }
        public string ContentText { get; set; }

        public PromptEditDto Prompt { get; set; }
    }

    public class PromptEditDto
    {
        public int PromptId { get; set; }
        public string Skill { get; set; }
        public string PromptText { get; set; }
        public string? ReferenceImageUrl { get; set; }
        public string? ReferenceAudioUrl { get; set; }
    }

}
