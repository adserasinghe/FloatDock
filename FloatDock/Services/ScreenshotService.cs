using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FloatDock.Native;

namespace FloatDock.Services
{
    /// <summary>
    /// Implements Tool 4 (Screenshot Tool):
    /// Multi-monitor virtual desktop capture using GDI+ BitBlt,
    /// region cropping, active window bounds capture, clipboard copy, and disk persistence.
    /// </summary>
    public static class ScreenshotService
    {
        public static Bitmap CaptureVirtualScreen()
        {
            int left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            int top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public static Bitmap CaptureCurrentMonitor(IntPtr hWnd)
        {
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hWnd, 2 /* MONITOR_DEFAULTTONEAREST */);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };

            if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            {
                int width = mi.rcMonitor.Width;
                int height = mi.rcMonitor.Height;

                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(mi.rcMonitor.Left, mi.rcMonitor.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                }
                return bitmap;
            }

            return CaptureVirtualScreen();
        }

        public static Bitmap? CaptureActiveWindow()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return null;

            // Use DWM extended frame bounds to avoid invisible drop shadows
            int hr = NativeMethods.DwmGetWindowAttribute(
                hWnd,
                NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out NativeMethods.RECT frameRect,
                Marshal.SizeOf(typeof(NativeMethods.RECT))
            );

            NativeMethods.RECT rect = (hr == 0) ? frameRect : default;
            if (hr != 0)
            {
                NativeMethods.GetWindowRect(hWnd, out rect);
            }

            int width = rect.Width;
            int height = rect.Height;

            if (width <= 0 || height <= 0) return null;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        public static Bitmap CropBitmap(Bitmap source, Rectangle rect)
        {
            var clamped = Rectangle.Intersect(new Rectangle(0, 0, source.Width, source.Height), rect);
            if (clamped.Width <= 0 || clamped.Height <= 0) return source;

            var target = new Bitmap(clamped.Width, clamped.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(target))
            {
                g.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height), clamped, GraphicsUnit.Pixel);
            }
            return target;
        }

        public static void CopyToClipboard(Bitmap bitmap)
        {
            try
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                    Clipboard.SetImage(source);
                    LoggerService.LogInfo("Screenshot successfully copied to Windows clipboard.");
                }
                finally
                {
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to copy screenshot to clipboard", ex);
            }
        }

        public static string SaveToDisk(Bitmap bitmap, string directory, string format = "PNG")
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string ext = format.Equals("JPEG", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                ImageFormat imgFormat = ext == "jpg" ? ImageFormat.Jpeg : ImageFormat.Png;

                string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
                string fullPath = Path.Combine(directory, fileName);

                bitmap.Save(fullPath, imgFormat);
                LoggerService.LogInfo($"Screenshot saved to {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Failed to save screenshot to {directory}", ex);
                return string.Empty;
            }
        }

        public static BitmapSource ToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                source.Freeze();
                return source;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }
        }
    }
}
