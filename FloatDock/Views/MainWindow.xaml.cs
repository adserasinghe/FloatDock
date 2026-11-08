using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using FloatDock.Models;
using FloatDock.Native;
using FloatDock.Services;
using H.NotifyIcon;

namespace FloatDock.Views
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService;
        private HotkeyService? _hotkeyService;
        private TaskbarIcon? _trayIcon;

        private readonly DispatcherTimer _autoCollapseTimer;
        private readonly DispatcherTimer _longPressTimer;
        private bool _isLongPressTriggered;
        private bool _isDragging;

        private ScreenDrawingWindow? _drawingWindow;
        private AppSwitcherPopup? _appSwitcherPopup;

        public MainWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _autoCollapseTimer = new DispatcherTimer();
            _autoCollapseTimer.Tick += AutoCollapseTimer_Tick;

            _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            _longPressTimer.Tick += LongPressTimer_Tick;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Apply WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, and WS_EX_TOPMOST styles
            WindowService.ApplyFloatDockWindowStyles(this);

            var helper = new WindowInteropHelper(this);
            IntPtr hWnd = helper.Handle;

            // Initialize Global Hotkeys
            _hotkeyService = new HotkeyService(hWnd);
            RegisterHotkeys();

            // Set initial position from settings
            Left = _settingsService.CurrentSettings.LastX;
            Top = _settingsService.CurrentSettings.LastY;
            SetOrientation(_settingsService.CurrentSettings.Orientation);

            ResetAutoCollapseTimer();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetupTrayIcon();
            ApplySettings(_settingsService.CurrentSettings);
            _settingsService.SettingsChanged += (s, settings) => ApplySettings(settings);
        }

        private void SetupTrayIcon()
        {
            try
            {
                // Set application window icon and taskbar tray icon to uploaded widget icon
                var iconUri = new Uri("pack://application:,,,/Assets/icons8-widget-96.png");
                var iconBitmap = new System.Windows.Media.Imaging.BitmapImage(iconUri);
                Icon = iconBitmap;

                _trayIcon = new TaskbarIcon
                {
                    ToolTipText = "FloatDock - Always On Top Widget",
                    IconSource = iconBitmap,
                    MenuActivation = PopupActivationMode.RightClick
                };

                var contextMenu = new ContextMenu();

                var itemShow = new MenuItem { Header = "Show / Hide Widget" };
                itemShow.Click += (s, e) => ToggleVisibility();

                var itemSettings = new MenuItem { Header = "Settings" };
                itemSettings.Click += (s, e) => OpenSettingsWindow();

                var itemExit = new MenuItem { Header = "Exit FloatDock" };
                itemExit.Click += (s, e) => ExitApplication();

                contextMenu.Items.Add(itemShow);
                contextMenu.Items.Add(itemSettings);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(itemExit);

                _trayIcon.ContextMenu = contextMenu;
                _trayIcon.TrayLeftMouseDown += (s, e) => ToggleVisibility();
                _trayIcon.ForceCreate();
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to initialize taskbar tray icon", ex);
            }
        }

        private void ApplySettings(AppSettings settings)
        {
            Dispatcher.Invoke(() =>
            {
                DockPill.Opacity = settings.Opacity;

                BtnBack.Visibility = settings.ToolVisibility.GetValueOrDefault("Back", true) ? Visibility.Visible : Visibility.Collapsed;
                BtnTaskManager.Visibility = settings.ToolVisibility.GetValueOrDefault("TaskManager", true) ? Visibility.Visible : Visibility.Collapsed;
                BtnDrawing.Visibility = settings.ToolVisibility.GetValueOrDefault("Draw", true) ? Visibility.Visible : Visibility.Collapsed;
                BtnScreenshot.Visibility = settings.ToolVisibility.GetValueOrDefault("Screenshot", true) ? Visibility.Visible : Visibility.Collapsed;
                BtnAppSwitcher.Visibility = settings.ToolVisibility.GetValueOrDefault("AppSwitcher", true) ? Visibility.Visible : Visibility.Collapsed;
                BtnSettings.Visibility = settings.ToolVisibility.GetValueOrDefault("Settings", true) ? Visibility.Visible : Visibility.Collapsed;

                ResetAutoCollapseTimer();
            });
        }

        private void RegisterHotkeys()
        {
            if (_hotkeyService == null) return;

            _hotkeyService.UnregisterAll();
            var settings = _settingsService.CurrentSettings;

            _hotkeyService.Register(settings.HotkeyShowHide, ToggleVisibility);
            _hotkeyService.Register(settings.HotkeyScreenDraw, () => Dispatcher.Invoke(ToggleDrawingOverlay));
            _hotkeyService.Register(settings.HotkeyScreenshot, () => Dispatcher.Invoke(LaunchScreenshotTool));
            _hotkeyService.Register(settings.HotkeyAppSwitcher, () => Dispatcher.Invoke(ToggleAppSwitcher));
        }

        #region Tool 1: Back Button & Esc

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLongPressTriggered)
            {
                NavigationService.SendNavigateBack();
            }
            _isLongPressTriggered = false;
        }

        private void BtnBack_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigationService.SendEscape();
        }

        private void BtnBack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isLongPressTriggered = false;
            _longPressTimer.Start();
        }

        private void BtnBack_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _longPressTimer.Stop();
        }

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {
            _longPressTimer.Stop();
            _isLongPressTriggered = true;
            NavigationService.SendEscape();
        }

        #endregion

        #region Tool 2: Task Manager

        private void BtnTaskManager_Click(object sender, RoutedEventArgs e)
        {
            ProcessService.OpenOrActivateTaskManager();
        }

        #endregion

        #region Tool 3: Screen Drawing Overlay

        private void BtnDrawing_Click(object sender, RoutedEventArgs e)
        {
            ToggleDrawingOverlay();
        }

        private void ToggleDrawingOverlay()
        {
            if (_drawingWindow == null || !_drawingWindow.IsLoaded)
            {
                _drawingWindow = new ScreenDrawingWindow(_settingsService);
                _drawingWindow.Closed += (s, e) => _drawingWindow = null;
                _drawingWindow.Show();
            }
            else
            {
                _drawingWindow.Close();
                _drawingWindow = null;
            }
        }

        #endregion

        #region Tool 4: Screenshot Tool

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            LaunchScreenshotTool();
        }

        private void LaunchScreenshotTool()
        {
            var snippingWindow = new ScreenshotSnippingWindow(_settingsService, (capturedBitmap) =>
            {
                // After capture, optionally open in drawing overlay if user requests
                _drawingWindow?.Close();
            });
            snippingWindow.Show();
        }

        #endregion

        #region Tool 5: App Switcher

        private void BtnAppSwitcher_Click(object sender, RoutedEventArgs e)
        {
            ToggleAppSwitcher();
        }

        private void ToggleAppSwitcher()
        {
            if (_appSwitcherPopup == null || !_appSwitcherPopup.IsLoaded)
            {
                var helper = new WindowInteropHelper(this);
                _appSwitcherPopup = new AppSwitcherPopup(helper.Handle, this);
                _appSwitcherPopup.Closed += (s, e) => _appSwitcherPopup = null;
                _appSwitcherPopup.Show();
            }
            else
            {
                _appSwitcherPopup.Close();
                _appSwitcherPopup = null;
            }
        }

        #endregion

        #region Dragging & Magnetic Edge Snapping

        private void DragGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _autoCollapseTimer.Stop();
                DragMove();
            }
        }

        private void DragGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SnapToScreenEdge();
                ResetAutoCollapseTimer();
            }
        }

        private void SnapToScreenEdge()
        {
            var helper = new WindowInteropHelper(this);
            var (snapX, snapY, edge, orientation) = WindowService.CalculateEdgeSnap(
                helper.Handle,
                Left,
                Top,
                ActualWidth,
                ActualHeight
            );

            Left = snapX;
            Top = snapY;

            SetOrientation(orientation);

            // Persist location
            _settingsService.CurrentSettings.LastX = Left;
            _settingsService.CurrentSettings.LastY = Top;
            _settingsService.CurrentSettings.DockedEdge = edge;
            _settingsService.CurrentSettings.Orientation = orientation;
            _settingsService.SaveSettings(_settingsService.CurrentSettings);
        }

        private void SetOrientation(string orientation)
        {
            if (orientation.Equals("Vertical", StringComparison.OrdinalIgnoreCase))
            {
                ToolsPanel.Orientation = Orientation.Vertical;
                DragGrip.Width = 32;
                DragGrip.Height = 16;
                DragGrip.Margin = new Thickness(0, 4, 0, 2);
            }
            else
            {
                ToolsPanel.Orientation = Orientation.Horizontal;
                DragGrip.Width = 18;
                DragGrip.Height = 32;
                DragGrip.Margin = new Thickness(4, 0, 2, 0);
            }
        }

        #endregion

        #region Auto-Collapse Bubble

        private void BtnCollapse_Click(object sender, RoutedEventArgs e)
        {
            CollapseToBubble();
        }

        private void CollapsedBubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ExpandFromBubble();
        }

        private void CollapseToBubble()
        {
            DockPill.Visibility = Visibility.Collapsed;
            CollapsedBubble.Visibility = Visibility.Visible;
            _autoCollapseTimer.Stop();
        }

        private void ExpandFromBubble()
        {
            CollapsedBubble.Visibility = Visibility.Collapsed;
            DockPill.Visibility = Visibility.Visible;
            ResetAutoCollapseTimer();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            _autoCollapseTimer.Stop();
            if (CollapsedBubble.Visibility == Visibility.Visible)
            {
                ExpandFromBubble();
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            ResetAutoCollapseTimer();
        }

        private void ResetAutoCollapseTimer()
        {
            _autoCollapseTimer.Stop();
            if (_settingsService.CurrentSettings.EnableAutoCollapse &&
                _settingsService.CurrentSettings.AutoCollapseSeconds > 0)
            {
                _autoCollapseTimer.Interval = TimeSpan.FromSeconds(_settingsService.CurrentSettings.AutoCollapseSeconds);
                _autoCollapseTimer.Start();
            }
        }

        private void AutoCollapseTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDragging && !IsMouseOver && DockPill.Visibility == Visibility.Visible)
            {
                CollapseToBubble();
            }
        }

        #endregion

        #region Menu & Lifecycle Handlers

        private void BtnSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void CollapsedBubble_MouseRightButtonUp(object sender, MouseButtonEventArgs e) => OpenSettingsWindow();

        private void MenuSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void MenuOrientation_Click(object sender, RoutedEventArgs e)
        {
            string newOrientation = ToolsPanel.Orientation == Orientation.Horizontal ? "Vertical" : "Horizontal";
            SetOrientation(newOrientation);
            _settingsService.CurrentSettings.Orientation = newOrientation;
            _settingsService.SaveSettings(_settingsService.CurrentSettings);
        }

        private void MenuHide_Click(object sender, RoutedEventArgs e) => Visibility = Visibility.Collapsed;

        private void MenuExit_Click(object sender, RoutedEventArgs e) => ExitApplication();

        public void ToggleVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
                ExpandFromBubble();
            }
        }

        private void OpenSettingsWindow()
        {
            var settingsWin = new SettingsWindow(_settingsService);
            settingsWin.ShowDialog();
            RegisterHotkeys();
        }

        private void ExitApplication()
        {
            _trayIcon?.Dispose();
            _hotkeyService?.Dispose();
            Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _trayIcon?.Dispose();
            _hotkeyService?.Dispose();
        }

        #endregion
    }
}
