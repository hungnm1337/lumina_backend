namespace DataLayer.DTOs.Streak
{
    /// <summary>
    /// DTO trả về kết quả sau khi update streak
    /// </summary>
    public class StreakUpdateResultDTO
    {
        /// <summary>
        /// Thành công hay không
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Thông tin streak sau khi update
        /// </summary>
        public StreakSummaryDTO? Summary { get; set; }

        /// <summary>
        /// Loại sự kiện vừa xảy ra
        /// </summary>
        public StreakEventType EventType { get; set; }

        /// <summary>
        /// Có đạt milestone không
        /// </summary>
        public bool MilestoneReached { get; set; }

        /// <summary>
        /// Milestone vừa đạt (nếu có)
        /// </summary>
        public int? MilestoneValue { get; set; }

        /// <summary>
        /// Message mô tả (dùng cho notification)
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}