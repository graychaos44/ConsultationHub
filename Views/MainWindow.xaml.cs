using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsultationLedger.Models;
using ConsultationLedger.ViewModels;

namespace ConsultationLedger.Views
{
    public partial class MainWindow : Window
    {
        private const string LayoutSettingsFileName = "layout_settings.txt";
        private const double DefaultLeftWidth = 430.0;
        private double _savedLeftWidth = DefaultLeftWidth;
        private bool _isMasterCollapsed = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadLayoutWidth();
                Activate();
                Focus();
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveLayoutWidth();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + F: Focus Search Box
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_isMasterCollapsed)
                {
                    ToggleMasterList();
                }
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            // F11: Toggle Full Writing Mode (Collapse/Expand List)
            else if (e.Key == Key.F11)
            {
                ToggleMasterList();
                e.Handled = true;
            }
            // F5: Refresh Data
            else if (e.Key == Key.F5)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.LoadData();
                    e.Handled = true;
                }
            }
        }

        private void OnToggleCollapseMasterListClicked(object sender, RoutedEventArgs e)
        {
            ToggleMasterList();
        }

        private void ToggleMasterList()
        {
            if (!_isMasterCollapsed)
            {
                // Collapse Left Panel
                if (LeftColumn.Width.Value > 200)
                {
                    _savedLeftWidth = LeftColumn.Width.Value;
                }
                LeftColumn.MinWidth = 0;
                LeftColumn.Width = new GridLength(0);
                SplitterColumn.Width = new GridLength(0);
                _isMasterCollapsed = true;
                ToggleCollapseButton.Content = "▶ 목록 열기";
                ToggleCollapseButton.ToolTip = "왼쪽 상담 목록을 다시 표시합니다 (단축키: F11)";

                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = "상담 집중 작성 모드 (목록 숨김)";
                }
            }
            else
            {
                // Expand Left Panel
                LeftColumn.MinWidth = 220;
                LeftColumn.Width = new GridLength(_savedLeftWidth > 200 ? _savedLeftWidth : DefaultLeftWidth);
                SplitterColumn.Width = new GridLength(10);
                _isMasterCollapsed = false;
                ToggleCollapseButton.Content = "◀ 목록 접기";
                ToggleCollapseButton.ToolTip = "왼쪽 상담 목록을 접어 상담 작성 공간을 전체화면으로 넓힙니다 (단축키: F11)";

                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = "상담 목록 표시 모드";
                }
            }

            SaveLayoutWidth();
        }

        private void OnSplitterDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            // Reset to default optimal width on double-click
            LeftColumn.MinWidth = 220;
            LeftColumn.Width = new GridLength(DefaultLeftWidth);
            _savedLeftWidth = DefaultLeftWidth;
            if (_isMasterCollapsed)
            {
                SplitterColumn.Width = new GridLength(10);
                _isMasterCollapsed = false;
                ToggleCollapseButton.Content = "◀ 목록 접기";
            }
            SaveLayoutWidth();

            if (DataContext is MainViewModel vm)
            {
                vm.StatusMessage = "좌우 분할 비율이 기본 크기로 복원되었습니다.";
            }
        }

        private void SaveLayoutWidth()
        {
            try
            {
                if (!_isMasterCollapsed && LeftColumn.Width.Value >= 200)
                {
                    _savedLeftWidth = LeftColumn.Width.Value;
                }

                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
                Directory.CreateDirectory(appDataPath);
                string path = Path.Combine(appDataPath, LayoutSettingsFileName);
                File.WriteAllText(path, $"{_savedLeftWidth:F0}|False");
            }
            catch
            {
                // Silently fallback
            }
        }

        private void LoadLayoutWidth()
        {
            try
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
                string path = Path.Combine(appDataPath, LayoutSettingsFileName);
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path).Trim();
                    string[] parts = content.Split('|');
                    if (parts.Length > 0 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double width))
                    {
                        if (width >= 200 && width <= 900)
                        {
                            _savedLeftWidth = width;
                            LeftColumn.Width = new GridLength(width);
                        }
                    }

                    // Always start with master list expanded so user immediately sees their records
                    _isMasterCollapsed = false;
                    ToggleCollapseButton.Content = "◀ 목록 접기";
                    ToggleCollapseButton.ToolTip = "왼쪽 상담 목록을 접어 상담 작성 공간을 전체화면으로 넓힙니다 (단축키: F11)";
                }
            }
            catch
            {
                // Default width
            }
        }

        private void OnCustomerInfoInputChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CheckDuplicates();
            }
        }

        private void OnPhoneTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm)
            {
                string raw = tb.Text;
                string formatted = ConsultationRecord.FormatPhoneNumber(raw);
                if (raw != formatted)
                {
                    vm.EditRecord.ClientPhone = formatted;
                    tb.Text = formatted;
                }
            }
        }

        private void OnWritingTextBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (DataContext is MainViewModel vm)
                {
                    if (e.Delta > 0)
                    {
                        vm.IncreaseFontSizeCommand.Execute(null);
                    }
                    else if (e.Delta < 0)
                    {
                        vm.DecreaseFontSizeCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
