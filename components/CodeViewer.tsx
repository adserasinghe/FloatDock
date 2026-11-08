'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import JSZip from 'jszip';
import {
  FileCode,
  Folder,
  Copy,
  Check,
  Download,
  Terminal,
  ExternalLink,
  Shield,
  Layers,
  Cpu,
  Monitor,
  Search,
  Code2,
  FileText
} from 'lucide-react';

export interface CodeFile {
  path: string;
  name: string;
  category: string;
  language: string;
  content: string;
}

interface CodeViewerProps {
  files: CodeFile[];
}

const TOOL_ICONS = [
  {
    role: 'App & Taskbar Tray Icon',
    fileName: 'icons8-widget-96.png',
    src: '/assets/aistudio/icons8-widget-96_aeba5e85.png',
    tool: 'App Window & Tray Notification',
  },
  {
    role: '1. Back Button',
    fileName: 'icons8-back-100.png',
    src: '/assets/aistudio/icons8-back-100_e9dc8c7d.png',
    tool: 'SendInput VK_BROWSER_BACK',
  },
  {
    role: '2. Task Manager',
    fileName: 'icons8-chart-96.png',
    src: '/assets/aistudio/icons8-chart-96_f4760975.png',
    tool: 'taskmgr.exe launcher & focus',
  },
  {
    role: '3. Screen Drawing',
    fileName: 'icons8-brush-96.png',
    src: '/assets/aistudio/icons8-brush-96_2556c803.png',
    tool: 'InkCanvas overlay + pass-through',
  },
  {
    role: '4. Screenshot Snip',
    fileName: 'icons8-screenshot-96.png',
    src: '/assets/aistudio/icons8-screenshot-96_b807e66b.png',
    tool: 'GDI BitBlt + HUD snip + clipboard',
  },
  {
    role: '5. App Switcher',
    fileName: 'icons8-apps-40.png',
    src: '/assets/aistudio/icons8-apps-40_06020b4c.png',
    tool: 'DWM live thumbnail previewer',
  },
  {
    role: '6. Settings Window',
    fileName: 'SettingsWindow.xaml',
    src: '/assets/aistudio/icons8-widget-96_aeba5e85.png',
    tool: 'Dock & Autostart Configuration',
  },
];

export function CodeViewer({ files }: CodeViewerProps) {
  const [selectedPath, setSelectedPath] = useState<string>(
    files.find((f) => f.name === 'MainWindow.xaml.cs')?.path || files[0]?.path || ''
  );
  const [copiedFile, setCopiedFile] = useState(false);
  const [copiedCmd, setCopiedCmd] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [isZipping, setIsZipping] = useState(false);

  const activeFile = files.find((f) => f.path === selectedPath) || files[0];

  const filteredFiles = files.filter(
    (f) =>
      f.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.path.toLowerCase().includes(searchQuery.toLowerCase()) ||
      f.category.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const categories = Array.from(new Set(files.map((f) => f.category)));

  const handleCopyCode = async () => {
    if (!activeFile) return;
    try {
      await navigator.clipboard.writeText(activeFile.content);
      setCopiedFile(true);
      setTimeout(() => setCopiedFile(false), 2000);
    } catch {
      // fallback
    }
  };

  const publishCommand =
    'dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true';

  const handleCopyCmd = async () => {
    try {
      await navigator.clipboard.writeText(publishCommand);
      setCopiedCmd(true);
      setTimeout(() => setCopiedCmd(false), 2000);
    } catch {
      // fallback
    }
  };

  const handleDownloadZip = async () => {
    setIsZipping(true);
    try {
      const zip = new JSZip();
      const folder = zip.folder('FloatDock');

      files.forEach((f) => {
        // remove leading 'FloatDock/' if present
        const rel = f.path.startsWith('FloatDock/') ? f.path.substring(10) : f.path;
        folder?.file(rel, f.content);
      });

      const blob = await zip.generateAsync({ type: 'blob' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'FloatDock-Windows-CSharp-Solution.zip';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Failed to create zip:', err);
    } finally {
      setIsZipping(false);
    }
  };

  return (
    <div className="flex h-screen w-full flex-col overflow-hidden bg-slate-950 text-slate-100 font-sans">
      {/* Top Navigation & App Header */}
      <header className="flex flex-shrink-0 items-center justify-between border-b border-slate-800 bg-slate-900/90 px-5 py-3 backdrop-blur-md">
        <div className="flex items-center gap-3">
          <div className="relative flex h-10 w-10 items-center justify-center rounded-xl bg-slate-800 p-1.5 shadow-md ring-1 ring-white/10">
            <Image
              src="/assets/aistudio/icons8-widget-96_aeba5e85.png"
              alt="FloatDock App Icon"
              width={36}
              height={36}
              referrerPolicy="no-referrer"
              className="object-contain"
              priority
            />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-semibold tracking-tight text-white">FloatDock</h1>
              <span className="rounded bg-blue-500/15 px-2 py-0.5 text-[11px] font-medium text-blue-400 ring-1 ring-blue-500/30">
                .NET 8 WPF x64
              </span>
              <span className="rounded bg-emerald-500/15 px-2 py-0.5 text-[11px] font-medium text-emerald-400 ring-1 ring-emerald-500/30">
                Single-File Exe
              </span>
            </div>
            <p className="text-xs text-slate-400">
              Windows Floating Shortcut Widget • Native C# 12 &amp; Win32 P/Invoke Solution
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleDownloadZip}
            disabled={isZipping}
            className="flex items-center gap-2 rounded-lg bg-blue-600 px-3.5 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-blue-500 active:scale-95 disabled:opacity-50"
          >
            <Download className="h-3.5 w-3.5" />
            {isZipping ? 'Packaging ZIP...' : 'Download Solution (.zip)'}
          </button>
        </div>
      </header>

      {/* Icons Mapping Strip */}
      <div className="flex flex-shrink-0 items-center overflow-x-auto border-b border-slate-800/80 bg-slate-900/40 px-5 py-2 scrollbar-none">
        <span className="mr-3 text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Icons8 Assets:
        </span>
        <div className="flex items-center gap-4">
          {TOOL_ICONS.map((icon) => (
            <div
              key={icon.fileName}
              className="flex items-center gap-2 rounded-md bg-slate-800/50 px-2.5 py-1 ring-1 ring-white/5"
            >
              <Image
                src={icon.src}
                alt={icon.role}
                width={18}
                height={18}
                referrerPolicy="no-referrer"
                className="object-contain"
              />
              <div className="flex flex-col">
                <span className="text-[11px] font-medium text-slate-200">{icon.role}</span>
                <span className="text-[9px] font-mono text-slate-400">{icon.fileName}</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Main Code Workspace */}
      <div className="flex flex-1 overflow-hidden">
        {/* Solution Explorer Sidebar */}
        <aside className="flex w-72 flex-shrink-0 flex-col border-r border-slate-800 bg-slate-900/60">
          <div className="p-3 border-b border-slate-800/80">
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-slate-400" />
              <input
                type="text"
                placeholder="Search files..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full rounded-md border border-slate-700 bg-slate-800/80 py-1.5 pl-8 pr-3 text-xs text-slate-200 placeholder-slate-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-2 space-y-3">
            {categories.map((cat) => {
              const catFiles = filteredFiles.filter((f) => f.category === cat);
              if (catFiles.length === 0) return null;

              return (
                <div key={cat} className="space-y-1">
                  <div className="flex items-center gap-1.5 px-2 py-1 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                    <Folder className="h-3 w-3 text-blue-400" />
                    <span>{cat}</span>
                    <span className="ml-auto text-[10px] text-slate-400 font-mono">
                      {catFiles.length}
                    </span>
                  </div>
                  <div className="space-y-0.5">
                    {catFiles.map((file) => {
                      const isSelected = file.path === selectedPath;
                      return (
                        <button
                          key={file.path}
                          onClick={() => setSelectedPath(file.path)}
                          className={`flex w-full items-center gap-2 rounded-md px-2.5 py-1.5 text-left text-xs font-mono transition ${
                            isSelected
                              ? 'bg-blue-600/20 text-blue-300 font-medium ring-1 ring-blue-500/30'
                              : 'text-slate-300 hover:bg-slate-800/60 hover:text-white'
                          }`}
                        >
                          <FileCode
                            className={`h-3.5 w-3.5 flex-shrink-0 ${
                              isSelected ? 'text-blue-400' : 'text-slate-400'
                            }`}
                          />
                          <span className="truncate">{file.name}</span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>

          <div className="border-t border-slate-800 bg-slate-900/90 p-3">
            <div className="text-[11px] font-semibold text-slate-400 mb-1.5 flex items-center gap-1.5">
              <Cpu className="h-3 w-3 text-emerald-400" />
              Runtime Architecture
            </div>
            <div className="space-y-1 text-[10px] text-slate-400">
              <div className="flex justify-between">
                <span>Target Framework:</span>
                <span className="font-mono text-slate-300">net8.0-windows10</span>
              </div>
              <div className="flex justify-between">
                <span>DPI Mode:</span>
                <span className="font-mono text-slate-300">PerMonitorV2</span>
              </div>
              <div className="flex justify-between">
                <span>Single Instance:</span>
                <span className="font-mono text-slate-300">Named Mutex</span>
              </div>
              <div className="flex justify-between">
                <span>Window Style:</span>
                <span className="font-mono text-slate-300">WS_EX_NOACTIVATE</span>
              </div>
            </div>
          </div>
        </aside>

        {/* Code Content Area */}
        <main className="flex flex-1 flex-col overflow-hidden bg-slate-950">
          {/* File Toolbar */}
          <div className="flex items-center justify-between border-b border-slate-800 bg-slate-900/50 px-4 py-2">
            <div className="flex items-center gap-2 font-mono text-xs text-slate-300">
              <FileText className="h-3.5 w-3.5 text-blue-400" />
              <span className="font-semibold text-white">{activeFile?.path}</span>
              <span className="rounded bg-slate-800 px-1.5 py-0.5 text-[10px] text-slate-400 uppercase">
                {activeFile?.language}
              </span>
              <span className="text-slate-400">
                {activeFile?.content.split('\n').length} lines
              </span>
            </div>

            <button
              onClick={handleCopyCode}
              className="flex items-center gap-1.5 rounded bg-slate-800 px-2.5 py-1 text-xs font-medium text-slate-200 transition hover:bg-slate-700 active:scale-95"
            >
              {copiedFile ? (
                <>
                  <Check className="h-3.5 w-3.5 text-emerald-400" />
                  <span className="text-emerald-400">Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5 text-slate-400" />
                  <span>Copy Code</span>
                </>
              )}
            </button>
          </div>

          {/* Code Viewer Text */}
          <div className="flex-1 overflow-auto bg-[#0d1117] p-4 font-mono text-[13px] leading-relaxed text-[#c9d1d9] selection:bg-blue-600/40">
            <pre className="tab-4">
              <code>{activeFile?.content}</code>
            </pre>
          </div>

          {/* Bottom Build Command Bar */}
          <div className="flex items-center justify-between border-t border-slate-800 bg-slate-900/90 px-4 py-2.5">
            <div className="flex items-center gap-2 overflow-hidden mr-4">
              <Terminal className="h-4 w-4 flex-shrink-0 text-emerald-400" />
              <span className="text-[11px] font-semibold text-slate-400 flex-shrink-0">
                Build Single-File EXE:
              </span>
              <code className="truncate rounded bg-black/50 px-2 py-1 text-[11px] font-mono text-emerald-300 ring-1 ring-white/10">
                {publishCommand}
              </code>
            </div>

            <button
              onClick={handleCopyCmd}
              className="flex flex-shrink-0 items-center gap-1.5 rounded bg-slate-800 px-2.5 py-1 text-xs font-medium text-slate-200 transition hover:bg-slate-700"
            >
              {copiedCmd ? (
                <>
                  <Check className="h-3 w-3 text-emerald-400" />
                  <span className="text-emerald-400">Copied</span>
                </>
              ) : (
                <>
                  <Copy className="h-3 w-3 text-slate-400" />
                  <span>Copy Command</span>
                </>
              )}
            </button>
          </div>
        </main>
      </div>
    </div>
  );
}
