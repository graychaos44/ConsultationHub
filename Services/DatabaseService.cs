using System;
using System.Collections.Generic;
using System.IO;

using ConsultationLedger.Models;

using Microsoft.Data.Sqlite;

namespace ConsultationLedger.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
            Directory.CreateDirectory(appDataPath);
            string dbPath = Path.Combine(appDataPath, "consultations.db");
            _connectionString = $"Data Source={dbPath}";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Consultations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConsultationDate TEXT NOT NULL,
                    ClientName TEXT NOT NULL,
                    ClientPhone TEXT,
                    Address TEXT,
                    Category TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Priority TEXT NOT NULL,
                    Summary TEXT NOT NULL,
                    Details TEXT,
                    FollowUpDate TEXT,
                    Tags TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
            ";

            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();

            // Automatic migration: add Address column if table exists without it
            try
            {
                using var alterCmd = new SqliteCommand("ALTER TABLE Consultations ADD COLUMN Address TEXT;", connection);
                alterCmd.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists
            }

            // Seed sample data if empty
            string countSql = "SELECT COUNT(*) FROM Consultations;";
            using var countCmd = new SqliteCommand(countSql, connection);
            long count = (long)countCmd.ExecuteScalar()!;
            if (count == 0)
            {
                SeedSampleData(connection);
            }
        }

        private void SeedSampleData(SqliteConnection connection)
        {
            var samples = new List<ConsultationRecord>
            {
                new ConsultationRecord
                {
                    ConsultationDate = DateTime.Now.AddHours(-2),
                    ClientName = "김철수",
                    ClientPhone = "010-9876-5432",
                    Address = "서울특별시 강남구 테헤란로 123",
                    Category = "상품문의",
                    Status = "완료",
                    Priority = "보통",
                    Summary = "신규 솔루션 견적 및 구축 절차 문의",
                    Details = "신규 솔루션도입 관련 비용 견적서를 이메일로 요청함. 기본 사양 패키지 PDF 발송 완료.",
                    FollowUpDate = DateTime.Today.AddDays(3),
                    Tags = "#신규고객,#견적서",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new ConsultationRecord
                {
                    ConsultationDate = DateTime.Now.AddDays(-1),
                    ClientName = "이영희",
                    ClientPhone = "010-1234-5678",
                    Address = "경기도 성남시 분당구 판교역로 456",
                    Category = "서비스지원",
                    Status = "진행중",
                    Priority = "중요",
                    Summary = "시스템 접속 오류 해결 요청",
                    Details = "로그인 시 403 권한 오류 발생. 계정 권한 재설정 진행 중이며 담당 엔지니어 확인 필요.",
                    FollowUpDate = DateTime.Today,
                    Tags = "#장애처리,#긴급",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new ConsultationRecord
                {
                    ConsultationDate = DateTime.Now.AddDays(-3),
                    ClientName = "박민수",
                    ClientPhone = "010-5555-4321",
                    Address = "부산광역시 해운대구 해운대로 789",
                    Category = "법률/행정",
                    Status = "보류",
                    Priority = "긴급",
                    Summary = "계약서 수정 조항 검토 관련 상담",
                    Details = "3조 2항 보증 기간 관련 수정 요청. 법무팀 최종 승인 대기 중.",
                    FollowUpDate = DateTime.Today.AddDays(1),
                    Tags = "#계약서,#법무검토",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    UpdatedAt = DateTime.Now.AddDays(-3)
                }
            };

            foreach (var item in samples)
            {
                InsertRecordInternal(connection, item);
            }
        }

        private void InsertRecordInternal(SqliteConnection connection, ConsultationRecord record)
        {
            string sql = @"
                INSERT INTO Consultations 
                (ConsultationDate, ClientName, ClientPhone, Address, Category, Status, Priority, Summary, Details, FollowUpDate, Tags, CreatedAt, UpdatedAt)
                VALUES (@ConsultationDate, @ClientName, @ClientPhone, @Address, @Category, @Status, @Priority, @Summary, @Details, @FollowUpDate, @Tags, @CreatedAt, @UpdatedAt);
            ";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ConsultationDate", record.ConsultationDate.ToString("o"));
            cmd.Parameters.AddWithValue("@ClientName", record.ClientName);
            cmd.Parameters.AddWithValue("@ClientPhone", record.ClientPhone ?? "");
            cmd.Parameters.AddWithValue("@Address", record.Address ?? "");
            cmd.Parameters.AddWithValue("@Category", record.Category);
            cmd.Parameters.AddWithValue("@Status", record.Status);
            cmd.Parameters.AddWithValue("@Priority", record.Priority);
            cmd.Parameters.AddWithValue("@Summary", record.Summary);
            cmd.Parameters.AddWithValue("@Details", record.Details ?? "");
            cmd.Parameters.AddWithValue("@FollowUpDate", record.FollowUpDate.HasValue ? record.FollowUpDate.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", record.Tags ?? "");
            cmd.Parameters.AddWithValue("@CreatedAt", record.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@UpdatedAt", record.UpdatedAt.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public List<ConsultationRecord> GetFilteredRecords(string? searchText, string? category, string? status, DateTime? startDate, DateTime? endDate)
        {
            var list = new List<ConsultationRecord>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT * FROM Consultations WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " AND (ClientName LIKE @Search OR ClientPhone LIKE @Search OR Address LIKE @Search OR Summary LIKE @Search OR Details LIKE @Search OR Tags LIKE @Search)";
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "전체")
            {
                sql += " AND Category = @Category";
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "전체")
            {
                sql += " AND Status = @Status";
            }

            if (startDate.HasValue)
            {
                sql += " AND ConsultationDate >= @StartDate";
            }

            if (endDate.HasValue)
            {
                sql += " AND ConsultationDate <= @EndDate";
            }

            sql += " ORDER BY ConsultationDate DESC;";

            using var cmd = new SqliteCommand(sql, connection);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                cmd.Parameters.AddWithValue("@Search", $"%{searchText.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "전체")
            {
                cmd.Parameters.AddWithValue("@Category", category);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "전체")
            {
                cmd.Parameters.AddWithValue("@Status", status);
            }

            if (startDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value.Date.ToString("o"));
            }

            if (endDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value.Date.AddDays(1).AddTicks(-1).ToString("o"));
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadRecord(reader));
            }

            return list;
        }

        public void AddRecord(ConsultationRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            InsertRecordInternal(connection, record);
        }

        public void UpdateRecord(ConsultationRecord record)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = @"
                UPDATE Consultations SET
                    ConsultationDate = @ConsultationDate,
                    ClientName = @ClientName,
                    ClientPhone = @ClientPhone,
                    Address = @Address,
                    Category = @Category,
                    Status = @Status,
                    Priority = @Priority,
                    Summary = @Summary,
                    Details = @Details,
                    FollowUpDate = @FollowUpDate,
                    Tags = @Tags,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
            ";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", record.Id);
            cmd.Parameters.AddWithValue("@ConsultationDate", record.ConsultationDate.ToString("o"));
            cmd.Parameters.AddWithValue("@ClientName", record.ClientName);
            cmd.Parameters.AddWithValue("@ClientPhone", record.ClientPhone ?? "");
            cmd.Parameters.AddWithValue("@Address", record.Address ?? "");
            cmd.Parameters.AddWithValue("@Category", record.Category);
            cmd.Parameters.AddWithValue("@Status", record.Status);
            cmd.Parameters.AddWithValue("@Priority", record.Priority);
            cmd.Parameters.AddWithValue("@Summary", record.Summary);
            cmd.Parameters.AddWithValue("@Details", record.Details ?? "");
            cmd.Parameters.AddWithValue("@FollowUpDate", record.FollowUpDate.HasValue ? record.FollowUpDate.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", record.Tags ?? "");
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public void DeleteRecord(long id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "DELETE FROM Consultations WHERE Id = @Id;";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public (int todayCount, int monthCount, int pendingFollowUpCount, int totalCount) GetStatistics()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            DateTime today = DateTime.Today;
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            string todaySql = "SELECT COUNT(*) FROM Consultations WHERE Date(ConsultationDate) = Date('now', 'localtime');";
            string monthSql = "SELECT COUNT(*) FROM Consultations WHERE ConsultationDate >= @FirstDay;";
            string pendingSql = "SELECT COUNT(*) FROM Consultations WHERE FollowUpDate IS NOT NULL AND Status != '완료';";
            string totalSql = "SELECT COUNT(*) FROM Consultations;";

            using var todayCmd = new SqliteCommand(todaySql, connection);
            int todayCount = Convert.ToInt32(todayCmd.ExecuteScalar());

            using var monthCmd = new SqliteCommand(monthSql, connection);
            monthCmd.Parameters.AddWithValue("@FirstDay", firstDayOfMonth.ToString("o"));
            int monthCount = Convert.ToInt32(monthCmd.ExecuteScalar());

            using var pendingCmd = new SqliteCommand(pendingSql, connection);
            int pendingCount = Convert.ToInt32(pendingCmd.ExecuteScalar());

            using var totalCmd = new SqliteCommand(totalSql, connection);
            int totalCount = Convert.ToInt32(totalCmd.ExecuteScalar());

            return (todayCount, monthCount, pendingCount, totalCount);
        }

        public List<ConsultationRecord> FindMatchingCustomers(string? phone, string? address, string? name)
        {
            var results = new List<ConsultationRecord>();
            bool hasPhone = !string.IsNullOrWhiteSpace(phone) && phone.Trim().Length >= 4;
            bool hasAddress = !string.IsNullOrWhiteSpace(address) && address.Trim().Length >= 3;
            bool hasName = !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;

            if (!hasPhone && !hasAddress && !hasName)
                return results;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT * FROM Consultations WHERE 1=0";
            if (hasPhone) sql += " OR ClientPhone LIKE @Phone";
            if (hasAddress) sql += " OR Address LIKE @Address";
            if (hasName) sql += " OR ClientName = @Name";

            sql += " ORDER BY ConsultationDate DESC LIMIT 5;";

            using var cmd = new SqliteCommand(sql, connection);
            if (hasPhone) cmd.Parameters.AddWithValue("@Phone", $"%{phone!.Trim()}%");
            if (hasAddress) cmd.Parameters.AddWithValue("@Address", $"%{address!.Trim()}%");
            if (hasName) cmd.Parameters.AddWithValue("@Name", name!.Trim());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(ReadRecord(reader));
            }

            return results;
        }

        private ConsultationRecord ReadRecord(SqliteDataReader reader)
        {
            return new ConsultationRecord
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                ConsultationDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("ConsultationDate"))),
                ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                ClientPhone = reader.IsDBNull(reader.GetOrdinal("ClientPhone")) ? "" : reader.GetString(reader.GetOrdinal("ClientPhone")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "" : reader.GetString(reader.GetOrdinal("Address")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Priority = reader.GetString(reader.GetOrdinal("Priority")),
                Summary = reader.GetString(reader.GetOrdinal("Summary")),
                Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? "" : reader.GetString(reader.GetOrdinal("Details")),
                FollowUpDate = reader.IsDBNull(reader.GetOrdinal("FollowUpDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("FollowUpDate"))),
                Tags = reader.IsDBNull(reader.GetOrdinal("Tags")) ? "" : reader.GetString(reader.GetOrdinal("Tags")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
            };
        }
    }
}
