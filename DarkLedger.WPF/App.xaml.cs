using System;
using System.Windows;
using System.Windows.Threading;

namespace DarkLedger.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальный обработчик необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            Logger.Info("═══════════════════════════════════════");
            Logger.Info("Dark Ledger запущен");
            Logger.Info($"Версия: {UpdateChecker.GetCurrentVersion()}");
            Logger.Info($"ОС: {Environment.OSVersion}");
            Logger.Info($".NET: {Environment.Version}");

            // Загружаем настройки (создаст файл, если его нет)
            var settings = SettingsManager.Current;
            Logger.Info($"Настройки загружены: PollingInterval={settings.PollingIntervalMs}ms, " +
                        $"IdleThreshold={settings.IdleThresholdSeconds}s, " +
                        $"TrackIdle={settings.TrackIdle}");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Dark Ledger завершён");
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("Необработанное исключение", e.Exception);

            DarkMessageBox.ShowError(
                $"Произошла непредвиденная ошибка:\n\n{e.Exception.Message}\n\n" +
                "Подробности записаны в лог.",
                "Ошибка");

            e.Handled = true;
        }
    }
}