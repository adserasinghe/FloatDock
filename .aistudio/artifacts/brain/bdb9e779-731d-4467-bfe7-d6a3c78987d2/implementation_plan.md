# FloatDock: Native Windows Floating Productivity Widget

A lightweight, high-performance Windows 11/10 desktop floating widget built with **C# 12, .NET 8, and WPF** with native Win32 interop. FloatDock stays topmost without stealing focus (`WS_EX_NOACTIVATE`), collapses to a floating bubble, docks and snaps to screen edges, and provides instant access to five core productivity tools: **Browser/App Back**, **Task Manager**, **Screen Drawing Annotation Overlay**, **Multi-mode Screenshot Tool**, and **App Switcher with live DWM Thumbnails**.

---

## User Review & Critical Decisions

All specifications have been mapped directly to Windows APIs and WPF paradigms. Sensible architectural choices have been made to satisfy all constraints without blocking development:

- **Focus Preservation (`WS_EX_NOACTIVATE` & `WS_EX_TOOLWINDOW`)**:
  - The widget window uses extended window styles `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`.
  - When the user clicks the widget or buttons, mouse events are processed without WPF causing Windows to switch foreground focus. This guarantees the previously focused app remains active so the **Back Button** (`VK_BROWSER_BACK` / `Alt+Left`) immediately reaches the target app.
- **Window Activation Workaround (App Switcher)**:
  - Windows restricts `SetForegroundWindow` calls from background/unfocused processes (`SPI_SETFOREGROUNDLOCKTIMEOUT`). FloatDock uses the standard Win32 pattern: `AttachThreadInput` between FloatDock's UI thread and the target window's thread (or synthesizing an innocuous input event via `SendInput`) to reliably pass foreground focus and restore minimized windows (`ShowWindow(SW_RESTORE)`).
- **Smooth Mica / Acrylic Aesthetic**:
  - Leverages `DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE` (Mica/Acrylic on Windows 11 22H2+) with fallback to custom WPF composition brush with soft drop shadow (`DropShadowEffect`) and dark/light system theme detection via registry (`AppsUseLightTheme`).
- **Single Instance & Low Resource Footprint**:
  - Named system `Mutex` (`Global\FloatDock_SingleInstance_Mutex`).
  - No background polling timers. Idle CPU ~0%, RAM < 60 MB.

---

## 1. Overview & Core Concept

FloatDock is designed as an unobtrusive, always-accessible power-user companion for multi-monitor workstations and laptops:
- **Floating Pill / Bubble**: Compact floating bar with subtle grip handles, magnetic edge snapping (top, bottom, left, right), and automatic idle collapse to an unobtrusive circular bubble.
- **Orientation Awareness**: Automatically rotates between horizontal (when docked top/bottom) and vertical (when docked left/right).
- **Five Core Workflows**:
  1. **Back Button**: Instantly trigger back navigation in browser tabs, File Explorer, or settings without switching active apps.
  2. **Task Manager**: Instantly spawn or foreground `taskmgr.exe`, handling UAC privileges cleanly.
  3. **Screen Drawing Tool**: Full-screen transparent overlay (`InkCanvas`) with pen, highlighter, arrows, text, shapes, pass-through mode, and high-DPI stylus support.
  4. **Screenshot Tool**: Region selection, window capture, active monitor, or virtual desktop capture with auto-clipboard, timestamped disk saves, and direct annotation handoff.
  5. **App Switcher**: Lightweight Alt+Tab alternative with live hardware DWM thumbnail previews (`DwmRegisterThumbnail`), fuzzy title search, and middle-click closing.

---

## 2. User Experience & Visual Design

### Widget States & Visual Transitions
```
   [ Normal Docked Pill ]                     [ Collapsed Bubble ]
┌─────────────────────────────────┐                 ┌─────┐
│ ⠿ │ ⬅ │ ⚡ │ ✏️ │ 📷 │ 🪟 │                 │ ⚡  │
└─────────────────────────────────┘                 └─────┘
  Grip  Back Task Draw Shot Switch                 Hover / Click expands
```

- **Fluent Visual Language**:
  - Rounded pill geometry (corner radius 18px), semi-transparent background with blur behind (`DwmEnableBlurBehindWindow` / `DwmSetWindowAttribute`), subtle 1px border highlight (`rgba(255, 255, 255, 0.14)` in dark mode, `rgba(0, 0, 0, 0.08)` in light mode).
  - Segoe Fluent Icons (`Segoe Fluent Icons` / `Segoe MDL2 Assets`) with vector SVG geometry fallbacks.
- **Docking & Snapping Physics**:
  - While dragging via grip handle, proximity threshold (24px) triggers visual edge magnet snap.
  - Position is persisted per-monitor device ID in `%AppData%\FloatDock\settings.json`.
- **System Theme Integration**:
  - Monitors `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` (`AppsUseLightTheme`) and `WM_SETTINGCHANGE` to adapt color schemes dynamically without restarts.

---

## 3. Win32 API Catalog & Technical Interop

The following table details every Win32 API required across the application, its DLL source, purpose, and known edge-case handling:

| API Function | Library | Purpose & Implementation Details | Edge Cases & Mitigations |
| :--- | :--- | :--- | :--- |
| `GetWindowLongPtr` / `SetWindowLongPtr` | `user32.dll` | Modifies window style to add `WS_EX_NOACTIVATE (0x08000000)`, `WS_EX_TOOLWINDOW (0x00000080)`, and `WS_EX_TOPMOST (0x00000008)`. | Applied in `HwndSource` hook after window handle creation (`SourceInitialized`). |
| `SetWindowPos` | `user32.dll` | Keeps widget at `HWND_TOPMOST (-1)` and repositions window smoothly without activating (`SWP_NOACTIVATE \| SWP_NOSIZE \| SWP_SHOWWINDOW`). | Prevents z-order sinking when full-screen apps focus. |
| `SendInput` | `user32.dll` | Sends `VK_BROWSER_BACK (0xA6)` keydown/keyup events, or fallback `VK_MENU` + `VK_LEFT`. For long-press: sends `VK_ESCAPE (0x1B)`. | Works seamlessly because FloatDock never steals focus (`WS_EX_NOACTIVATE`). |
| `GetForegroundWindow` / `GetWindowThreadProcessId` | `user32.dll` | Identifies the target active window before any FloatDock interaction. | Caches handle before popups open to ensure correct target context. |
| `SetForegroundWindow` / `BringWindowToTop` | `user32.dll` | Restores and brings windows to front (Task Manager, App Switcher targets). | Windows lock timeout bypass: pairs with `AttachThreadInput` to grant focus permissions. |
| `AttachThreadInput` | `user32.dll` | Temporarily attaches FloatDock thread message queue to target window thread to allow focus transfer. | Always detached immediately in a `finally` block to prevent message queue hangs. |
| `EnumWindows` / `IsWindowVisible` | `user32.dll` | Enumerates top-level desktop windows for the App Switcher. | Filters cloaked windows (`DWMWA_CLOAKED`), zero-size bounds, and empty titles. |
| `DwmGetWindowAttribute` | `dwmapi.dll` | Checks `DWMWA_CLOAKED (14)` (virtual desktops, suspended UWP apps) and gets extended frame bounds (`DWMWA_EXTENDED_FRAME_BOUNDS`). | Eliminates invisible "ghost" windows from the App Switcher. |
| `DwmSetWindowAttribute` | `dwmapi.dll` | Enables Windows 11 Mica/Acrylic backdrop (`DWMWA_SYSTEMBACKDROP_TYPE = 38`) and dark mode title bar (`DWMWA_USE_IMMERSIVE_DARK_MODE = 20`). | Gracefully ignored on Windows 10 without throwing exceptions. |
| `DwmRegisterThumbnail` / `DwmUpdateThumbnailProperties` / `DwmUnregisterThumbnail` | `dwmapi.dll` | Renders real-time, hardware-accelerated DWM window thumbnails in the App Switcher without pixel copying. | Destination rect updated upon layout/scroll; unregistered on popup close. |
| `RegisterHotKey` / `UnregisterHotKey` | `user32.dll` | Registers system-wide hotkeys (Show/Hide, Draw, Screenshot, Switcher). | Catches `WM_HOTKEY` in HwndSource hook; logs collision if `ERROR_HOTKEY_ALREADY_REGISTERED`. |
| `GetSystemMetrics` / `GetMonitorInfo` / `MonitorFromWindow` | `user32.dll` | Retrieves work area (excluding Taskbar) and virtual screen bounding boxes across multiple displays. | Virtual desktop coordinate offsets handled for negative monitor coordinates. |
| `GetDpiForMonitor` / `GetDpiForWindow` | `shcore.dll` / `user32.dll` | Per-Monitor V2 DPI queries for crisp scaling on mixed 100%/150%/200% scaling setups. | Re-calculates layout on `WM_DPICHANGED`. |
| `CreateCompatibleDC` / `CreateCompatibleBitmap` / `BitBlt` / `GetDC` / `ReleaseDC` | `gdi32.dll` / `user32.dll` | High-performance screenshot capture directly from desktop device context. | Proper handling of virtual screen origins (`SM_XVIRTUALSCREEN`, `SM_YVIRTUALSCREEN`). |
| `SendMessage` (`WM_CLOSE`) | `user32.dll` | Gracefully closes target window upon middle-click in App Switcher. | Doesn't terminate process abruptly, giving app chance to prompt save. |
| `OpenProcess` / `Process.GetProcessesByName` | `kernel32.dll` | Checks if `taskmgr.exe` is already running to avoid redundant instances. | Elevates via `ShellExecuteEx` (`runas`) if launching fresh instance. |

---

## 4. Technical Architecture & Component Hierarchy

```
┌────────────────────────────────────────────────────────────────────────┐
│                              FloatDock App                             │
│                     (App.xaml / Single Instance Mutex)                  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
    ┌───────────────────────────────┴──────────────────────────────┐
    ▼                                                              ▼
┌───────────────────────────────────────┐      ┌─────────────────────────────────┐
│           Services Layer              │      │       Settings & Persistence    │
│ ├─ NativeMethods (P/Invoke)           │      │ ├─ SettingsService (JSON)       │
│ ├─ WindowService (Enum, Snap, Mica)   │      │ ├─ ThemeService (Mica/Dark/Light│
│ ├─ HotkeyService (RegisterHotKey)     │      │ └─ LoggerService (%AppData%)    │
│ ├─ ScreenshotService (GDI+/Capture)   │      └─────────────────────────────────┘
│ └─ DrawingService (InkCanvas Model)   │
└───────────────────┬───────────────────┘
                    │
    ┌───────────────┴──────────────────────────────────────────────┐
    ▼                                                              ▼
┌───────────────────────────────────────┐      ┌─────────────────────────────────┐
│          Primary UI Windows           │      │        Overlay Windows          │
│ ├─ MainWindow (Floating Dock Pill)    │      │ ├─ ScreenDrawingWindow          │
│ │   ├─ BackButtonCommand              │      │ │   ├─ WPF InkCanvas Overlay    │
│ │   ├─ TaskManagerCommand             │      │ │   └─ Floating Draw Palette    │
│ │   ├─ ScreenshotCommand              │      │ ├─ ScreenshotSnippingWindow     │
│ │   ├─ DrawingCommand                 │      │ │   └─ Drag-Rect Dimmed Canvas  │
│ │   └─ AppSwitcherToggleCommand       │      │ └─ AppSwitcherPopup             │
│ ├─ SettingsWindow (WPF Modern Form)   │      │     ├─ Search Filter Box        │
│ └─ TrayIcon (H.NotifyIcon / Menu)     │      │     └─ Live DWM Thumbnail List  │
└───────────────────────────────────────┘      └─────────────────────────────────┘
```

---

## 5. Milestone Implementation Plan

The project will be delivered across sequential, self-contained milestones:

- **Milestone 1 (M1): Floating Widget Shell**
  - Project setup, `app.manifest` (PerMonitorV2 DPI awareness), single-instance named Mutex.
  - Floating borderless window with `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`.
  - Windows 11 Mica/Acrylic backdrop interop, dark/light theme switching.
  - Grip handle dragging, 4-edge magnetic screen snapping, monitor work area bounds clamping.
  - Idle auto-collapse timer (animates pill into circular floating bubble, click/hover to expand).
  - System Tray icon (`H.NotifyIcon.Wpf`) with Show/Hide, Settings, and Exit menu.
- **Milestone 2 (M2): Back Button & Task Manager Shortcut**
  - Tool 1: Win32 `SendInput` dispatcher (`VK_BROWSER_BACK`, fallback `Alt+Left`, long-press `Esc`).
  - Tool 2: `Process.Start` with `UseShellExecute`, UAC cancellation absorption, `SetForegroundWindow` targeting existing Task Manager instance.
- **Milestone 3 (M3): Screenshot Tool**
  - Virtual desktop coordinate capture engine via GDI+ (`BitBlt` across multi-monitors).
  - Snipping overlay window with dimmed background, mouse drag selection rectangle, and live pixel size HUD.
  - Modes: Region, Full Screen (Current Monitor), All Monitors, Active Window.
  - Auto-copy to clipboard, save to `%USERPROFILE%\Pictures\FloatDock\Screenshot_YYYYMMDD_HHmmss.png`.
  - Floating toast notification with Quick Save, Edit in Drawing Tool, and Open Folder actions.
- **Milestone 4 (M4): App Switcher with Live Thumbnails**
  - Window enumeration (`EnumWindows`) filtered for user-visible, non-cloaked windows with icons.
  - Fast popup with live `DwmRegisterThumbnail` hardware-accelerated preview cards.
  - Fuzzy search box, arrow-key navigation, Enter to focus (`AttachThreadInput` workaround), middle-click to close window (`WM_CLOSE`), and "Show Desktop" action.
- **Milestone 5 (M5): Screen Drawing Tool**
  - Full-screen transparent overlay with WPF `InkCanvas`.
  - Floating draggable toolbar: Pen, Highlighter, Eraser, Line, Arrow, Rectangle, Ellipse, Text, 8 color presets, thickness slider, Undo (`Ctrl+Z`), Redo (`Ctrl+Y`), Clear.
  - "Pass-through mode" (`WS_EX_TRANSPARENT`) allowing clicks to pass through to underlying desktop while drawings stay visible.
  - Export annotated screen directly to clipboard or PNG file.
- **Milestone 6 (M6): Settings Window, Global Hotkeys & Publishing**
  - Global hotkeys (`RegisterHotKey`) for Show/Hide, Drawing, Screenshot, App Switcher.
  - Modern Settings UI: Theme, Opacity, Pill Size, Tool Visibility & Ordering, Auto-collapse timing, Screenshot paths, Hotkey re-binding, "Start with Windows" (HKCU Run key).
  - Logging infrastructure (`%AppData%\FloatDock\logs\floatdock.log`).
  - `.csproj` configuration, publish command (`dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`), Inno Setup installer script, and README.

---

## 6. Verification & Quality Assurance Strategy

1. **Focus Non-Stealing Test**: Click the widget Back button while working in Chrome or Windows Explorer; verify that typing focus never leaves the application and page navigation occurs immediately.
2. **Multi-Monitor & DPI Test**: Drag the dock across a 4K display (150% DPI) to a 1080p display (100% DPI); verify snapping, font rendering, and screenshot coordinate alignment remain accurate.
3. **UAC Silence Test**: Launch Task Manager with standard user credentials; cancel the elevation prompt and verify FloatDock handles it cleanly without crashing.
4. **App Switcher Focus Guarantee**: Switch to an application on a secondary monitor; verify `SetForegroundWindow` brings it into foreground focus without flashing taskbar buttons.
5. **Memory & CPU Profiling**: Validate that continuous idle state maintains ~0% CPU and private working set stays under 60 MB RAM.
