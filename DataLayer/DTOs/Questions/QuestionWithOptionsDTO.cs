using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{
    public class QuestionWithOptionsDTO
    {
        public AddQuestionDTO Question { get; set; }
        public List<OptionDTO> Options { get; set; }
    }
}