using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserNote
{
    public class UserNoteRequestDTO
    {
        public int NoteId { get; set; }

        public int UserId { get; set; }

        public int ArticleId { get; set; }

        public int SectionId { get; set; }

        public string NoteContent { get; set; } = null!;
    }
}
