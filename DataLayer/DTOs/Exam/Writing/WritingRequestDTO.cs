using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Exam.Writting
{
    public class WritingRequestP1DTO
    {
        public string PictureCaption { get; set; }

        public string VocabularyRequest { get; set; }
        public string UserAnswer { get; set; }

    }

    public class WritingRequestP23DTO
    {
        public int PartNumber { get; set; } // 2 or 3
        public string Prompt { get; set; }
        public string UserAnswer { get; set; }

    }

}
