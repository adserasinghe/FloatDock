using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FloatDock.Native;
using FloatDock.Services;
using Point = System.Windows.Point;

namespace FloatDock.Views
{
    public partial class ScreenshotSnippingWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly Action<Bitmap>? _onCaptureComplete;
        private Bitmap _screenFreezeBitmap;

        private enum SnippingMode { Region, Window, Monitor, Virtual }
        private SnippingMode _activeMode = SnippingMode.Region;

        private bool _isSelecting;
        private Point _startPoint;

        public ScreenshotSnippingWindow(SettingsService settingsService, Action<Bitmap>? onCaptureComplete = null)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _onCaptureComplete = onCaptureComplete;

            // Immediately capture the undisturbed screen before the window displays
            _screenFreezeBitmap = ScreenshotService.CaptureVirtualScreen();
            BackgroundScreenshot.Source = ScreenshotService.ToBitmapSource(_screenFreezeBitmap);

            Loaded += ScreenshotSnippingWindow_Loaded;
        }

        private void ScreenshotSnippingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            Top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            Width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            Height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
        }

        private void BtnModeRegion_Click(object sender, RoutedEventArgs e) => SetMode(SnippingMode.Region);
        private void BtnModeWindow_Click(object sender, RoutedEventArgs e)
        {
            SetMode(SnippingMode.Window);
            CaptureAndFinish(ScreenshotService.CaptureActiveWindow() ?? _screenFreezeBitmap);
        }
        private void BtnModeMonitor_Click(object sender, RoutedEventArgs e)
        {
            SetMode(SnippingMode.Monitor);
            var helper = new WindowInteropHelper(this);
            CaptureAndFinish(ScreenshotService.CaptureCurrentMonitor(helper.Handle));
        }
        private void BtnModeVirtual_Click(object sender, RoutedEventArgs e)
        {
            SetMode(SnippingMode.Virtual);
            CaptureAndFinish(_screenFreezeBitmap);
        }

        private void SetMode(SnippingMode mode)
        {
            _activeMode = mode;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _activeMode == SnippingMode.Region)
            {
                _isSelecting = true;
                _startPoint = e.GetPosition(this);

                SelectionBorder.Width = 0;
                SelectionBorder.Height = 0;
                SelectionBorder.Visibility = Visibility.Visible;
                DimensionHud.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && _activeMode == SnippingMode.Region)
            {
                Point currentPoint = e.GetPosition(this);

                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(currentPoint.X - _startPoint.X);
                double h = Math.Abs(currentPoint.Y - _startPoint.Y);

                SelectionBorder.Margin = new Thickness(x, y, 0, 0);
                SelectionBorder.Width = w;
                SelectionBorder.Height = h;

                // Position HUD slightly below or above cursor
                DimensionHud.Margin = new Thickness(x + 8, y + h + 8, 0, 0);
                TxtDimensionHud.Text = $"{(int)w} × {(int)h} px";
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _activeMode == SnippingMode.Region)
            {
                _isSelecting = false;
                Point endPoint = e.GetPosition(this);

                int x = (int)Math.Min(_startPoint.X, endPoint.X);
                int y = (int)Math.Min(_startPoint.Y, endPoint.Y);
                int w = (int)Math.Abs(endPoint.X - _startPoint.X);
                int h = (int)Math.Abs(endPoint.Y - _startPoint.Y);

                if (w > 4 && h > 4)
                {
                    var cropped = ScreenshotService.CropBitmap(_screenFreezeBitmap, new Rectangle(x, y, w, h));
                    CaptureAndFinish(cropped);
                }
                else
                {
                    SelectionBorder.Visibility = Visibility.Collapsed;
                    DimensionHud.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CaptureAndFinish(Bitmap bitmap)
        {
            Hide();

            if (_settingsService.CurrentSettings.CopyScreenshotToClipboard)
            {
                ScreenshotService.CopyToClipboard(bitmap);
            }

            string savedPath = string.Empty;
            if (_settingsService.CurrentSettings.AutoSaveScreenshotToDisk)
            {
                savedPath = ScreenshotService.SaveToDisk(
                    bitmap,
                    _settingsService.CurrentSettings.ScreenshotDirectory,
                    _settingsService.CurrentSettings.ScreenshotFormat
                );
            }

            _onCaptureComplete?.Invoke(bitmap);

            // Display non-blocking toast notification
            ShowCaptureToast(savedPath);

            Close();
        }

        private void ShowCaptureToast(string savedPath)
        {
            // Simple toast prompt: Open folder or view
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                var result = MessageBox.Show(
                    $"Screenshot captured!\nCopied to clipboard and saved to:\n{savedPath}\n\nWould you like to open the folder?",
                    "FloatDock Screenshot",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start("explorer.exe", $"/select,\"{savedPath}\"");
                    }
                    catch { }
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _screenFreezeBitmap.Dispose();
        }
    }
}
