namespace DataLayer.DTOs.Exam
{
    public class NlpRequestDTO
    {
        public string Transcript { get; set; }
        public string Sample_answer { get; set; } // Tên phải khớp với model Pydantic trong Python
    }
}