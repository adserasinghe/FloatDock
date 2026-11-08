using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FloatDock.Models;
using FloatDock.Native;
using FloatDock.Services;

namespace FloatDock.Views
{
    public partial class AppSwitcherPopup : Window
    {
        private readonly IntPtr _ownerHWnd;
        private readonly Window _dockWindow;
        private List<WindowItem> _allWindows = new();
        private IntPtr _activeThumbnailId = IntPtr.Zero;

        public AppSwitcherPopup(IntPtr ownerHWnd, Window dockWindow)
        {
            InitializeComponent();
            _ownerHWnd = ownerHWnd;
            _dockWindow = dockWindow;

            Loaded += AppSwitcherPopup_Loaded;
            Closing += AppSwitcherPopup_Closing;
        }

        private void AppSwitcherPopup_Loaded(object sender, RoutedEventArgs e)
        {
            // Position popup adjacent to the FloatDock widget
            PositionAdjacentToDock();

            RefreshWindowList();
            TxtSearch.Focus();
        }

        private void PositionAdjacentToDock()
        {
            // Position below or to the side of the dock
            if (_dockWindow.Top > 200)
            {
                Top = _dockWindow.Top - Height - 10;
            }
            else
            {
                Top = _dockWindow.Top + _dockWindow.ActualHeight + 10;
            }

            Left = Math.Max(20, Math.Min(_dockWindow.Left, SystemParameters.VirtualScreenWidth - Width - 20));
        }

        private void RefreshWindowList()
        {
            UnregisterCurrentThumbnail();

            _allWindows = WindowService.GetOpenWindows(_ownerHWnd);
            FilterWindows(TxtSearch.Text);
        }

        private void FilterWindows(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                LstWindows.ItemsSource = _allWindows;
            }
            else
            {
                string lower = filter.ToLowerInvariant();
                LstWindows.ItemsSource = _allWindows
                    .Where(w => w.Title.ToLowerInvariant().Contains(lower) || w.ProcessName.ToLowerInvariant().Contains(lower))
                    .ToList();
            }

            if (LstWindows.Items.Count > 0)
            {
                LstWindows.SelectedIndex = 0;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterWindows(TxtSearch.Text);
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (LstWindows.SelectedIndex < LstWindows.Items.Count - 1)
                {
                    LstWindows.SelectedIndex++;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (LstWindows.SelectedIndex > 0)
                {
                    LstWindows.SelectedIndex--;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ActivateSelectedItem();
                e.Handled = true;
            }
        }

        private void LstWindows_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstWindows.SelectedItem is WindowItem selected)
            {
                UpdateLiveDwmThumbnail(selected.HWnd);
            }
            else
            {
                UnregisterCurrentThumbnail();
            }
        }

        private void UpdateLiveDwmThumbnail(IntPtr targetHWnd)
        {
            UnregisterCurrentThumbnail();

            var helper = new WindowInteropHelper(this);
            IntPtr destHWnd = helper.Handle;

            int hr = NativeMethods.DwmRegisterThumbnail(destHWnd, targetHWnd, out _activeThumbnailId);
            if (hr == 0 && _activeThumbnailId != IntPtr.Zero)
            {
                DwmThumbnailHost.Visibility = Visibility.Visible;

                var transform = DwmThumbnailHost.TransformToAncestor(this);
                Point hostPoint = transform.Transform(new Point(0, 0));

                var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_RECTDESTINATION | NativeMethods.DWM_TNP_OPACITY,
                    fVisible = true,
                    opacity = 255,
                    rcDestination = new NativeMethods.RECT(
                        (int)hostPoint.X,
                        (int)hostPoint.Y,
                        (int)(hostPoint.X + DwmThumbnailHost.ActualWidth),
                        (int)(hostPoint.Y + DwmThumbnailHost.ActualHeight)
                    )
                };

                NativeMethods.DwmUpdateThumbnailProperties(_activeThumbnailId, ref props);
            }
            else
            {
                DwmThumbnailHost.Visibility = Visibility.Collapsed;
            }
        }

        private void UnregisterCurrentThumbnail()
        {
            if (_activeThumbnailId != IntPtr.Zero)
            {
                NativeMethods.DwmUnregisterThumbnail(_activeThumbnailId);
                _activeThumbnailId = IntPtr.Zero;
            }
            DwmThumbnailHost.Visibility = Visibility.Collapsed;
        }

        private void ActivateSelectedItem()
        {
            if (LstWindows.SelectedItem is WindowItem item)
            {
                WindowService.ActivateAndForegroundWindow(item.HWnd);
                Close();
            }
        }

        private void LstWindows_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ActivateSelectedItem();
        }

        private void LstWindows_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Middle-click closes the window
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (sender is ListBox && e.OriginalSource is DependencyObject dep)
                {
                    var item = ItemsControl.ContainerFromElement(LstWindows, dep) as ListBoxItem;
                    if (item?.DataContext is WindowItem windowItem)
                    {
                        WindowService.CloseWindow(windowItem.HWnd);
                        _allWindows.Remove(windowItem);
                        FilterWindows(TxtSearch.Text);
                        e.Handled = true;
                    }
                }
            }
        }

        private void BtnCloseItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is IntPtr hWnd)
            {
                WindowService.CloseWindow(hWnd);
                var item = _allWindows.FirstOrDefault(w => w.HWnd == hWnd);
                if (item != null)
                {
                    _allWindows.Remove(item);
                    FilterWindows(TxtSearch.Text);
                }
            }
        }

        private void BtnShowDesktop_Click(object sender, RoutedEventArgs e)
        {
            // Minimize all windows / Show desktop via Shell
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                shell?.MinimizeAll();
            }
            Close();
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            // Auto-close on focus loss
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void AppSwitcherPopup_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            UnregisterCurrentThumbnail();
        }
    }
}
