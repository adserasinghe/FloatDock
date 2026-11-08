using System;
using System.Collections.Generic;
using System.IO;

namespace FloatDock.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "System"; // "System", "Dark", "Light"
        public double Opacity { get; set; } = 0.92;
        public string Size { get; set; } = "Medium"; // "Small", "Medium", "Large"
        public int AutoCollapseSeconds { get; set; } = 5; // 0 to disable
        public bool EnableAutoCollapse { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;

        // Tool Visibility & Ordering
        public List<string> ToolOrder { get; set; } = new()
        {
            "Back",
            "TaskManager",
            "Draw",
            "Screenshot",
            "AppSwitcher"
        };

        public Dictionary<string, bool> ToolVisibility { get; set; } = new()
        {
            { "Back", true },
            { "TaskManager", true },
            { "Draw", true },
            { "Screenshot", true },
            { "AppSwitcher", true }
        };

        // Screenshot Configuration
        public string ScreenshotDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "FloatDock"
        );
        public string ScreenshotFormat { get; set; } = "PNG";
        public bool CopyScreenshotToClipboard { get; set; } = true;
        public bool AutoSaveScreenshotToDisk { get; set; } = true;
        public int ScreenshotDelaySeconds { get; set; } = 0;

        // Drawing Overlay Defaults
        public string DefaultDrawingColor { get; set; } = "#FF0078D4"; // Fluent Blue
        public double DefaultStrokeThickness { get; set; } = 3.5;
        public bool DrawingMultiMonitorSpan { get; set; } = false;

        // Hotkeys (Modifiers + Key strings)
        public string HotkeyShowHide { get; set; } = "Ctrl+Alt+W";
        public string HotkeyScreenDraw { get; set; } = "Ctrl+Alt+D";
        public string HotkeyScreenshot { get; set; } = "Ctrl+Alt+S";
        public string HotkeyAppSwitcher { get; set; } = "Ctrl+Alt+Tab";

        // Persistent Dock State
        public double LastX { get; set; } = 500;
        public double LastY { get; set; } = 24;
        public string DockedEdge { get; set; } = "Top"; // "Top", "Bottom", "Left", "Right", "Floating"
        public string Orientation { get; set; } = "Horizontal"; // "Horizontal", "Vertical"

        /// <summary>
        /// Validates settings values and restores safe defaults if values are out of bounds.
        /// </summary>
        public void Validate()
        {
            if (Opacity < 0.2 || Opacity > 1.0) Opacity = 0.92;
            if (AutoCollapseSeconds < 0 || AutoCollapseSeconds > 60) AutoCollapseSeconds = 5;
            if (string.IsNullOrWhiteSpace(ScreenshotDirectory))
            {
                ScreenshotDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "FloatDock"
                );
            }
            if (DefaultStrokeThickness < 1.0 || DefaultStrokeThickness > 30.0) DefaultStrokeThickness = 3.5;
        }
    }
}
