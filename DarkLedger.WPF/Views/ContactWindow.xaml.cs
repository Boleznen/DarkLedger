using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DarkLedger.WPF.Views
{
    public partial class ContactWindow : Window
    {
        public ContactWindow()
        {
            InitializeComponent();
            LoadSystemInfo();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void LoadSystemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ОС: {Environment.OSVersion}");
            sb.AppendLine($".NET: {Environment.Version}");
            sb.AppendLine($"Пользователь: {Environment.UserName}");
            sb.AppendLine($"Машина: {Environment.MachineName}");
            sb.AppendLine($"64-bit: {(Environment.Is64BitOperatingSystem ? "Да" : "Нет")}");
            sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Папка данных: {SettingsManager.GetDataFolderPath()}");

            InfoText.Text = sb.ToString();
        }

        private void TelegramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://t.me/Boleznen",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть Telegram", ex);
                DarkMessageBox.ShowWarning($"Не удалось открыть Telegram.\n\nПричина: {ex.Message}", "Ошибка");
            }
        }

        private void CopyLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logs = Logger.GetCurrentLogContents();
                if (string.IsNullOrWhiteSpace(logs) || logs.StartsWith("Записей"))
                {
                    DarkMessageBox.ShowInfo("Журнал текущей сессии пуст.", "Связь с разработчиком");
                    return;
                }

                Clipboard.SetText(logs);
                DarkMessageBox.ShowInfo("Логи скопированы в буфер обмена.", "Готово");
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось скопировать логи", ex);
                DarkMessageBox.ShowWarning($"Не удалось скопировать логи.\n\nПричина: {ex.Message}", "Ошибка");
            }
        }

        private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Logger.GetLogsFolderPath();
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть папку логов", ex);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}