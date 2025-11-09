using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserNote
{
    public class UserNoteResponseDTO
    {
        public int NoteId { get; set; }

        public string User { get; set; }

        public int UserId { get; set; }

        public string Article { get; set; }

        public int ArticleId { get; set; }

        public string Section { get; set; }

        public int SectionId { get; set; }

        public string NoteContent { get; set; } = null!;

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }
    }
}
