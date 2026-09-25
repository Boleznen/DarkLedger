using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DarkLedger.WPF.Views
{
    public partial class DarkMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        public DarkMessageBoxWindow(string text, string caption,
            MessageBoxButton buttons, MessageBoxImage icon)
        {
            InitializeComponent();

            TitleText.Text = string.IsNullOrEmpty(caption) ? "Dark Ledger" : caption;
            Title = TitleText.Text;
            MessageText.Text = text;

            // Иконка
            IconText.Text = GetIconChar(icon);
            IconText.Foreground = GetIconBrush(icon);

            // Кнопки
            BuildButtons(buttons);
        }

        // ==================== TITLE BAR ====================

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }

        // ==================== ИКОНКА ====================

        private static string GetIconChar(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Information => "ℹ️",
                MessageBoxImage.Warning => "⚠️",
                MessageBoxImage.Error => "❌",
                MessageBoxImage.Question => "❓",
                _ => "ℹ️"
            };
        }

        private static Brush GetIconBrush(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Warning => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                MessageBoxImage.Error => new SolidColorBrush(Color.FromRgb(0x8C, 0x14, 0x1E)),
                MessageBoxImage.Question => new SolidColorBrush(Color.FromRgb(0x4B, 0x28, 0x6E)),
                _ => new SolidColorBrush(Color.FromRgb(0x1E, 0x32, 0x6E))
            };
        }

        // ==================== КНОПКИ ====================

        private void BuildButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, isAccent: true);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("OK", MessageBoxResult.OK, isAccent: true);
                    AddButton("Отмена", MessageBoxResult.Cancel);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("Да", MessageBoxResult.Yes, isAccent: true);
                    AddButton("Нет", MessageBoxResult.No);
                    break;
                case MessageBoxButton.YesNoCancel:
                    AddButton("Да", MessageBoxResult.Yes, isAccent: true);
                    AddButton("Нет", MessageBoxResult.No);
                    AddButton("Отмена", MessageBoxResult.Cancel);
                    break;
                default:
                    AddButton("OK", MessageBoxResult.OK, isAccent: true);
                    break;
            }
        }

        private void AddButton(string text, MessageBoxResult result, bool isAccent = false)
        {
            var btn = new Button
            {
                Content = text,
                Width = 100,
                Height = 32,
                Margin = new Thickness(8, 0, 0, 0),
                Style = (Style)FindResource(isAccent ? "AccentButton" : "DarkButton")
            };

            btn.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
                Close();
            };

            ButtonPanel.Children.Add(btn);
        }
    }
}