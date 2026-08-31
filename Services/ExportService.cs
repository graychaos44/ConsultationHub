using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsultationLedger.Models;

namespace ConsultationLedger.Services
{
    public class ExportService
    {
        public static void ExportToCsv(IEnumerable<ConsultationRecord> records, string filePath)
        {
            var sb = new StringBuilder();
            // UTF-8 BOM for Excel compatibility
            sb.AppendLine("ID,상담일시,고객명,연락처,주소,카테고리,상태,우선순위,요약,상세내용,팔로우업일,태그");

            foreach (var r in records)
            {
                string line = $"{EscapeCsv(r.Id.ToString())}," +
                              $"{EscapeCsv(r.FormattedDate)}," +
                              $"{EscapeCsv(r.ClientName)}," +
                              $"{EscapeCsv(r.ClientPhone)}," +
                              $"{EscapeCsv(r.Address)}," +
                              $"{EscapeCsv(r.Category)}," +
                              $"{EscapeCsv(r.Status)}," +
                              $"{EscapeCsv(r.Priority)}," +
                              $"{EscapeCsv(r.Summary)}," +
                              $"{EscapeCsv(r.Details)}," +
                              $"{EscapeCsv(r.FormattedFollowUp)}," +
                              $"{EscapeCsv(r.Tags)}";
                sb.AppendLine(line);
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
