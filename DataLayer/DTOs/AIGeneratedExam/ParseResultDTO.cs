using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class ParseResultDTO
    {
        public int PartNumber { get; set; }
        public int Quantity { get; set; } = 1;  // Mặc định 1 nếu AI không trả
        public string? Topic { get; set; }
    }

}
