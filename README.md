# FloatDock - User & Installation Guide

<div align="center">
  <img src="public/assets/aistudio/icons8-widget-96_aeba5e85.png" alt="FloatDock Logo" width="80" height="80" />
  <h3>FloatDock: Always-On-Top Windows Floating Shortcut Widget</h3>
  <p><strong>C# 12 • .NET 8 • WPF • Win32 Interop • Single-File x64 Executable</strong></p>
</div>

---

## 📖 Overview

**FloatDock** is an always-on-top, ultra-lightweight floating shortcut widget engineered natively for **Windows 11 and Windows 10 (1809+)**. 

FloatDock is built with **pure native C# (.NET 8 WPF)** and Win32 P/Invoke (`user32.dll`, `dwmapi.dll`, `gdi32.dll`). It features:
- **No Focus Stealing (`WS_EX_NOACTIVATE`)**: Clicking buttons on the widget sends commands to your active app without shifting keyboard focus.
- **Hidden from Taskbar and Alt+Tab (`WS_EX_TOOLWINDOW`)**: Runs cleanly as a floating utility bar.
- **Magnetic Edge Snapping**: Draggable grip snaps to any screen edge, automatically orienting horizontally or vertically.
- **Auto-Collapse Bubble**: Shrinks into a compact floating bubble after idle timeout.
- **RAM < 60 MB & Idle CPU ~0%**.

---

## 📦 How to Install the App

### Prerequisites
* **Operating System**: Windows 10 (Build 1809 or higher) or Windows 11 (64-bit).
* **Runtime**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (only required if building from source; the self-contained package embeds the runtime).

---

### Option 1: Build as a Single-File Portable Executable (Recommended)

You can publish FloatDock as a single, self-contained `.exe` file that runs instantly on any Windows 10/11 PC with no installer and no .NET runtime pre-installed.

1. Open a terminal (PowerShell, Command Prompt, or Windows Terminal).
2. Navigate to the `FloatDock` project folder:
   ```powershell
   cd FloatDock
   ```
3. Run the single-file publish command:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
   ```
4. The generated executable will be located at:
   ```
   FloatDock\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\FloatDock.exe
   ```
5. **Run the app**: Double-click `FloatDock.exe`.

---

### Option 2: Build a Windows Installer (`.exe` Setup)

FloatDock includes an Inno Setup script (`FloatDock.iss`) to produce a standard Windows installer wizard.

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php).
2. Right-click `FloatDock/FloatDock.iss` and select **Compile GUI** (or run `iscc FloatDock.iss` from command line).
3. The setup package `FloatDock-Setup-x64.exe` will be output to the `Output\` folder.
4. Run the installer:
   - Installs to `%ProgramFiles%\FloatDock`.
   - Adds Start Menu shortcuts and optional desktop shortcut.
   - Includes an uninstaller in Windows Settings / Control Panel.

---

### Option 3: Open in Visual Studio 2022

1. Open `FloatDock.csproj` in **Visual Studio 2022** (v17.8+).
2. Ensure the **.NET Desktop Development** workload is installed.
3. Select configuration **Release** and platform **x64**.
4. Press **F5** (or `Ctrl+F5`) to run.

---

## 🚀 How to Use FloatDock

When launched, FloatDock appears as a sleek translucent pill floating near the top center of your primary monitor.

```
+-------------------------------------------------------+
|  ⠿  |  [<]  |  [📊]  |  [🖌️]  |  [📸]  |  [🔲]  |  [⚙️]  |  [-]  |
+-------------------------------------------------------+
 Grip   Back   TaskMgr   Draw    Screen   Switch  Settings Collapse
```

### 1. Positioning & Magnetic Edge Snapping
* **Move the Widget**: Click and drag the grip handle (`⠿`) on the edge of the dock.
* **Snap to Screen Edges**: Drag the dock close to any monitor edge (top, bottom, left, or right):
  - Snapping to **Top** or **Bottom** organizes the tools in a **horizontal bar**.
  - Snapping to **Left** or **Right** automatically rotates tools into a **vertical bar**.
* **Multi-Monitor Safe**: Remembers its coordinates per display and automatically repositions inside the visible work area when display configurations change.

---

### 2. Auto-Collapse Floating Bubble
* After **5 seconds** of inactivity (configurable in Settings), the dock smoothly shrinks into a 40×40 circular floating bubble displaying the FloatDock icon.
* **Expand**: Hover the mouse over the bubble or click it once with the left mouse button.
* **Settings**: Right-click the bubble to open the Settings Window directly.
* **Manual Collapse**: Click the minimize/collapse button (`[-]`) on the dock to collapse it immediately.

---

### 3. The 6 Built-in Tools

#### Tool 1: Back Button (`icons8-back-100.png`)
* **Left-Click**: Sends a `VK_BROWSER_BACK` (browser back) key event to the active window you were working in before clicking the dock.
  - Works seamlessly across Google Chrome, Edge, Firefox, Windows File Explorer, and Windows Settings.
  - Because of `WS_EX_NOACTIVATE`, the widget never steals focus from your app.
* **Fallback**: Dispatches `Alt+Left` if the active application does not process `VK_BROWSER_BACK`.
* **Right-Click or Long-Press (500ms)**: Sends an `Escape` (`VK_ESCAPE`) key event to dismiss dialogs, popups, or fullscreen views.

#### Tool 2: Task Manager Shortcut (`icons8-chart-96.png`)
* **Click**: Opens Windows Task Manager (`taskmgr.exe`).
* **Smart Window Focus**: If Task Manager is already running, FloatDock immediately restores and brings the existing window to the front instead of opening duplicate instances.
* **Silent UAC**: Gracefully handles cancelled UAC elevation prompts without showing any errors.

#### Tool 3: Screen Drawing Overlay (`icons8-brush-96.png`)
* **Click (or press `Ctrl+Alt+D`)**: Instantly creates a full-screen transparent annotation canvas over your display.
* **Drawing Tools Toolbar**:
  - **Pen & Highlighter**: Freehand drawing with smooth curves.
  - **Shapes**: Straight line, directional arrow, rectangle, and ellipse.
  - **Text**: Place typed notes directly on the screen.
  - **Colors & Size**: Choose from 8 vibrant colors and an adjustable stroke width slider.
  - **Undo / Redo**: `Ctrl+Z` and `Ctrl+Y` shortcuts supported.
  - **Pass-Through Mode**: Click the pass-through toggle (`WS_EX_TRANSPARENT`) to interact with background applications while keeping your annotations visible on screen.
  - **Save & Clear**: One-click save to PNG or press `Esc` to exit drawing mode.

#### Tool 4: Screenshot Tool (`icons8-screenshot-96.png`)
* **Click (or press `Ctrl+Alt+S`)**: Enters precision snipping mode.
* **Interactive Snip**:
  - Drag over any area to capture a custom rectangular snip.
  - Real-time HUD displays exact pixel dimensions (e.g. `1280 × 720 px`).
* **Capture Options**: Full virtual desktop, active monitor, or single window.
* **Automatic Output**:
  - Copies image directly to your Windows Clipboard (`Ctrl+V` into Slack, Discord, Word, etc.).
  - Saves a timestamped `.png` file to `%USERPROFILE%\Pictures\FloatDock`.
  - Displays a non-intrusive toast notification with an "Open Folder" action.

#### Tool 5: App Switcher (`icons8-apps-40.png`)
* **Click (or press `Ctrl+Alt+Tab`)**: Opens a lightweight, fast window switcher.
* **Live DWM Thumbnails**: Renders real-time hardware previews of open windows via the Desktop Window Manager (`DwmRegisterThumbnail`).
* **Fuzzy Search Filter**: Type to quickly filter open windows by window title or process name.
* **Middle-Click to Close**: Middle-click any window in the list to gracefully close it (`WM_CLOSE`).
* **Show Desktop**: One-click button to minimize all windows and reveal your desktop.

#### Tool 6: Settings Window (`SettingsWindow.xaml`)
* **Click the Gear icon `⚙` on the dock** (or right-click the dock / tray icon):
  - **Appearance**: Adjust theme (System Default, Dark, Light), dock opacity slider (30% to 100%), and auto-collapse idle delay (1 to 60 seconds).
  - **Tools**: Turn individual tool buttons on or off to keep your dock as minimal as you like.
  - **Captures**: Select your preferred screenshot save directory and toggle clipboard auto-copy.
  - **Hotkeys**: Customize all global shortcuts.
  - **Start with Windows**: Toggle automatic startup on Windows logon (configures the registry at `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).

---

## ⌨️ Global Keyboard Shortcuts

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Ctrl+Alt+W` | **Toggle Dock** | Show or hide FloatDock from anywhere |
| `Ctrl+Alt+D` | **Screen Draw** | Open the full-screen drawing overlay |
| `Ctrl+Alt+S` | **Screenshot** | Enter region screen snipping mode |
| `Ctrl+Alt+Tab` | **App Switcher** | Open the live thumbnail window switcher |
| `Esc` | **Dismiss Overlay** | Exits Screen Drawing or Screenshot mode |

*(All hotkeys can be customized in the Settings Window).*

---

## 🔔 System Tray & Context Menu

FloatDock runs in the Windows System Tray (Taskbar notification area) with the custom icon `icons8-widget-96.png`:
* **Left-Click Tray Icon**: Toggles widget visibility (Show / Hide).
* **Right-Click Tray Icon**:
  - **Show / Hide Widget**
  - **Settings** (opens configuration window)
  - **Exit FloatDock** (terminates the background process cleanly)

---

## 📁 File Locations & Data Storage

| Item | Location |
| :--- | :--- |
| **Settings File** | `%AppData%\FloatDock\settings.json` |
| **Log File** | `%AppData%\FloatDock\logs\floatdock.log` |
| **Screenshots** | `%USERPROFILE%\Pictures\FloatDock\Screenshot_YYYYMMDD_HHmmss.png` |
| **Autostart Key** | `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\FloatDock` |

---

## ❓ Frequently Asked Questions (FAQ)

#### Q: Why doesn't the Back button make my active window lose focus?
> FloatDock applies the Win32 `WS_EX_NOACTIVATE` extended window style. When you click buttons on the dock, Windows does not activate FloatDock, keeping your active document, browser tab, or IDE focused while delivering the navigation input.

#### Q: Can I run multiple copies of FloatDock?
> No. FloatDock enforces single-instance execution via a named system Mutex (`Global\FloatDock_SingleInstance_Mutex`). Attempting to start a second instance will foreground the existing dock.

#### Q: How do I make the dock start automatically when I turn on my PC?
> Open **Settings** (gear icon `⚙` on the dock), go to the **Appearance** tab, check **"Start FloatDock automatically on Windows logon"**, and click **Save & Apply**.

#### Q: How do I completely exit the app?
> Right-click the dock or the taskbar tray icon and select **Exit FloatDock**.

---

<div align="center">
  <sub>FloatDock • Native C# 12 &amp; .NET 8 WPF Desktop Application</sub>
</div>
