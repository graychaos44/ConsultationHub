using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ConsultationLedger
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register global unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("DispatcherUnhandledException", e.Exception);
            MessageBox.Show($"예기치 않은 오류가 발생했습니다:\n\n{e.Exception.Message}\n\n자세한 내용은 로그를 확인하세요.", 
                            "오류 발생", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app crash
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("UnhandledException", ex);
                MessageBox.Show($"심각한 오류가 발생했습니다:\n\n{ex.Message}", 
                                "치명적 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            LogError("UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void LogError(string context, Exception ex)
        {
            try
            {
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsultationLedger");
                Directory.CreateDirectory(appDataPath);
                string logFile = Path.Combine(appDataPath, "error.log");
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex}\n----------------------------------------\n";
                File.AppendAllText(logFile, logMessage);
            }
            catch
            {
                // Ignore logging failures
            }
        }
    }
}
