using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using ConsultationLedger.Models;
using ConsultationLedger.Services;

namespace ConsultationLedger.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;

        private ObservableCollection<ConsultationRecord> _records = new();
        public ObservableCollection<ConsultationRecord> Records
        {
            get => _records;
            set => SetProperty(ref _records, value);
        }

        private ConsultationRecord? _selectedRecord;
        public ConsultationRecord? SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (SetProperty(ref _selectedRecord, value) && value != null)
                {
                    LoadRecordForEditing(value);
                }
            }
        }

        private ConsultationRecord _editRecord = new();
        public ConsultationRecord EditRecord
        {
            get => _editRecord;
            set => SetProperty(ref _editRecord, value);
        }

        private bool _isEditingNew = true;
        public bool IsEditingNew
        {
            get => _isEditingNew;
            set => SetProperty(ref _isEditingNew, value);
        }

        // Filter Properties
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PerformSearch();
                }
            }
        }

        private string _selectedCategoryFilter = "전체";
        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                {
                    PerformSearch();
                }
            }
        }

        private string _selectedStatusFilter = "전체";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    PerformSearch();
                }
            }
        }

        private DateTime? _startDateFilter;
        public DateTime? StartDateFilter
        {
            get => _startDateFilter;
            set
            {
                if (SetProperty(ref _startDateFilter, value))
                {
                    PerformSearch();
                }
            }
        }

        private DateTime? _endDateFilter;
        public DateTime? EndDateFilter
        {
            get => _endDateFilter;
            set
            {
                if (SetProperty(ref _endDateFilter, value))
                {
                    PerformSearch();
                }
            }
        }

        // Dashboard Stats
        private int _todayCount;
        public int TodayCount { get => _todayCount; set => SetProperty(ref _todayCount, value); }

        private int _monthCount;
        public int MonthCount { get => _monthCount; set => SetProperty(ref _monthCount, value); }

        private int _pendingFollowUpCount;
        public int PendingFollowUpCount { get => _pendingFollowUpCount; set => SetProperty(ref _pendingFollowUpCount, value); }

        private int _totalCount;
        public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }

        private string _statusMessage = "준비 완료";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        // Duplicate Detection Properties
        private bool _hasExistingCustomerMatch;
        public bool HasExistingCustomerMatch
        {
            get => _hasExistingCustomerMatch;
            set => SetProperty(ref _hasExistingCustomerMatch, value);
        }

        private string _existingMatchMessage = string.Empty;
        public string ExistingMatchMessage
        {
            get => _existingMatchMessage;
            set => SetProperty(ref _existingMatchMessage, value);
        }

        private ConsultationRecord? _matchedRecord;
        public ConsultationRecord? MatchedRecord
        {
            get => _matchedRecord;
            set => SetProperty(ref _matchedRecord, value);
        }

        // Collections for Comboboxes
        public ObservableCollection<string> Categories { get; } = new() { "전체", "일반상담", "법률/행정", "상품문의", "서비스지원", "기타" };
        public ObservableCollection<string> EditCategories { get; } = new() { "일반상담", "법률/행정", "상품문의", "서비스지원", "기타" };
        public ObservableCollection<string> Statuses { get; } = new() { "전체", "대기중", "진행중", "완료", "보류" };
        public ObservableCollection<string> EditStatuses { get; } = new() { "대기중", "진행중", "완료", "보류" };
        public ObservableCollection<string> Priorities { get; } = new() { "보통", "중요", "긴급" };

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand NewRecordCommand { get; }
        public ICommand SaveRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand CheckDuplicatesCommand { get; }
        public ICommand FillExistingCustomerCommand { get; }
        public ICommand FilterHistoryForMatchedCustomerCommand { get; }

        public MainViewModel()
        {
            _dbService = new DatabaseService();

            SearchCommand = new RelayCommand(PerformSearch);
            ResetFilterCommand = new RelayCommand(ResetFilters);
            NewRecordCommand = new RelayCommand(PrepareNewRecord);
            SaveRecordCommand = new RelayCommand(SaveRecord);
            DeleteRecordCommand = new RelayCommand(DeleteRecord, () => SelectedRecord != null || EditRecord.Id > 0);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            CheckDuplicatesCommand = new RelayCommand(CheckDuplicates);
            FillExistingCustomerCommand = new RelayCommand(FillExistingCustomer);
            FilterHistoryForMatchedCustomerCommand = new RelayCommand(FilterHistoryForMatchedCustomer);

            PrepareNewRecord();
            LoadData();
        }

        public void CheckDuplicates()
        {
            if (!IsEditingNew)
            {
                HasExistingCustomerMatch = false;
                return;
            }

            var matches = _dbService.FindMatchingCustomers(EditRecord?.ClientPhone, EditRecord?.Address, EditRecord?.ClientName);
            if (matches.Count > 0)
            {
                MatchedRecord = matches.First();
                HasExistingCustomerMatch = true;
                ExistingMatchMessage = $"💡 힌트: '{MatchedRecord.ClientName}' 님과 일치하는 이전 상담 {matches.Count}건 보관 중 (최근: {MatchedRecord.FormattedDate})";
            }
            else
            {
                HasExistingCustomerMatch = false;
                MatchedRecord = null;
                ExistingMatchMessage = string.Empty;
            }
        }

        private void FillExistingCustomer()
        {
            if (MatchedRecord == null) return;
            EditRecord.ClientName = MatchedRecord.ClientName;
            EditRecord.ClientPhone = MatchedRecord.ClientPhone;
            EditRecord.Address = MatchedRecord.Address;

            // Trigger UI update
            var temp = EditRecord;
            EditRecord = null!;
            EditRecord = temp;

            StatusMessage = $"'{MatchedRecord.ClientName}' 님의 기존 인적사항이 자동 입력되었습니다.";
        }

        private void FilterHistoryForMatchedCustomer()
        {
            if (MatchedRecord == null) return;
            SearchText = MatchedRecord.ClientName;
            PerformSearch();
            StatusMessage = $"'{MatchedRecord.ClientName}' 님의 이전 상담 이력을 조회했습니다.";
        }

        public void LoadData()
        {
            PerformSearch();
            RefreshStats();
        }

        private void PerformSearch()
        {
            var results = _dbService.GetFilteredRecords(SearchText, SelectedCategoryFilter, SelectedStatusFilter, StartDateFilter, EndDateFilter);
            for (int i = 0; i < results.Count; i++)
            {
                results[i].RowIndex = i + 1;
            }
            Records = new ObservableCollection<ConsultationRecord>(results);
            StatusMessage = $"조회 결과: 총 {Records.Count}건";
        }

        private void RefreshStats()
        {
            var (today, month, pending, total) = _dbService.GetStatistics();
            TodayCount = today;
            MonthCount = month;
            PendingFollowUpCount = pending;
            TotalCount = total;
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedCategoryFilter = "전체";
            SelectedStatusFilter = "전체";
            StartDateFilter = null;
            EndDateFilter = null;
            PerformSearch();
        }

        private void PrepareNewRecord()
        {
            EditRecord = new ConsultationRecord
            {
                ConsultationDate = DateTime.Now,
                Category = "일반상담",
                Status = "진행중",
                Priority = "보통"
            };
            IsEditingNew = true;
            SelectedRecord = null;
            StatusMessage = "신규 상담 입력 모드";
        }

        private void LoadRecordForEditing(ConsultationRecord record)
        {
            EditRecord = new ConsultationRecord
            {
                Id = record.Id,
                ConsultationDate = record.ConsultationDate,
                ClientName = record.ClientName,
                ClientPhone = record.ClientPhone,
                Address = record.Address,
                Category = record.Category,
                Status = record.Status,
                Priority = record.Priority,
                Summary = record.Summary,
                Details = record.Details,
                FollowUpDate = record.FollowUpDate,
                Tags = record.Tags,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            };
            IsEditingNew = false;
            StatusMessage = $"상담 기록 #{record.Id} ({record.ClientName}) 수정 모드";
        }

        private void SaveRecord()
        {
            if (string.IsNullOrWhiteSpace(EditRecord.ClientName))
            {
                MessageBox.Show("고객명을 입력해 주세요.", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditRecord.Summary))
            {
                MessageBox.Show("상담 요약을 입력해 주세요.", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditingNew)
            {
                _dbService.AddRecord(EditRecord);
                StatusMessage = $"'{EditRecord.ClientName}' 님의 상담 기록이 저장되었습니다.";
            }
            else
            {
                _dbService.UpdateRecord(EditRecord);
                StatusMessage = $"'{EditRecord.ClientName}' 님의 상담 기록이 수정되었습니다.";
            }

            LoadData();
            PrepareNewRecord();
        }

        private void DeleteRecord()
        {
            long idToDelete = EditRecord.Id > 0 ? EditRecord.Id : (SelectedRecord?.Id ?? 0);
            string clientName = EditRecord.Id > 0 ? EditRecord.ClientName : (SelectedRecord?.ClientName ?? "");

            if (idToDelete == 0) return;

            var result = MessageBox.Show($"'{clientName}' 님의 상담 기록(ID: {idToDelete})을 정말 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _dbService.DeleteRecord(idToDelete);
                StatusMessage = $"상담 기록 #{idToDelete}가 삭제되었습니다.";
                LoadData();
                PrepareNewRecord();
            }
        }

        private void ExportCsv()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"상담장부_내보내기_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string fullPath = Path.Combine(desktopPath, fileName);

                ExportService.ExportToCsv(Records, fullPath);
                MessageBox.Show($"현재 검색된 {Records.Count}건의 상담 기록이 바탕화면에 저장되었습니다.\n\n파일: {fileName}", "CSV 내보내기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"CSV 내보내기 완료: {fileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"내보내기 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
