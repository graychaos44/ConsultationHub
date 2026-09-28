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

                CREATE TABLE IF NOT EXISTS AppMeta (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
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

            // Automatic migration: add ImagePaths column if table exists without it
            try
            {
                using var alterImgCmd = new SqliteCommand("ALTER TABLE Consultations ADD COLUMN ImagePaths TEXT;", connection);
                alterImgCmd.ExecuteNonQuery();
            }
            catch
            {
                // Column already exists
            }

            // Check if seeded before
            string checkMetaSql = "SELECT Value FROM AppMeta WHERE Key = 'IsSeeded';";
            using var checkMetaCmd = new SqliteCommand(checkMetaSql, connection);
            var seededVal = checkMetaCmd.ExecuteScalar();

            if (seededVal == null)
            {
                string countSql = "SELECT COUNT(*) FROM Consultations;";
                using var countCmd = new SqliteCommand(countSql, connection);
                long count = (long)countCmd.ExecuteScalar()!;
                if (count == 0)
                {
                    SeedSampleData(connection);
                }

                string setMetaSql = "INSERT OR REPLACE INTO AppMeta (Key, Value) VALUES ('IsSeeded', '1');";
                using var setMetaCmd = new SqliteCommand(setMetaSql, connection);
                setMetaCmd.ExecuteNonQuery();
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
                (ConsultationDate, ClientName, ClientPhone, Address, Category, Status, Priority, Summary, Details, FollowUpDate, Tags, ImagePaths, CreatedAt, UpdatedAt)
                VALUES (@ConsultationDate, @ClientName, @ClientPhone, @Address, @Category, @Status, @Priority, @Summary, @Details, @FollowUpDate, @Tags, @ImagePaths, @CreatedAt, @UpdatedAt);
            ";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ConsultationDate", record.ConsultationDate.ToString("o"));
            cmd.Parameters.AddWithValue("@ClientName", record.ClientName ?? "");
            cmd.Parameters.AddWithValue("@ClientPhone", record.ClientPhone ?? "");
            cmd.Parameters.AddWithValue("@Address", record.Address ?? "");
            cmd.Parameters.AddWithValue("@Category", record.Category);
            cmd.Parameters.AddWithValue("@Status", record.Status);
            cmd.Parameters.AddWithValue("@Priority", record.Priority);
            cmd.Parameters.AddWithValue("@Summary", record.Summary);
            cmd.Parameters.AddWithValue("@Details", record.Details ?? "");
            cmd.Parameters.AddWithValue("@FollowUpDate", record.FollowUpDate.HasValue ? record.FollowUpDate.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", record.Tags ?? "");
            cmd.Parameters.AddWithValue("@ImagePaths", record.ImagePaths ?? "");
            cmd.Parameters.AddWithValue("@CreatedAt", record.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@UpdatedAt", record.UpdatedAt.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public List<ConsultationRecord> GetFilteredRecords(
            string? searchText, 
            string? category, 
            string? status, 
            DateTime? startDate, 
            DateTime? endDate,
            string? quickFilter = "전체")
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

            // Quick Filter Chips handling
            if (!string.IsNullOrWhiteSpace(quickFilter))
            {
                switch (quickFilter)
                {
                    case "오늘":
                        sql += " AND ConsultationDate >= @TodayStart AND ConsultationDate < @TomorrowStart";
                        break;
                    case "진행중":
                        sql += " AND Status = '진행중'";
                        break;
                    case "재상담 예정":
                        sql += " AND FollowUpDate IS NOT NULL AND Status != '완료'";
                        break;
                    case "완료":
                        sql += " AND Status = '완료'";
                        break;
                    case "긴급":
                        sql += " AND Priority = '긴급'";
                        break;
                }
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

            if (quickFilter == "오늘")
            {
                cmd.Parameters.AddWithValue("@TodayStart", DateTime.Today.ToString("o"));
                cmd.Parameters.AddWithValue("@TomorrowStart", DateTime.Today.AddDays(1).ToString("o"));
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
                    ImagePaths = @ImagePaths,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id;
            ";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Id", record.Id);
            cmd.Parameters.AddWithValue("@ConsultationDate", record.ConsultationDate.ToString("o"));
            cmd.Parameters.AddWithValue("@ClientName", record.ClientName ?? "");
            cmd.Parameters.AddWithValue("@ClientPhone", record.ClientPhone ?? "");
            cmd.Parameters.AddWithValue("@Address", record.Address ?? "");
            cmd.Parameters.AddWithValue("@Category", record.Category);
            cmd.Parameters.AddWithValue("@Status", record.Status);
            cmd.Parameters.AddWithValue("@Priority", record.Priority);
            cmd.Parameters.AddWithValue("@Summary", record.Summary);
            cmd.Parameters.AddWithValue("@Details", record.Details ?? "");
            cmd.Parameters.AddWithValue("@FollowUpDate", record.FollowUpDate.HasValue ? record.FollowUpDate.Value.ToString("o") : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", record.Tags ?? "");
            cmd.Parameters.AddWithValue("@ImagePaths", record.ImagePaths ?? "");
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

        public List<ConsultationRecord> FindMatchingCustomers(string? phone, string? address, string? name)
        {
            var results = new List<ConsultationRecord>();

            string cleanPhone = new string((phone ?? "").Where(char.IsDigit).ToArray());
            string cleanAddress = (address ?? "").Trim();
            string cleanName = (name ?? "").Trim();

            // Match requires at least 4 digits of phone, or at least 4 chars of address, or at least 2 chars of name
            bool hasValidPhone = cleanPhone.Length >= 4;
            bool hasValidAddress = cleanAddress.Length >= 4;
            bool hasValidName = cleanName.Length >= 2;

            if (!hasValidPhone && !hasValidAddress && !hasValidName)
            {
                return results;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var conditions = new List<string>();

            if (hasValidPhone)
            {
                conditions.Add("REPLACE(REPLACE(ClientPhone, '-', ''), ' ', '') LIKE @Phone");
            }
            if (hasValidAddress)
            {
                conditions.Add("Address LIKE @Address");
            }
            if (hasValidName)
            {
                conditions.Add("ClientName LIKE @Name");
            }

            string sql = $@"
                SELECT * FROM Consultations 
                WHERE {string.Join(" OR ", conditions)}
                ORDER BY ConsultationDate DESC
                LIMIT 10;
            ";

            using var cmd = new SqliteCommand(sql, connection);
            if (hasValidPhone)
            {
                cmd.Parameters.AddWithValue("@Phone", $"%{cleanPhone}%");
            }
            if (hasValidAddress)
            {
                cmd.Parameters.AddWithValue("@Address", $"%{cleanAddress}%");
            }
            if (hasValidName)
            {
                cmd.Parameters.AddWithValue("@Name", $"%{cleanName}%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(ReadRecord(reader));
            }

            return results;
        }

        public (int TodayCount, int MonthCount, int PendingFollowUpCount, int TotalCount) GetStatistics()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string todayStart = DateTime.Today.ToString("o");
            string tomorrowStart = DateTime.Today.AddDays(1).ToString("o");
            string monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("o");
            string nextMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).ToString("o");

            string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM Consultations WHERE ConsultationDate >= @TodayStart AND ConsultationDate < @TomorrowStart),
                    (SELECT COUNT(*) FROM Consultations WHERE ConsultationDate >= @MonthStart AND ConsultationDate < @NextMonthStart),
                    (SELECT COUNT(*) FROM Consultations WHERE FollowUpDate IS NOT NULL AND Status != '완료'),
                    (SELECT COUNT(*) FROM Consultations);
            ";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@TodayStart", todayStart);
            cmd.Parameters.AddWithValue("@TomorrowStart", tomorrowStart);
            cmd.Parameters.AddWithValue("@MonthStart", monthStart);
            cmd.Parameters.AddWithValue("@NextMonthStart", nextMonthStart);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int today = reader.GetInt32(0);
                int month = reader.GetInt32(1);
                int pending = reader.GetInt32(2);
                int total = reader.GetInt32(3);
                return (today, month, pending, total);
            }

            return (0, 0, 0, 0);
        }

        private static ConsultationRecord ReadRecord(SqliteDataReader reader)
        {
            var record = new ConsultationRecord
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                ConsultationDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("ConsultationDate"))),
                ClientName = reader.GetString(reader.GetOrdinal("ClientName")),
                ClientPhone = reader.IsDBNull(reader.GetOrdinal("ClientPhone")) ? "" : reader.GetString(reader.GetOrdinal("ClientPhone")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Priority = reader.GetString(reader.GetOrdinal("Priority")),
                Summary = reader.GetString(reader.GetOrdinal("Summary")),
                Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? "" : reader.GetString(reader.GetOrdinal("Details")),
                Tags = reader.IsDBNull(reader.GetOrdinal("Tags")) ? "" : reader.GetString(reader.GetOrdinal("Tags")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt")))
            };

            int addressIndex = reader.GetOrdinal("Address");
            if (addressIndex >= 0 && !reader.IsDBNull(addressIndex))
            {
                record.Address = reader.GetString(addressIndex);
            }

            int followUpIndex = reader.GetOrdinal("FollowUpDate");
            if (!reader.IsDBNull(followUpIndex))
            {
                record.FollowUpDate = DateTime.Parse(reader.GetString(followUpIndex));
            }

            int imagePathsIndex = -1;
            try { imagePathsIndex = reader.GetOrdinal("ImagePaths"); } catch { }
            if (imagePathsIndex >= 0 && !reader.IsDBNull(imagePathsIndex))
            {
                record.ImagePaths = reader.GetString(imagePathsIndex);
            }

            return record;
        }

        public static string SaveImageToAppStorage(string sourceFilePath)
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger", "Images");
            Directory.CreateDirectory(appDataPath);
            string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            string newFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            string destPath = Path.Combine(appDataPath, newFileName);
            File.Copy(sourceFilePath, destPath, true);
            return destPath;
        }

        public static string SaveBitmapSourceToAppStorage(System.Windows.Media.Imaging.BitmapSource bitmap)
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger", "Images");
            Directory.CreateDirectory(appDataPath);
            string newFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            string destPath = Path.Combine(appDataPath, newFileName);

            using var fileStream = new FileStream(destPath, FileMode.Create);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            encoder.Save(fileStream);

            return destPath;
        }
    }
}
