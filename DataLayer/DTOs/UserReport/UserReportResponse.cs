using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.UserReport
{
    public class UserReportResponse
    {
        public int ReportId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public string SendBy { get; set; }

        public DateTime SendAt { get; set; }

        public string? ReplyBy { get; set; }

        public DateTime? ReplyAt { get; set; }

        public string? ReplyContent { get; set; }

        public string Type { get; set; } = null!;
    }
}
