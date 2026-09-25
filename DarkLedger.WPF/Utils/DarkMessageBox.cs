using System.Windows;
using DarkLedger.WPF.Views;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Тёмный MessageBox. Использовать так же, как стандартный:
    ///   DarkMessageBox.ShowInfo("Текст", "Заголовок");
    ///   DarkMessageBox.ShowQuestion("Удалить?", "Подтверждение");
    /// </summary>
    internal static class DarkMessageBox
    {
        public static MessageBoxResult ShowInfo(string text, string caption)
            => Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);

        public static MessageBoxResult ShowWarning(string text, string caption)
            => Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Warning);

        public static MessageBoxResult ShowError(string text, string caption)
            => Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult ShowQuestion(string text, string caption)
            => Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);

        public static MessageBoxResult Show(string text, string caption,
            MessageBoxButton buttons, MessageBoxImage icon)
        {
            var owner = Application.Current?.MainWindow;
            var window = new DarkMessageBoxWindow(text, caption, buttons, icon);

            if (owner != null && owner.IsLoaded && owner.IsVisible)
                window.Owner = owner;

            window.ShowDialog();
            return window.Result;
        }
    }
}