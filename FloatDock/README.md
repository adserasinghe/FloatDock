# FloatDock - Windows Floating Shortcut Widget

**FloatDock** is a production-quality, high-performance Windows 11/10 desktop floating widget built with **C# 12, .NET 8, and WPF** utilizing native Win32 interop.

It stays topmost without stealing focus (`WS_EX_NOACTIVATE`), collapses to a compact floating bubble after inactivity, docks and magnetically snaps to screen edges, and provides one-click access to five essential tools:

1. **Back Button**: Dispatches navigation back to the last active window via `SendInput` (`VK_BROWSER_BACK` or `Alt+Left`, long-press/right-click sends `Esc`).
2. **Task Manager**: Instantly foregrounds an existing `taskmgr.exe` or launches a new elevated instance, handling UAC cancellation silently.
3. **Screen Drawing Tool**: Full-screen transparent overlay with WPF `InkCanvas`, shapes (arrow, line, rect, ellipse), text, 8 colors, stroke slider, undo/redo, pass-through click-through mode (`WS_EX_TRANSPARENT`), and annotation export.
4. **Screenshot Tool**: Region drag selection with live pixel HUD, window capture, active monitor, or full virtual desktop capture with clipboard auto-copy and disk persistence.
5. **App Switcher**: Lightweight Alt+Tab alternative with live hardware DWM thumbnail previews (`DwmRegisterThumbnail`), fuzzy title search, and middle-click to close window.

---

## 🛠️ Tech Stack & Requirements

- **Language / Runtime**: C# 12, .NET 8.0 SDK (target `net8.0-windows10.0.19041.0`)
- **UI Framework**: Windows Presentation Foundation (WPF) with PerMonitorV2 DPI awareness
- **Native Interop**: P/Invoke (`user32.dll`, `dwmapi.dll`, `gdi32.dll`, `shell32.dll`, `kernel32.dll`)
- **Allowed NuGets**:
  - `H.NotifyIcon.Wpf` (system tray icon and menu)
  - `System.Text.Json` (settings persistence)
- **Target OS**: Windows 10 (1809+) and Windows 11 (21H2/22H2/23H2+), x64
- **Single Instance**: Named system Mutex (`Global\FloatDock_SingleInstance_Mutex`)

---

## 🚀 Building & Publishing

### Prerequisites
- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# Dev Kit

### Command Line Publish (Self-Contained Single-File Executable)
```cmd
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Output binary:
`bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\FloatDock.exe`

### Inno Setup Installer Generation
Open `FloatDock.iss` in [Inno Setup 6](https://jrsoftware.org/isdl.php) and click **Compile** to generate `Output\FloatDock-Setup-x64.exe`.

---

## 🧪 Milestone Manual Test Checklist

### Milestone 1: Floating Widget Shell
- [ ] Launch `FloatDock.exe`. Verify the pill appears smoothly near the top edge without appearing in the Taskbar or Alt+Tab.
- [ ] Try launching a second copy of `FloatDock.exe`. Verify single-instance dialog alerts and exits gracefully.
- [ ] Click and drag the grip handle `⠿`. Drag near the top, bottom, left, and right screen edges; verify magnetic snapping.
- [ ] When docked to left/right, verify layout rotates vertically; when top/bottom, horizontally.
- [ ] Move mouse away for 5 seconds; verify the pill collapses into a 40x40 circular bubble. Hovering or clicking returns to full pill.
- [ ] Right-click tray icon: test Show/Hide, Settings, and Exit.

### Milestone 2: Back Button & Task Manager Shortcut
- [ ] Open Google Chrome or Windows Explorer and navigate through 3 pages/folders.
- [ ] Click FloatDock's Back button (``). Verify the active app navigates back without losing input focus.
- [ ] Right-click or long-press the Back button: verify `Esc` key is dispatched.
- [ ] Click the Task Manager button (``). Verify Task Manager opens.
- [ ] Minimize Task Manager and click the button again: verify existing Task Manager is brought to the front.
- [ ] If prompted with UAC and cancelled, verify FloatDock does not crash or display an error dialog.

### Milestone 3: Screenshot Tool
- [ ] Click the Screenshot button (``) or press `Ctrl+Alt+S`.
- [ ] Drag a region box. Verify dimmed background and real-time pixel size HUD (e.g., `800 × 600 px`).
- [ ] Release mouse. Verify screenshot is copied to clipboard and saved in `%USERPROFILE%\Pictures\FloatDock`.
- [ ] Verify toast prompt offering to open the containing folder.

### Milestone 4: App Switcher with Live Thumbnails
- [ ] Click the App Switcher button (``) or press `Ctrl+Alt+Tab`.
- [ ] Verify list of open windows with process icons and titles.
- [ ] Select a window: verify live hardware DWM thumbnail preview renders in the preview box.
- [ ] Type in the search box to filter by name. Use Up/Down arrows and Enter to focus window.
- [ ] Middle-click an item in the list: verify window closes (`WM_CLOSE`).
- [ ] Click "Show Desktop": verify all windows minimize.

### Milestone 5: Screen Drawing Overlay
- [ ] Click the Screen Drawing button (``) or press `Ctrl+Alt+D`.
- [ ] Draw freehand strokes with Pen and Highlighter. Test colors and stroke slider.
- [ ] Click Arrow / Rectangle / Ellipse and drag across screen to render geometric guides.
- [ ] Press `Ctrl+Z` to undo, `Ctrl+Y` to redo.
- [ ] Click "Pass-Through Mode". Verify you can interact with desktop windows underneath while drawing remains visible.
- [ ] Click "Save Screenshot" to export annotated canvas. Press `Esc` to close.

### Milestone 6: Settings Window & Hotkeys
- [ ] Open Settings. Change opacity, toggle tools on/off, adjust auto-collapse delay.
- [ ] Test "Start with Windows" checkbox and verify HKCU Run registry key updates.
- [ ] Test global hotkeys `Ctrl+Alt+W`, `Ctrl+Alt+D`, `Ctrl+Alt+S`, `Ctrl+Alt+Tab`.
- [ ] Check `%AppData%\FloatDock\logs\floatdock.log` for clean execution logs.

---

## ⚠️ Known Windows Platform Limitations & Mitigations

1. **Focus Stealing Prevention (`WS_EX_NOACTIVATE`)**:
   - *Limitation*: Windows normally activates any window upon mouse click.
   - *Workaround*: FloatDock sets `WS_EX_NOACTIVATE` via `SetWindowLongPtr` and processes WPF input messages without triggering window activation.
2. **Foreground Activation Lock (`SPI_SETFOREGROUNDLOCKTIMEOUT`)**:
   - *Limitation*: Windows blocks background processes from pulling arbitrary windows to the foreground.
   - *Workaround*: FloatDock calls `AttachThreadInput` between its thread and the target window's thread before executing `SetForegroundWindow` and `ShowWindow(SW_RESTORE)`.
3. **DWM Thumbnails & Virtual Desktops**:
   - *Limitation*: Windows on other virtual desktops or suspended UWP apps can render black/blank thumbnails.
   - *Workaround*: FloatDock checks `DwmGetWindowAttribute` with `DWMWA_CLOAKED` to filter out cloaked windows from the App Switcher.
4. **Mixed-DPI Multi-Monitor Setups**:
   - *Limitation*: Mixed display scalings (e.g. 150% laptop + 100% 4K monitor) can cause blurry screenshots or coordinate drift.
   - *Workaround*: Declared `PerMonitorV2` in `app.manifest` and query virtual screen metrics (`SM_XVIRTUALSCREEN`, `SM_YVIRTUALSCREEN`) to guarantee 1:1 pixel fidelity.
