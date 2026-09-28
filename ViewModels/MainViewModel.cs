using System;
using System.Collections.Generic;
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
            set
            {
                if (SetProperty(ref _editRecord, value))
                {
                    OnPropertyChanged(nameof(EditModeTitle));
                    OnPropertyChanged(nameof(IsEditingNew));
                }
            }
        }

        private bool _isEditingNew = true;
        public bool IsEditingNew
        {
            get => _isEditingNew;
            set
            {
                if (SetProperty(ref _isEditingNew, value))
                {
                    OnPropertyChanged(nameof(EditModeTitle));
                }
            }
        }

        public string EditModeTitle => IsEditingNew
            ? "✨ 새 상담 작성"
            : $"📝 상담 기록 #{EditRecord.Id} 수정 중";

        // Quick Filter Chips: 전체, 오늘, 진행중, 재상담 예정, 완료, 긴급
        private string _currentQuickFilter = "전체";
        public string CurrentQuickFilter
        {
            get => _currentQuickFilter;
            set
            {
                if (SetProperty(ref _currentQuickFilter, value))
                {
                    OnPropertyChanged(nameof(IsFilterAll));
                    OnPropertyChanged(nameof(IsFilterToday));
                    OnPropertyChanged(nameof(IsFilterProgress));
                    OnPropertyChanged(nameof(IsFilterFollowUp));
                    OnPropertyChanged(nameof(IsFilterCompleted));
                    OnPropertyChanged(nameof(IsFilterUrgent));
                    PerformSearch();
                }
            }
        }

        public bool IsFilterAll => CurrentQuickFilter == "전체";
        public bool IsFilterToday => CurrentQuickFilter == "오늘";
        public bool IsFilterProgress => CurrentQuickFilter == "진행중";
        public bool IsFilterFollowUp => CurrentQuickFilter == "재상담 예정";
        public bool IsFilterCompleted => CurrentQuickFilter == "완료";
        public bool IsFilterUrgent => CurrentQuickFilter == "긴급";

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

        // Font Size Settings for Writing Area (작성 영역 글자 크기)
        private double _writingFontSize = 14.0;
        public double WritingFontSize
        {
            get => _writingFontSize;
            set
            {
                if (value >= 10 && value <= 28 && SetProperty(ref _writingFontSize, value))
                {
                    SaveWritingFontSize();
                }
            }
        }

        public ObservableCollection<double> AvailableFontSizes { get; } = new()
        {
            10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28
        };

        // Image Attachment Properties (그림/사진 첨부 및 크기 조절)
        private ObservableCollection<string> _attachedImages = new();
        public ObservableCollection<string> AttachedImages
        {
            get => _attachedImages;
            set
            {
                if (SetProperty(ref _attachedImages, value))
                {
                    OnPropertyChanged(nameof(HasAttachedImages));
                    OnPropertyChanged(nameof(AttachedImagesCountText));
                }
            }
        }

        public bool HasAttachedImages => AttachedImages.Count > 0;
        public string AttachedImagesCountText => $"첨부 사진 ({AttachedImages.Count}장)";

        private double _imageThumbnailSize = 85.0;
        public double ImageThumbnailSize
        {
            get => _imageThumbnailSize;
            set
            {
                if (value >= 50 && value <= 260 && SetProperty(ref _imageThumbnailSize, value))
                {
                    SaveThumbnailSize();
                }
            }
        }

        // Full-screen / Modal Image Viewer Properties
        private bool _isImageViewerOpen;
        public bool IsImageViewerOpen
        {
            get => _isImageViewerOpen;
            set => SetProperty(ref _isImageViewerOpen, value);
        }

        private string _viewingImagePath = string.Empty;
        public string ViewingImagePath
        {
            get => _viewingImagePath;
            set
            {
                if (SetProperty(ref _viewingImagePath, value))
                {
                    OnPropertyChanged(nameof(ViewingImageFileName));
                }
            }
        }

        public string ViewingImageFileName => string.IsNullOrWhiteSpace(ViewingImagePath) ? "" : Path.GetFileName(ViewingImagePath);

        private double _viewerZoomFactor = 1.0;
        public double ViewerZoomFactor
        {
            get => _viewerZoomFactor;
            set
            {
                if (SetProperty(ref _viewerZoomFactor, value))
                {
                    OnPropertyChanged(nameof(ViewerZoomPercentage));
                }
            }
        }

        public string ViewerZoomPercentage => $"{(int)(ViewerZoomFactor * 100)}%";

        // Collections for Comboboxes
        public ObservableCollection<string> Categories { get; } = new() { "전체", "일반상담", "법률/행정", "상품문의", "서비스지원", "기타" };
        public ObservableCollection<string> EditCategories { get; } = new() { "일반상담", "법률/행정", "상품문의", "서비스지원", "기타" };
        public ObservableCollection<string> Statuses { get; } = new() { "전체", "대기중", "진행중", "완료", "보류" };
        public ObservableCollection<string> EditStatuses { get; } = new() { "대기중", "진행중", "완료", "보류" };
        public ObservableCollection<string> Priorities { get; } = new() { "보통", "중요", "긴급" };

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand SetQuickFilterCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand NewRecordCommand { get; }
        public ICommand SaveRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand CheckDuplicatesCommand { get; }
        public ICommand FillExistingCustomerCommand { get; }
        public ICommand FilterHistoryForMatchedCustomerCommand { get; }
        public ICommand IncreaseFontSizeCommand { get; }
        public ICommand DecreaseFontSizeCommand { get; }
        public ICommand ResetFontSizeCommand { get; }
        public ICommand SetFollowUpDaysCommand { get; }
        public ICommand CopyPhoneCommand { get; }
        public ICommand CopyAddressCommand { get; }
        public ICommand CopyAllSummaryCommand { get; }
        public ICommand AddImageCommand { get; }
        public ICommand PasteImageCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand OpenImageViewerCommand { get; }
        public ICommand CloseImageViewerCommand { get; }
        public ICommand ZoomInViewerCommand { get; }
        public ICommand ZoomOutViewerCommand { get; }
        public ICommand ResetViewerZoomCommand { get; }
        public ICommand IncreaseThumbnailSizeCommand { get; }
        public ICommand DecreaseThumbnailSizeCommand { get; }
        public ICommand ResetThumbnailSizeCommand { get; }

        public MainViewModel()
        {
            _dbService = new DatabaseService();

            SearchCommand = new RelayCommand(PerformSearch);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
            SetQuickFilterCommand = new RelayCommand<string>(filter => CurrentQuickFilter = filter ?? "전체");
            ResetFilterCommand = new RelayCommand(ResetFilters);
            NewRecordCommand = new RelayCommand(PrepareNewRecord);
            SaveRecordCommand = new RelayCommand(SaveRecord);
            DeleteRecordCommand = new RelayCommand(DeleteRecord, () => SelectedRecord != null || EditRecord.Id > 0);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            CheckDuplicatesCommand = new RelayCommand(CheckDuplicates);
            FillExistingCustomerCommand = new RelayCommand(FillExistingCustomer);
            FilterHistoryForMatchedCustomerCommand = new RelayCommand(FilterHistoryForMatchedCustomer);
            IncreaseFontSizeCommand = new RelayCommand(IncreaseFontSize);
            DecreaseFontSizeCommand = new RelayCommand(DecreaseFontSize);
            ResetFontSizeCommand = new RelayCommand(ResetFontSize);
            SetFollowUpDaysCommand = new RelayCommand<string>(SetFollowUpDays);
            CopyPhoneCommand = new RelayCommand(CopyPhone);
            CopyAddressCommand = new RelayCommand(CopyAddress);
            CopyAllSummaryCommand = new RelayCommand(CopyAllSummary);

            // Image Commands
            AddImageCommand = new RelayCommand(AddImagesFromDialog);
            PasteImageCommand = new RelayCommand(PasteImageFromClipboard);
            RemoveImageCommand = new RelayCommand<string>(RemoveImage);
            OpenImageViewerCommand = new RelayCommand<string>(OpenImageViewer);
            CloseImageViewerCommand = new RelayCommand(CloseImageViewer);
            ZoomInViewerCommand = new RelayCommand(ZoomInViewer);
            ZoomOutViewerCommand = new RelayCommand(ZoomOutViewer);
            ResetViewerZoomCommand = new RelayCommand(ResetViewerZoom);
            IncreaseThumbnailSizeCommand = new RelayCommand(IncreaseThumbnailSize);
            DecreaseThumbnailSizeCommand = new RelayCommand(DecreaseThumbnailSize);
            ResetThumbnailSizeCommand = new RelayCommand(ResetThumbnailSize);

            LoadWritingFontSize();
            LoadThumbnailSize();
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
                string customerLabel = !string.IsNullOrWhiteSpace(MatchedRecord.ClientName)
                    ? $"'{MatchedRecord.ClientName}' 님"
                    : $"연락처 '{MatchedRecord.ClientPhone}'";
                ExistingMatchMessage = $"💡 일치 안내: {customerLabel}과 일치하는 이전 상담 {matches.Count}건 보관 중 (최근: {MatchedRecord.FormattedDate})";
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

            string label = !string.IsNullOrWhiteSpace(MatchedRecord.ClientName) ? $"'{MatchedRecord.ClientName}' 님" : $"연락처 '{MatchedRecord.ClientPhone}'";
            StatusMessage = $"{label}의 기존 인적사항이 자동 완성되었습니다.";
        }

        private void FilterHistoryForMatchedCustomer()
        {
            if (MatchedRecord == null) return;
            SearchText = !string.IsNullOrWhiteSpace(MatchedRecord.ClientPhone)
                ? MatchedRecord.ClientPhone
                : (!string.IsNullOrWhiteSpace(MatchedRecord.ClientName) ? MatchedRecord.ClientName : MatchedRecord.Address);
            PerformSearch();
            string label = !string.IsNullOrWhiteSpace(MatchedRecord.ClientName) ? $"'{MatchedRecord.ClientName}' 님" : $"연락처 '{MatchedRecord.ClientPhone}'";
            StatusMessage = $"{label}의 이전 상담 이력을 조회했습니다.";
        }

        public void LoadData()
        {
            PerformSearch();
            RefreshStats();
        }

        private void PerformSearch()
        {
            var results = _dbService.GetFilteredRecords(
                SearchText, 
                SelectedCategoryFilter, 
                SelectedStatusFilter, 
                StartDateFilter, 
                EndDateFilter,
                CurrentQuickFilter);

            for (int i = 0; i < results.Count; i++)
            {
                results[i].RowIndex = i + 1;
            }
            Records = new ObservableCollection<ConsultationRecord>(results);
            StatusMessage = $"조회 결과: 총 {Records.Count}건 ({CurrentQuickFilter})";
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
            _currentQuickFilter = "전체";
            OnPropertyChanged(nameof(CurrentQuickFilter));
            OnPropertyChanged(nameof(IsFilterAll));
            OnPropertyChanged(nameof(IsFilterToday));
            OnPropertyChanged(nameof(IsFilterProgress));
            OnPropertyChanged(nameof(IsFilterFollowUp));
            OnPropertyChanged(nameof(IsFilterCompleted));
            OnPropertyChanged(nameof(IsFilterUrgent));

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
            AttachedImages.Clear();
            OnPropertyChanged(nameof(HasAttachedImages));
            OnPropertyChanged(nameof(AttachedImagesCountText));
            IsEditingNew = true;
            SelectedRecord = null;
            HasExistingCustomerMatch = false;
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
                ImagePaths = record.ImagePaths,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt
            };

            AttachedImages.Clear();
            if (!string.IsNullOrWhiteSpace(record.ImagePaths))
            {
                var paths = record.ImagePaths.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in paths)
                {
                    if (File.Exists(p))
                    {
                        AttachedImages.Add(p);
                    }
                }
            }
            OnPropertyChanged(nameof(HasAttachedImages));
            OnPropertyChanged(nameof(AttachedImagesCountText));

            IsEditingNew = false;
            HasExistingCustomerMatch = false;
            string targetTitle = !string.IsNullOrWhiteSpace(record.ClientName) ? record.ClientName : (!string.IsNullOrWhiteSpace(record.ClientPhone) ? record.ClientPhone : record.Address);
            StatusMessage = $"상담 기록 #{record.Id} ({targetTitle}) 수정 모드";
        }

        private void SaveRecord()
        {
            // Auto-format phone before validating
            if (!string.IsNullOrWhiteSpace(EditRecord.ClientPhone))
            {
                EditRecord.ClientPhone = ConsultationRecord.FormatPhoneNumber(EditRecord.ClientPhone);
            }

            if (string.IsNullOrWhiteSpace(EditRecord.ClientPhone))
            {
                MessageBox.Show("연락처(전화번호)를 입력해 주세요. (필수 항목)", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditRecord.Address))
            {
                MessageBox.Show("주소(소재지/배송지)를 입력해 주세요. (필수 항목)", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditRecord.Summary))
            {
                MessageBox.Show("상담 요약을 입력해 주세요. (필수 항목)", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save attached images
            EditRecord.ImagePaths = string.Join(";", AttachedImages);

            string clientIdentifier = !string.IsNullOrWhiteSpace(EditRecord.ClientName)
                ? $"'{EditRecord.ClientName}' 님"
                : $"연락처 '{EditRecord.ClientPhone}'";

            if (IsEditingNew)
            {
                _dbService.AddRecord(EditRecord);
                StatusMessage = $"{clientIdentifier}의 상담 기록이 저장되었습니다.";
            }
            else
            {
                _dbService.UpdateRecord(EditRecord);
                StatusMessage = $"{clientIdentifier}의 상담 기록이 수정되었습니다.";
            }

            LoadData();
            PrepareNewRecord();
        }

        private void DeleteRecord()
        {
            long idToDelete = EditRecord.Id > 0 ? EditRecord.Id : (SelectedRecord?.Id ?? 0);
            string clientName = EditRecord.Id > 0 ? EditRecord.ClientName : (SelectedRecord?.ClientName ?? "");
            string displayName = !string.IsNullOrWhiteSpace(clientName) ? $"'{clientName}' 님" : "해당";

            if (idToDelete == 0) return;

            var result = MessageBox.Show($"{displayName}의 상담 기록(ID: {idToDelete})을 정말 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _dbService.DeleteRecord(idToDelete);
                StatusMessage = $"상담 기록 #{idToDelete}가 삭제되었습니다.";
                LoadData();
                PrepareNewRecord();
            }
        }

        private void SetFollowUpDays(string? daysStr)
        {
            if (int.TryParse(daysStr, out int days))
            {
                if (days < 0)
                {
                    EditRecord.FollowUpDate = null;
                    StatusMessage = "재상담 일정이 해제되었습니다.";
                }
                else
                {
                    EditRecord.FollowUpDate = DateTime.Today.AddDays(days);
                    string label = days == 0 ? "오늘" : $"{days}일 후 ({EditRecord.FollowUpDate.Value:MM-dd})";
                    StatusMessage = $"재상담 예정일이 {label}로 설정되었습니다.";
                }

                // Trigger property change on EditRecord
                var temp = EditRecord;
                EditRecord = null!;
                EditRecord = temp;
            }
        }

        private void CopyPhone()
        {
            if (!string.IsNullOrWhiteSpace(EditRecord.ClientPhone))
            {
                Clipboard.SetText(EditRecord.ClientPhone);
                StatusMessage = $"연락처 '{EditRecord.ClientPhone}'가 클립보드에 복사되었습니다. 📋";
            }
            else
            {
                StatusMessage = "복사할 연락처가 없습니다.";
            }
        }

        private void CopyAddress()
        {
            if (!string.IsNullOrWhiteSpace(EditRecord.Address))
            {
                Clipboard.SetText(EditRecord.Address);
                StatusMessage = $"주소 '{EditRecord.Address}'가 클립보드에 복사되었습니다. 📋";
            }
            else
            {
                StatusMessage = "복사할 주소가 없습니다.";
            }
        }

        private void CopyAllSummary()
        {
            string name = string.IsNullOrWhiteSpace(EditRecord.ClientName) ? "(미기재)" : EditRecord.ClientName;
            string phone = string.IsNullOrWhiteSpace(EditRecord.ClientPhone) ? "-" : EditRecord.ClientPhone;
            string addr = string.IsNullOrWhiteSpace(EditRecord.Address) ? "-" : EditRecord.Address;
            string followUp = EditRecord.FollowUpDate.HasValue ? EditRecord.FollowUpDate.Value.ToString("yyyy-MM-dd") : "없음";

            string text = $@"📋 [상담 기록 공유]
• 고객명: {name}
• 연락처: {phone}
• 주소: {addr}
• 일시: {EditRecord.ConsultationDate:yyyy-MM-dd HH:mm}
• 구분/상태: {EditRecord.Category} / {EditRecord.Status} (우선순위: {EditRecord.Priority})
• 재상담일: {followUp}
• 상담 요약: {EditRecord.Summary}
• 상세 내용:
{EditRecord.Details}";

            Clipboard.SetText(text);
            StatusMessage = "상담 내용 전체가 메신저 공유용으로 클립보드에 복사되었습니다. 📋";
        }

        private void ExportCsv()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string fileName = $"상담장부_추출_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string fullPath = Path.Combine(desktopPath, fileName);

                ExportService.ExportToCsv(Records, fullPath);
                StatusMessage = $"CSV 내보내기 완료: 바탕화면/{fileName}";

                var result = MessageBox.Show($"바탕화면에 파일이 저장되었습니다.\n\n파일명: {fileName}\n\n파일이 있는 폴더를 여시겠습니까?", "내보내기 완료", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 내보내기 중 오류가 발생했습니다.\n\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "CSV 내보내기 실패";
            }
        }

        private void IncreaseFontSize()
        {
            if (WritingFontSize < 28)
            {
                WritingFontSize += 1.0;
                StatusMessage = $"작성 글자 크기: {WritingFontSize}pt";
            }
        }

        private void DecreaseFontSize()
        {
            if (WritingFontSize > 10)
            {
                WritingFontSize -= 1.0;
                StatusMessage = $"작성 글자 크기: {WritingFontSize}pt";
            }
        }

        private void ResetFontSize()
        {
            WritingFontSize = 14.0;
            StatusMessage = "작성 글자 크기가 기본값(14pt)으로 재설정되었습니다.";
        }

        private string GetFontSizeSettingsPath()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
            return Path.Combine(appDataPath, "editor_font_size.txt");
        }

        private void LoadWritingFontSize()
        {
            try
            {
                string path = GetFontSizeSettingsPath();
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path).Trim();
                    if (double.TryParse(text, out double size) && size >= 10 && size <= 28)
                    {
                        _writingFontSize = size;
                        OnPropertyChanged(nameof(WritingFontSize));
                    }
                }
            }
            catch
            {
                _writingFontSize = 14.0;
            }
        }

        private void SaveWritingFontSize()
        {
            try
            {
                string path = GetFontSizeSettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, WritingFontSize.ToString());
            }
            catch
            {
                // Silently fallback
            }
        }

        private void AddImagesFromDialog()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "첨부할 이미지 선택 (다중 선택 가능)",
                Filter = "이미지 파일 (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|모든 파일 (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true && dlg.FileNames != null && dlg.FileNames.Length > 0)
            {
                AddImageFiles(dlg.FileNames);
            }
        }

        public void AddImageFiles(IEnumerable<string> filePaths)
        {
            int addedCount = 0;
            string[] supportedExts = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

            foreach (var file in filePaths)
            {
                if (File.Exists(file))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (supportedExts.Contains(ext))
                    {
                        try
                        {
                            string storedPath = DatabaseService.SaveImageToAppStorage(file);
                            AttachedImages.Add(storedPath);
                            addedCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"이미지 저장 중 오류가 발생했습니다: {Path.GetFileName(file)}\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }

            if (addedCount > 0)
            {
                OnPropertyChanged(nameof(HasAttachedImages));
                OnPropertyChanged(nameof(AttachedImagesCountText));
                StatusMessage = $"사진 {addedCount}장이 첨부되었습니다. 📷";
            }
        }

        public void PasteImageFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        string storedPath = DatabaseService.SaveBitmapSourceToAppStorage(bitmap);
                        AttachedImages.Add(storedPath);
                        OnPropertyChanged(nameof(HasAttachedImages));
                        OnPropertyChanged(nameof(AttachedImagesCountText));
                        StatusMessage = "클립보드에서 이미지가 붙여넣기 되었습니다. 📋📷";
                        return;
                    }
                }

                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files != null && files.Count > 0)
                    {
                        var fileList = new List<string>();
                        foreach (string? f in files)
                        {
                            if (!string.IsNullOrWhiteSpace(f)) fileList.Add(f);
                        }
                        AddImageFiles(fileList);
                        return;
                    }
                }

                StatusMessage = "클립보드에 붙여넣을 수 있는 이미지나 이미지 파일이 없습니다.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"클립보드 이미지 가져오기 실패: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (AttachedImages.Contains(path))
            {
                AttachedImages.Remove(path);
                OnPropertyChanged(nameof(HasAttachedImages));
                OnPropertyChanged(nameof(AttachedImagesCountText));
                StatusMessage = "첨부 사진이 목록에서 제거되었습니다.";
                if (IsImageViewerOpen && ViewingImagePath == path)
                {
                    CloseImageViewer();
                }
            }
        }

        private void OpenImageViewer(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            ViewingImagePath = path;
            ViewerZoomFactor = 1.0;
            IsImageViewerOpen = true;
        }

        private void CloseImageViewer()
        {
            IsImageViewerOpen = false;
            ViewingImagePath = string.Empty;
        }

        private void ZoomInViewer()
        {
            if (ViewerZoomFactor < 4.0)
            {
                ViewerZoomFactor = Math.Round(ViewerZoomFactor + 0.25, 2);
            }
        }

        private void ZoomOutViewer()
        {
            if (ViewerZoomFactor > 0.3)
            {
                ViewerZoomFactor = Math.Round(ViewerZoomFactor - 0.25, 2);
            }
        }

        private void ResetViewerZoom()
        {
            ViewerZoomFactor = 1.0;
        }

        private void IncreaseThumbnailSize()
        {
            if (ImageThumbnailSize <= 240)
            {
                ImageThumbnailSize += 20;
                StatusMessage = $"사진 썸네일 크기: {(int)ImageThumbnailSize}px";
            }
        }

        private void DecreaseThumbnailSize()
        {
            if (ImageThumbnailSize >= 70)
            {
                ImageThumbnailSize -= 20;
                StatusMessage = $"사진 썸네일 크기: {(int)ImageThumbnailSize}px";
            }
        }

        private void ResetThumbnailSize()
        {
            ImageThumbnailSize = 85.0;
            StatusMessage = "사진 썸네일 크기가 기본값(85px)으로 재설정되었습니다.";
        }

        private string GetThumbnailSizeSettingsPath()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
            return Path.Combine(appDataPath, "image_thumb_size.txt");
        }

        private void LoadThumbnailSize()
        {
            try
            {
                string path = GetThumbnailSizeSettingsPath();
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path).Trim();
                    if (double.TryParse(text, out double size) && size >= 50 && size <= 260)
                    {
                        _imageThumbnailSize = size;
                        OnPropertyChanged(nameof(ImageThumbnailSize));
                    }
                }
            }
            catch
            {
                _imageThumbnailSize = 85.0;
            }
        }

        private void SaveThumbnailSize()
        {
            try
            {
                string path = GetThumbnailSizeSettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, ImageThumbnailSize.ToString());
            }
            catch
            {
                // Silently fallback
            }
        }
    }
}
