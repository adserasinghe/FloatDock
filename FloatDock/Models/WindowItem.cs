using System;
using System.Windows.Media;

namespace FloatDock.Models
{
    public class WindowItem
    {
        public IntPtr HWnd { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public ImageSource? Icon { get; set; }
        public bool IsMinimized { get; set; }
        public int ZOrderIndex { get; set; }

        public override string ToString() => $"{Title} ({ProcessName})";
    }
}
