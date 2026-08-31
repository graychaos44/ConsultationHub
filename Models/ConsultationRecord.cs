using System;

namespace ConsultationLedger.Models
{
    public class ConsultationRecord
    {
        public long Id { get; set; }
        public int RowIndex { get; set; } // 1, 2, 3... 순번 표시용
        public DateTime ConsultationDate { get; set; } = DateTime.Now;
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty; // 고객 주소
        public string Category { get; set; } = "일반상담";
        public string Status { get; set; } = "진행중"; // 대기중, 진행중, 완료, 보류
        public string Priority { get; set; } = "보통"; // 보통, 중요, 긴급
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime? FollowUpDate { get; set; }
        public string Tags { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Computed Helper Properties for UI display
        public string DisplayClientName => string.IsNullOrWhiteSpace(ClientName) ? "(미기재)" : ClientName;
        public string FormattedDate => ConsultationDate.ToString("yyyy-MM-dd HH:mm");
        public string FormattedFollowUp => FollowUpDate.HasValue ? FollowUpDate.Value.ToString("yyyy-MM-dd") : "-";
        public bool IsFollowUpOverdue => FollowUpDate.HasValue && FollowUpDate.Value.Date <= DateTime.Today && Status != "완료";
    }
}
