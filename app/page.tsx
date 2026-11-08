import fs from 'node:fs';
import path from 'node:path';
import { CodeViewer, CodeFile } from '@/components/CodeViewer';

const FILES_METADATA: Array<{
  path: string;
  category: string;
  language: string;
}> = [
  { path: 'FloatDock/FloatDock.csproj', category: 'Project & Manifest', language: 'xml' },
  { path: 'FloatDock/app.manifest', category: 'Project & Manifest', language: 'xml' },
  { path: 'FloatDock/App.xaml', category: 'App Lifecycle', language: 'xaml' },
  { path: 'FloatDock/App.xaml.cs', category: 'App Lifecycle', language: 'csharp' },
  { path: 'FloatDock/Native/NativeMethods.cs', category: 'Win32 Interop', language: 'csharp' },
  { path: 'FloatDock/Views/MainWindow.xaml', category: 'Views & Overlays', language: 'xaml' },
  { path: 'FloatDock/Views/MainWindow.xaml.cs', category: 'Views & Overlays', language: 'csharp' },
  { path: 'FloatDock/Views/ScreenDrawingWindow.xaml', category: 'Views & Overlays', language: 'xaml' },
  { path: 'FloatDock/Views/ScreenDrawingWindow.xaml.cs', category: 'Views & Overlays', language: 'csharp' },
  { path: 'FloatDock/Views/ScreenshotSnippingWindow.xaml', category: 'Views & Overlays', language: 'xaml' },
  { path: 'FloatDock/Views/ScreenshotSnippingWindow.xaml.cs', category: 'Views & Overlays', language: 'csharp' },
  { path: 'FloatDock/Views/AppSwitcherPopup.xaml', category: 'Views & Overlays', language: 'xaml' },
  { path: 'FloatDock/Views/AppSwitcherPopup.xaml.cs', category: 'Views & Overlays', language: 'csharp' },
  { path: 'FloatDock/Views/SettingsWindow.xaml', category: 'Views & Overlays', language: 'xaml' },
  { path: 'FloatDock/Views/SettingsWindow.xaml.cs', category: 'Views & Overlays', language: 'csharp' },
  { path: 'FloatDock/Services/NavigationService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/ProcessService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/ScreenshotService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/WindowService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/HotkeyService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/SettingsService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Services/LoggerService.cs', category: 'Services & Tools', language: 'csharp' },
  { path: 'FloatDock/Models/AppSettings.cs', category: 'Models & Settings', language: 'csharp' },
  { path: 'FloatDock/Models/WindowItem.cs', category: 'Models & Settings', language: 'csharp' },
  { path: 'FloatDock/FloatDock.iss', category: 'Installer & Docs', language: 'inno' },
  { path: 'FloatDock/README.md', category: 'Installer & Docs', language: 'markdown' },
  { path: 'README.md', category: 'Installer & Docs', language: 'markdown' },
];

export default function Page() {
  const rootDir = process.cwd();

  const codeFiles: CodeFile[] = FILES_METADATA.map((meta) => {
    const fullPath = path.join(rootDir, meta.path);
    let content = '';
    try {
      if (fs.existsSync(fullPath)) {
        content = fs.readFileSync(fullPath, 'utf-8');
      }
    } catch (err) {
      console.error(`Error reading ${meta.path}:`, err);
    }

    const name = path.basename(meta.path);
    return {
      path: meta.path,
      name,
      category: meta.category,
      language: meta.language,
      content,
    };
  });

  return <CodeViewer files={codeFiles} />;
}
