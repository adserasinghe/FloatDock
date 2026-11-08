using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FloatDock.Models;
using FloatDock.Services;
using Microsoft.Win32;

namespace FloatDock.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "FloatDock";

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            var s = _settingsService.CurrentSettings;

            // Theme
            foreach (ComboBoxItem item in CmbTheme.Items)
            {
                if (item.Tag?.ToString() == s.Theme)
                {
                    CmbTheme.SelectedItem = item;
                    break;
                }
            }

            // Opacity
            SliderOpacity.Value = s.Opacity;
            TxtOpacityVal.Text = $"{(int)(s.Opacity * 100)}%";

            // Auto-collapse
            ChkAutoCollapse.IsChecked = s.EnableAutoCollapse;
            TxtCollapseSeconds.Text = s.AutoCollapseSeconds.ToString();

            // Autostart
            ChkStartWithWindows.IsChecked = CheckIsAutostartEnabled();

            // Tools visibility
            ChkToolBack.IsChecked = s.ToolVisibility.GetValueOrDefault("Back", true);
            ChkToolTaskMgr.IsChecked = s.ToolVisibility.GetValueOrDefault("TaskManager", true);
            ChkToolDraw.IsChecked = s.ToolVisibility.GetValueOrDefault("Draw", true);
            ChkToolScreenshot.IsChecked = s.ToolVisibility.GetValueOrDefault("Screenshot", true);
            ChkToolSwitcher.IsChecked = s.ToolVisibility.GetValueOrDefault("AppSwitcher", true);

            // Captures
            TxtScreenshotFolder.Text = s.ScreenshotDirectory;
            ChkClipboard.IsChecked = s.CopyScreenshotToClipboard;
            ChkAutoSave.IsChecked = s.AutoSaveScreenshotToDisk;
            TxtDrawThickness.Text = s.DefaultStrokeThickness.ToString("F1");

            // Hotkeys
            TxtHotkeyToggle.Text = s.HotkeyShowHide;
            TxtHotkeyDraw.Text = s.HotkeyScreenDraw;
            TxtHotkeyScreenshot.Text = s.HotkeyScreenshot;
            TxtHotkeySwitcher.Text = s.HotkeyAppSwitcher;
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtOpacityVal != null)
            {
                TxtOpacityVal.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Screenshot Save Folder",
                InitialDirectory = Directory.Exists(TxtScreenshotFolder.Text)
                    ? TxtScreenshotFolder.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog() == true)
            {
                TxtScreenshotFolder.Text = dialog.FolderName;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = _settingsService.CurrentSettings;

            // Theme
            if (CmbTheme.SelectedItem is ComboBoxItem themeItem)
            {
                s.Theme = themeItem.Tag?.ToString() ?? "System";
            }

            // Opacity
            s.Opacity = SliderOpacity.Value;

            // Auto-collapse
            s.EnableAutoCollapse = ChkAutoCollapse.IsChecked ?? true;
            if (int.TryParse(TxtCollapseSeconds.Text, out int secs))
            {
                s.AutoCollapseSeconds = Math.Clamp(secs, 1, 60);
            }

            // Autostart
            bool autostart = ChkStartWithWindows.IsChecked ?? false;
            s.StartWithWindows = autostart;
            SetAutostartRegistry(autostart);

            // Tools
            s.ToolVisibility["Back"] = ChkToolBack.IsChecked ?? true;
            s.ToolVisibility["TaskManager"] = ChkToolTaskMgr.IsChecked ?? true;
            s.ToolVisibility["Draw"] = ChkToolDraw.IsChecked ?? true;
            s.ToolVisibility["Screenshot"] = ChkToolScreenshot.IsChecked ?? true;
            s.ToolVisibility["AppSwitcher"] = ChkToolSwitcher.IsChecked ?? true;

            // Captures
            s.ScreenshotDirectory = TxtScreenshotFolder.Text.Trim();
            s.CopyScreenshotToClipboard = ChkClipboard.IsChecked ?? true;
            s.AutoSaveScreenshotToDisk = ChkAutoSave.IsChecked ?? true;
            if (double.TryParse(TxtDrawThickness.Text, out double thick))
            {
                s.DefaultStrokeThickness = Math.Clamp(thick, 1.0, 30.0);
            }

            // Hotkeys
            s.HotkeyShowHide = TxtHotkeyToggle.Text.Trim();
            s.HotkeyScreenDraw = TxtHotkeyDraw.Text.Trim();
            s.HotkeyScreenshot = TxtHotkeyScreenshot.Text.Trim();
            s.HotkeyAppSwitcher = TxtHotkeySwitcher.Text.Trim();

            _settingsService.SaveSettings(s);
            DialogResult = true;
            Close();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Reset all FloatDock settings to initial defaults?",
                "FloatDock Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirm == MessageBoxResult.Yes)
            {
                _settingsService.SaveSettings(new AppSettings());
                LoadCurrentValues();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static bool CheckIsAutostartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppRegistryName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void SetAutostartRegistry(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key != null)
                {
                    if (enable)
                    {
                        string exePath = Environment.ProcessPath ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue(AppRegistryName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppRegistryName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to update Autostart Run registry key", ex);
            }
        }
    }
}
