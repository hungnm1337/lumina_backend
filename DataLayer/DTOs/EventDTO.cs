using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs
{
    public class EventDTO
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = null!;

        public string? Content { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public int CreateBy { get; set; }

        public int? UpdateBy { get; set; }
    }
}
