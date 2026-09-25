using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.SolidColorBrush;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Иконка в системном трее + контекстное меню.
    /// Использует Hardcodet.NotifyIcon.Wpf (WPF-версия NotifyIcon).
    /// </summary>
    internal sealed class TrayManager : IDisposable
    {
        private TaskbarIcon? _trayIcon;
        private bool _disposed;

        public event Action? OnShowRequested;
        public event Action? OnSettingsRequested;
        public event Action? OnExitRequested;
        public event Action? OnToggleTrackingRequested;

        public bool IsVisible => _trayIcon?.Visibility == Visibility.Visible;

        public void Initialize()
        {
            try
            {
                _trayIcon = new TaskbarIcon
                {
                    ToolTipText = "Dark Ledger — Тёмный реестр",
                    Visibility = Visibility.Collapsed
                };

                // Иконка
                try
                {
                    var icon = LoadAppIcon();
                    if (icon != null)
                        _trayIcon.Icon = icon;
                }
                catch (Exception ex)
                {
                    Logger.Error("Не удалось загрузить иконку для трея", ex);
                }

                // Контекстное меню
                var menu = new ContextMenu
                {
                    Background = new MediaBrush(MediaColor.FromRgb(0x23, 0x20, 0x2D)),
                    Foreground = new MediaBrush(MediaColor.FromRgb(0xE6, 0xE6, 0xF0)),
                    BorderBrush = new MediaBrush(MediaColor.FromRgb(0x37, 0x32, 0x4B)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4)
                };

                menu.Items.Add(MakeMenuItem("Открыть Dark Ledger", () => OnShowRequested?.Invoke()));
                menu.Items.Add(MakeMenuItem("Пауза / Возобновить", () => OnToggleTrackingRequested?.Invoke()));
                menu.Items.Add(MakeSeparator());
                menu.Items.Add(MakeMenuItem("⚙️ Настройки", () => OnSettingsRequested?.Invoke()));
                menu.Items.Add(MakeSeparator());
                menu.Items.Add(MakeMenuItem("Выход", () => OnExitRequested?.Invoke(),
                    MediaColor.FromRgb(0x8C, 0x14, 0x1E)));

                _trayIcon.ContextMenu = menu;
                _trayIcon.TrayMouseDoubleClick += (s, e) => OnShowRequested?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка создания иконки в трее", ex);
            }
        }

        // ==================== ПУНКТЫ МЕНЮ ====================

        private static MenuItem MakeMenuItem(string header, Action onClick, MediaColor? foreColor = null)
        {
            var item = new MenuItem
            {
                Header = header,
                Foreground = new MediaBrush(
                    foreColor ?? MediaColor.FromRgb(0xE6, 0xE6, 0xF0)),
                Background = new MediaBrush(MediaColor.FromRgb(0x23, 0x20, 0x2D)),
                Padding = new Thickness(12, 6, 12, 6)
            };

            var template = new ControlTemplate(typeof(MenuItem));

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Bd";
            border.SetValue(Border.BackgroundProperty,
                new TemplateBindingExtension(MenuItem.BackgroundProperty));
            border.SetValue(Border.PaddingProperty,
                new TemplateBindingExtension(MenuItem.PaddingProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);
            template.VisualTree = border;

            var hover = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty,
                new MediaBrush(MediaColor.FromRgb(0x5A, 0x0F, 0x19)), "Bd"));

            template.Triggers.Add(hover);

            item.Template = template;
            item.Click += (s, e) => onClick();
            return item;
        }

        private static Separator MakeSeparator()
        {
            return new Separator
            {
                Background = new MediaBrush(MediaColor.FromRgb(0x37, 0x32, 0x4B)),
                Height = 1,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        // ==================== ЗАГРУЗКА ИКОНКИ ====================

        private static Icon? LoadAppIcon()
        {
            // 1) Из .exe
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    var extracted = Icon.ExtractAssociatedIcon(exePath);
                    if (extracted != null) return extracted;
                }
            }
            catch { }

            // 2) рядом с .exe
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "darkledger.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch { }

            // 3) в корне проекта
            try
            {
                string projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "darkledger.ico");
                projectPath = Path.GetFullPath(projectPath);
                if (File.Exists(projectPath))
                    return new Icon(projectPath);
            }
            catch { }

            return DrawFallbackIcon();
        }

        private static Icon DrawFallbackIcon()
        {
            try
            {
                using var bmp = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(System.Drawing.Color.Transparent);

                    using var bg = new SolidBrush(System.Drawing.Color.FromArgb(0x19, 0x19, 0x23));
                    g.FillEllipse(bg, 0, 0, 31, 31);

                    using var accent = new SolidBrush(System.Drawing.Color.FromArgb(0x8C, 0x14, 0x1E));
                    g.FillEllipse(accent, 8, 8, 15, 15);
                }

                return Icon.FromHandle(bmp.GetHicon());
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        // ==================== ПОКАЗ / СКРЫТИЕ ====================

        public void Show()
        {
            if (_trayIcon != null) _trayIcon.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            if (_trayIcon != null) _trayIcon.Visibility = Visibility.Collapsed;
        }

        public void ShowNotification(string title, string text,
            BalloonIcon icon = BalloonIcon.Info, int timeoutMs = 3000)
        {
            try { _trayIcon?.ShowBalloonTip(title, text, icon); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _trayIcon?.Dispose();
                _trayIcon = null;
            }
            catch { }
        }
    }
}