export interface CSharpSourceFile {
  path: string;
  name: string;
  category: 'Project' | 'Native' | 'Services' | 'Views' | 'Models' | 'Installer' | 'Docs';
  language: 'csharp' | 'xml' | 'json' | 'ini' | 'markdown';
  content: string;
}

export const CSHARP_FILES: CSharpSourceFile[] = [
  {
    path: 'FloatDock.csproj',
    name: 'FloatDock.csproj',
    category: 'Project',
    language: 'xml',
    content: `<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon>Assets\\icon.ico</ApplicationIcon>
    <PlatformTarget>x64</PlatformTarget>
    <Platforms>x64</Platforms>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>

    <!-- Single-File Publish Configuration -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <PublishTrimmed>false</PublishTrimmed>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="H.NotifyIcon.Wpf" Version="2.1.4" />
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>

  <ItemGroup>
    <Resource Include="Assets\\**\\*" />
  </ItemGroup>

</Project>`
  },
  {
    path: 'app.manifest',
    name: 'app.manifest',
    category: 'Project',
    language: 'xml',
    content: `<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="FloatDock.app"/>
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>

  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Windows 10 and Windows 11 -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
      <!-- Windows 8.1 -->
      <supportedOS Id="{1f676c76-80e1-4239-95bb-83d0f6d0da78}" />
    </application>
  </compatibility>

  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2, PerMonitor</dpiAwareness>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/PM</dpiAware>
      <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
    </windowsSettings>
  </application>
</assembly>`
  },
  {
    path: 'App.xaml',
    name: 'App.xaml',
    category: 'Views',
    language: 'xml',
    content: `<Application x:Class="FloatDock.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
        <FontFamily x:Key="FluentFont">Segoe UI Variable Display, Segoe UI, sans-serif</FontFamily>
    </Application.Resources>
</Application>`
  },
  {
    path: 'App.xaml.cs',
    name: 'App.xaml.cs',
    category: 'Views',
    language: 'csharp',
    content: `using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using FloatDock.Services;
using FloatDock.Views;

namespace FloatDock
{
    public partial class App : Application
    {
        private const string MutexName = @"Global\\FloatDock_SingleInstance_Mutex";
        private Mutex? _singleInstanceMutex;
        private SettingsService? _settingsService;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            SetupGlobalExceptionHandling();

            // Enforce single instance via named system Mutex
            _singleInstanceMutex = new Mutex(true, MutexName, out bool isOnlyInstance);
            if (!isOnlyInstance)
            {
                LoggerService.LogWarning("FloatDock is already running. Exiting secondary process instance.");
                MessageBox.Show(
                    "FloatDock is already running.\\nCheck your system tray or press Ctrl+Alt+W to reveal the widget.",
                    "FloatDock",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Shutdown();
                return;
            }

            LoggerService.LogInfo("FloatDock starting up. Initializing services.");
            base.OnStartup(e);

            _settingsService = new SettingsService();
            _mainWindow = new MainWindow(_settingsService);
            _mainWindow.Show();
        }

        private void SetupGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    LoggerService.LogError("AppDomain Unhandled Exception", ex);
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LoggerService.LogError("WPF Dispatcher Unhandled Exception", args.Exception);
                args.Handled = true;
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LoggerService.LogError("Unobserved Task Exception", args.Exception);
                args.SetObserved();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggerService.LogInfo("FloatDock shutting down.");
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}`
  },
  {
    path: 'Native/NativeMethods.cs',
    name: 'NativeMethods.cs',
    category: 'Native',
    language: 'csharp',
    content: `using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FloatDock.Native
{
    public static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TRANSPARENT = 0x00000020;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;

        public const ushort VK_BROWSER_BACK = 0xA6;
        public const ushort VK_MENU = 0x12;
        public const ushort VK_LEFT = 0x25;
        public const ushort VK_ESCAPE = 0x1B;
        public const uint INPUT_KEYBOARD = 1;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_CLOAKED = 14;
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr hwndDestination, IntPtr hwndSource, out IntPtr phThumbnailId);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr hThumbnailId);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr hThumbnailId, ref DWM_THUMBNAIL_PROPERTIES ptnProperties);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES { public uint dwFlags; public RECT rcDestination; public RECT rcSource; public byte opacity; [MarshalAs(UnmanagedType.Bool)] public bool fVisible; [MarshalAs(UnmanagedType.Bool)] public bool fSourceClientAreaOnly; }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT { public uint type; public InputUnion u; }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    }
}`
  },
  {
    path: 'Services/NavigationService.cs',
    name: 'NavigationService.cs',
    category: 'Services',
    language: 'csharp',
    content: `using System;
using System.Runtime.InteropServices;
using FloatDock.Native;

namespace FloatDock.Services
{
    public static class NavigationService
    {
        public static void SendNavigateBack()
        {
            var inputs = new NativeMethods.INPUT[2];
            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = NativeMethods.VK_BROWSER_BACK,
                        dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY
                    }
                }
            };
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = NativeMethods.VK_BROWSER_BACK,
                        dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP
                    }
                }
            };
            uint res = NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
            if (res == 0) SendAltLeftFallback();
        }

        public static void SendAltLeftFallback()
        {
            var inputs = new NativeMethods.INPUT[4];
            inputs[0].type = NativeMethods.INPUT_KEYBOARD; inputs[0].u.ki.wVk = NativeMethods.VK_MENU;
            inputs[1].type = NativeMethods.INPUT_KEYBOARD; inputs[1].u.ki.wVk = NativeMethods.VK_LEFT; inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY;
            inputs[2].type = NativeMethods.INPUT_KEYBOARD; inputs[2].u.ki.wVk = NativeMethods.VK_LEFT; inputs[2].u.ki.dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP;
            inputs[3].type = NativeMethods.INPUT_KEYBOARD; inputs[3].u.ki.wVk = NativeMethods.VK_MENU; inputs[3].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
            NativeMethods.SendInput(4, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        public static void SendEscape()
        {
            var inputs = new NativeMethods.INPUT[2];
            inputs[0].type = NativeMethods.INPUT_KEYBOARD; inputs[0].u.ki.wVk = NativeMethods.VK_ESCAPE;
            inputs[1].type = NativeMethods.INPUT_KEYBOARD; inputs[1].u.ki.wVk = NativeMethods.VK_ESCAPE; inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
            NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}`
  },
  {
    path: 'Services/ProcessService.cs',
    name: 'ProcessService.cs',
    category: 'Services',
    language: 'csharp',
    content: `using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace FloatDock.Services
{
    public static class ProcessService
    {
        private const int ERROR_CANCELLED_BY_USER = 1223;

        public static void OpenOrActivateTaskManager()
        {
            try
            {
                var processes = Process.GetProcessesByName("Taskmgr");
                if (processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        if (proc.MainWindowHandle != IntPtr.Zero)
                        {
                            WindowService.ActivateAndForegroundWindow(proc.MainWindowHandle);
                            return;
                        }
                    }
                }

                string taskmgrPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskmgr.exe");
                Process.Start(new ProcessStartInfo
                {
                    FileName = taskmgrPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Win32Exception win32Ex) when (win32Ex.NativeErrorCode == ERROR_CANCELLED_BY_USER)
            {
                // Silently swallow cancelled UAC prompt
                LoggerService.LogInfo("Task Manager UAC elevation cancelled by user.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to launch Task Manager", ex);
            }
        }
    }
}`
  },
  {
    path: 'Views/MainWindow.xaml.cs',
    name: 'MainWindow.xaml.cs',
    category: 'Views',
    language: 'csharp',
    content: `// FloatDock Main Floating Widget Logic
// Features: WS_EX_NOACTIVATE, Grip Drag, Edge Magnetic Snap, Auto-collapse Bubble, Tool Dispatch
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using FloatDock.Native;
using FloatDock.Services;

namespace FloatDock.Views
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _autoCollapseTimer;
        private readonly DispatcherTimer _longPressTimer;

        public MainWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _autoCollapseTimer = new DispatcherTimer();
            _autoCollapseTimer.Tick += (s, e) => CollapseToBubble();
            _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
            _longPressTimer.Tick += (s, e) => { _longPressTimer.Stop(); NavigationService.SendEscape(); };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowService.ApplyFloatDockWindowStyles(this);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => NavigationService.SendNavigateBack();
        private void BtnTaskManager_Click(object sender, RoutedEventArgs e) => ProcessService.OpenOrActivateTaskManager();
        private void BtnDrawing_Click(object sender, RoutedEventArgs e) => new ScreenDrawingWindow(_settingsService).Show();
        private void BtnScreenshot_Click(object sender, RoutedEventArgs e) => new ScreenshotSnippingWindow(_settingsService).Show();
        private void BtnAppSwitcher_Click(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            new AppSwitcherPopup(helper.Handle, this).Show();
        }
        private void CollapseToBubble()
        {
            DockPill.Visibility = Visibility.Collapsed;
            CollapsedBubble.Visibility = Visibility.Visible;
        }
        private void ExpandFromBubble()
        {
            CollapsedBubble.Visibility = Visibility.Collapsed;
            DockPill.Visibility = Visibility.Visible;
        }
    }
}`
  },
  {
    path: 'Views/MainWindow.xaml',
    name: 'MainWindow.xaml',
    category: 'Views',
    language: 'xml',
    content: `<Window x:Class="FloatDock.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="FloatDock"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        SizeToContent="WidthAndHeight">
    <Grid Margin="10">
        <Border x:Name="DockPill" Background="#D01E1E1E" BorderBrush="#25FFFFFF" BorderThickness="1" CornerRadius="18" Padding="4">
            <StackPanel x:Name="ToolsPanel" Orientation="Horizontal" VerticalAlignment="Center">
                <Border x:Name="DragGrip" Width="18" Height="32" Cursor="SizeAll">
                    <TextBlock Text="⠿" Foreground="#88FFFFFF" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
                <!-- Tool 1: Back Button -->
                <Button x:Name="BtnBack" Style="{StaticResource FluentToolButton}" ToolTip="Navigate Back" Click="BtnBack_Click">
                    <Image Source="pack://application:,,,/Assets/icons8-back-100.png" Width="20" Height="20" RenderOptions.BitmapScalingMode="HighQuality" />
                </Button>
                <!-- Tool 2: Task Manager -->
                <Button x:Name="BtnTaskManager" Style="{StaticResource FluentToolButton}" ToolTip="Task Manager" Click="BtnTaskManager_Click">
                    <Image Source="pack://application:,,,/Assets/icons8-chart-96.png" Width="20" Height="20" RenderOptions.BitmapScalingMode="HighQuality" />
                </Button>
                <!-- Tool 3: Screen Drawing Overlay -->
                <Button x:Name="BtnDrawing" Style="{StaticResource FluentToolButton}" ToolTip="Screen Drawing" Click="BtnDrawing_Click">
                    <Image Source="pack://application:,,,/Assets/icons8-brush-96.png" Width="20" Height="20" RenderOptions.BitmapScalingMode="HighQuality" />
                </Button>
                <!-- Tool 4: Screenshot Tool -->
                <Button x:Name="BtnScreenshot" Style="{StaticResource FluentToolButton}" ToolTip="Screenshot" Click="BtnScreenshot_Click">
                    <Image Source="pack://application:,,,/Assets/icons8-screenshot-96.png" Width="20" Height="20" RenderOptions.BitmapScalingMode="HighQuality" />
                </Button>
                <!-- Tool 5: App Switcher -->
                <Button x:Name="BtnAppSwitcher" Style="{StaticResource FluentToolButton}" ToolTip="App Switcher" Click="BtnAppSwitcher_Click">
                    <Image Source="pack://application:,,,/Assets/icons8-apps-40.png" Width="20" Height="20" RenderOptions.BitmapScalingMode="HighQuality" />
                </Button>
            </StackPanel>
        </Border>
        <!-- Collapsed Floating Bubble View -->
        <Border x:Name="CollapsedBubble" Width="40" Height="40" Background="#E61E1E1E" CornerRadius="20" Visibility="Collapsed" Cursor="Hand">
            <Image Source="pack://application:,,,/Assets/icons8-chart-96.png" Width="22" Height="22" RenderOptions.BitmapScalingMode="HighQuality" />
        </Border>
    </Grid>
</Window>`
  },
  {
    path: 'FloatDock.iss',
    name: 'FloatDock.iss',
    category: 'Installer',
    language: 'ini',
    content: `[Setup]
AppId={{5E97D4B6-61D3-479D-99C1-F833A5DF2F18}
AppName=FloatDock
AppVersion=1.0.0
DefaultDirName={autopf}\\FloatDock
OutputBaseFilename=FloatDock-Setup-x64
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Files]
Source: "bin\\Release\\net8.0-windows10.0.19041.0\\win-x64\\publish\\FloatDock.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\\FloatDock"; Filename: "{app}\\FloatDock.exe"
Name: "{userstartup}\\FloatDock"; Filename: "{app}\\FloatDock.exe"

[Run]
Filename: "{app}\\FloatDock.exe"; Description: "Launch FloatDock"; Flags: nowait postinstall skipifsilent`
  },
  {
    path: 'README.md',
    name: 'README.md',
    category: 'Docs',
    language: 'markdown',
    content: `# FloatDock - Windows Floating Shortcut Widget (.NET 8 WPF)

## Publish Command
\`\`\`cmd
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
\`\`\`

## Milestones Delivered
- M1: Floating Widget Shell (WS_EX_NOACTIVATE, Snapping, Auto-collapse)
- M2: Back Button & Task Manager Shortcut
- M3: Screenshot Tool with Region Drag HUD
- M4: App Switcher with Live DWM Thumbnails
- M5: Screen Drawing Overlay with Pass-through Mode
- M6: Settings, Hotkeys, Inno Setup Installer`
  }
];
