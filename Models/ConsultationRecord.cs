using System;
using System.Linq;

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
        public string ImagePaths { get; set; } = string.Empty; // 세미콜론(;) 구분 파일 경로들
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Computed Helper Properties for UI display
        public bool HasImages => !string.IsNullOrWhiteSpace(ImagePaths);
        public int ImageCount => string.IsNullOrWhiteSpace(ImagePaths) ? 0 : ImagePaths.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        public string DisplayClientName => string.IsNullOrWhiteSpace(ClientName) ? "(미기재)" : ClientName;
        public string FormattedDate => ConsultationDate.ToString("yyyy-MM-dd HH:mm");
        public string ShortDate => ConsultationDate.ToString("MM-dd HH:mm");
        public string FormattedFollowUp => FollowUpDate.HasValue ? FollowUpDate.Value.ToString("yyyy-MM-dd") : "-";
        public bool IsFollowUpOverdue => FollowUpDate.HasValue && FollowUpDate.Value.Date <= DateTime.Today && Status != "완료";

        // Modern Pill Badge Color Helpers
        public string StatusBgColor => Status switch
        {
            "완료" => "#DCFCE7",     // Soft Emerald
            "진행중" => "#DBEAFE",   // Soft Indigo/Blue
            "보류" => "#FEF3C7",     // Soft Amber
            "대기중" => "#F1F5F9",   // Soft Slate
            _ => "#F3F4F6"
        };

        public string StatusFgColor => Status switch
        {
            "완료" => "#15803D",     // Deep Emerald
            "진행중" => "#1D4ED8",   // Deep Blue
            "보류" => "#B45309",     // Deep Amber
            "대기중" => "#475569",   // Slate Gray
            _ => "#374151"
        };

        public string PriorityIcon => Priority switch
        {
            "긴급" => "🔴",
            "중요" => "🟠",
            _ => "⚪"
        };

        public string PriorityFgColor => Priority switch
        {
            "긴급" => "#DC2626",
            "중요" => "#EA580C",
            _ => "#64748B"
        };

        // Utility: Phone number auto-formatter helper
        public static string FormatPhoneNumber(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 11)
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 4)}-{digits.Substring(7, 4)}";
            if (digits.Length == 10)
            {
                if (digits.StartsWith("02"))
                    return $"{digits.Substring(0, 2)}-{digits.Substring(2, 4)}-{digits.Substring(6, 4)}";
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
            }
            if (digits.Length == 9 && digits.StartsWith("02"))
                return $"{digits.Substring(0, 2)}-{digits.Substring(2, 3)}-{digits.Substring(5, 4)}";
            if (digits.Length == 8)
                return $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}";
            return raw;
        }
    }
}
