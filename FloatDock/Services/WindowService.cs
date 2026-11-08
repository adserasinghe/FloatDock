using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FloatDock.Models;
using FloatDock.Native;

namespace FloatDock.Services
{
    /// <summary>
    /// Core Win32 window management service.
    /// Manages window styles (no-activate, tool-window, click-through),
    /// window enumeration, foreground transfer workarounds, and screen snapping.
    /// </summary>
    public static class WindowService
    {
        public static void ApplyFloatDockWindowStyles(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;

            if (hWnd == IntPtr.Zero) return;

            // Retrieve current extended style
            IntPtr exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
            long newExStyle = exStyle.ToInt64();

            // Apply WS_EX_NOACTIVATE (prevents focus stealing)
            // Apply WS_EX_TOOLWINDOW (hides from Alt+Tab and taskbar)
            // Apply WS_EX_TOPMOST (stays above normal windows)
            newExStyle |= NativeMethods.WS_EX_NOACTIVATE;
            newExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            newExStyle |= NativeMethods.WS_EX_TOPMOST;

            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newExStyle));

            // Force window position update to topmost without activating
            NativeMethods.SetWindowPos(
                hWnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_FRAMECHANGED
            );

            // Apply Windows 11 Acrylic / Mica backdrop if supported
            TryApplyBackdrop(hWnd, NativeMethods.DWMSBT_TRANSIENTWINDOW); // Acrylic style
        }

        public static void SetClickThrough(Window window, bool enable)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;
            if (hWnd == IntPtr.Zero) return;

            IntPtr exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
            long newExStyle = exStyle.ToInt64();

            if (enable)
            {
                newExStyle |= NativeMethods.WS_EX_TRANSPARENT;
            }
            else
            {
                newExStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            }

            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newExStyle));
        }

        public static bool TryApplyBackdrop(IntPtr hWnd, int backdropType)
        {
            try
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    int type = backdropType;
                    int result = NativeMethods.DwmSetWindowAttribute(
                        hWnd,
                        NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
                        ref type,
                        sizeof(int)
                    );
                    return result == 0;
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogWarning($"DwmSetWindowAttribute backdrop not supported on this OS build: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Reliable foreground window activation workaround.
        /// Bypasses the Windows SetForegroundWindow lock restriction by attaching thread inputs.
        /// </summary>
        public static void ActivateAndForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            try
            {
                // If minimized, restore first
                if (NativeMethods.IsIconic(hWnd))
                {
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                }

                IntPtr foregroundHWnd = NativeMethods.GetForegroundWindow();
                uint foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundHWnd, out _);
                uint currentThreadId = NativeMethods.GetCurrentThreadId();
                uint targetThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out _);

                bool attached = false;
                if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
                {
                    attached = NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
                }

                try
                {
                    NativeMethods.BringWindowToTop(hWnd);
                    NativeMethods.SetForegroundWindow(hWnd);
                }
                finally
                {
                    if (attached)
                    {
                        NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    }
                }

                LoggerService.LogInfo($"Successfully brought window 0x{hWnd.ToInt64():X} to foreground.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Failed to activate window 0x{hWnd.ToInt64():X}", ex);
            }
        }

        public static void CloseWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                NativeMethods.PostMessage(hWnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                LoggerService.LogInfo($"Posted WM_CLOSE to window 0x{hWnd.ToInt64():X}.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Failed to post WM_CLOSE to window 0x{hWnd.ToInt64():X}", ex);
            }
        }

        /// <summary>
        /// Enumerates visible, non-cloaked top-level application windows for the App Switcher.
        /// </summary>
        public static List<WindowItem> GetOpenWindows(IntPtr excludeHWnd)
        {
            var windows = new List<WindowItem>();
            int zOrder = 0;

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == excludeHWnd) return true;

                // Window must be visible
                if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                // Check title length
                int titleLength = NativeMethods.GetWindowTextLength(hWnd);
                if (titleLength == 0) return true;

                // Filter out tool windows
                IntPtr exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
                if ((exStyle.ToInt64() & NativeMethods.WS_EX_TOOLWINDOW) != 0) return true;

                // Check if cloaked by DWM (e.g. running on another virtual desktop or suspended UWP app)
                int cloakedVal = 0;
                int hr = NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_CLOAKED, out cloakedVal, sizeof(int));
                if (hr == 0 && cloakedVal != 0) return true;

                // Retrieve title
                var sb = new StringBuilder(titleLength + 1);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (string.IsNullOrWhiteSpace(title)) return true;

                // Check bounds (ignore 0x0 windows)
                if (NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect))
                {
                    if (rect.Width <= 0 || rect.Height <= 0) return true;
                }

                // Retrieve process name and icon
                string processName = "Application";
                ImageSource? iconSource = null;

                try
                {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                    var proc = Process.GetProcessById((int)processId);
                    processName = proc.ProcessName;

                    try
                    {
                        if (proc.MainModule?.FileName != null)
                        {
                            using var ico = Icon.ExtractAssociatedIcon(proc.MainModule.FileName);
                            if (ico != null)
                            {
                                iconSource = Imaging.CreateBitmapSourceFromHIcon(
                                    ico.Handle,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions()
                                );
                                iconSource.Freeze();
                            }
                        }
                    }
                    catch
                    {
                        // Some system processes restrict icon extraction without elevation
                    }
                }
                catch
                {
                    // Process may have exited during enumeration
                }

                windows.Add(new WindowItem
                {
                    HWnd = hWnd,
                    Title = title,
                    ProcessName = processName,
                    Icon = iconSource,
                    IsMinimized = NativeMethods.IsIconic(hWnd),
                    ZOrderIndex = zOrder++
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// Calculates magnetic edge snapping position against the work area of the active monitor.
        /// </summary>
        public static (double SnapX, double SnapY, string Edge, string Orientation) CalculateEdgeSnap(
            IntPtr windowHandle,
            double currentLeft,
            double currentTop,
            double width,
            double height,
            double snapThreshold = 28.0)
        {
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(windowHandle, 2 /* MONITOR_DEFAULTTONEAREST */);
            var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFO)) };

            if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            {
                return (currentLeft, currentTop, "Floating", "Horizontal");
            }

            double workLeft = mi.rcWork.Left;
            double workTop = mi.rcWork.Top;
            double workRight = mi.rcWork.Right;
            double workBottom = mi.rcWork.Bottom;

            // Clamp inside work area first
            double targetLeft = Math.Max(workLeft, Math.Min(currentLeft, workRight - width));
            double targetTop = Math.Max(workTop, Math.Min(currentTop, workBottom - height));

            // Test snap to Top edge
            if (Math.Abs(currentTop - workTop) <= snapThreshold)
            {
                return (targetLeft, workTop + 6, "Top", "Horizontal");
            }

            // Test snap to Bottom edge
            if (Math.Abs((currentTop + height) - workBottom) <= snapThreshold)
            {
                return (targetLeft, workBottom - height - 6, "Bottom", "Horizontal");
            }

            // Test snap to Left edge
            if (Math.Abs(currentLeft - workLeft) <= snapThreshold)
            {
                return (workLeft + 6, targetTop, "Left", "Vertical");
            }

            // Test snap to Right edge
            if (Math.Abs((currentLeft + width) - workRight) <= snapThreshold)
            {
                return (workRight - width - 6, targetTop, "Right", "Vertical");
            }

            return (targetLeft, targetTop, "Floating", "Horizontal");
        }
    }
}
