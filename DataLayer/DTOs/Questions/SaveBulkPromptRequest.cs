using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{
    public class SaveBulkPromptRequest
    {
        public List<CreatePromptWithQuestionsDTO> Prompts { get; set; }
        public int PartId { get; set; }
    }
}
