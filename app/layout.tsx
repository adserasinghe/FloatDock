import type {Metadata} from 'next';
import './globals.css'; // Global styles

export const metadata: Metadata = {
  title: 'FloatDock - Windows Floating Shortcut Widget',
  description: 'Production-quality Windows 11/10 desktop floating shortcut widget built with C# .NET 8, WPF, and Win32 interop with five productivity tools and interactive desktop simulator.',
  openGraph: {
    title: 'FloatDock - Windows Floating Shortcut Widget',
    description: 'Production-quality Windows 11/10 desktop floating shortcut widget built with C# .NET 8, WPF, and Win32 interop with five productivity tools and interactive desktop simulator.',
    type: 'website',
  },
  twitter: {
    card: 'summary_large_image',
    title: 'FloatDock - Windows Floating Shortcut Widget',
    description: 'Production-quality Windows 11/10 desktop floating shortcut widget built with C# .NET 8, WPF, and Win32 interop with five productivity tools and interactive desktop simulator.',
  },
};

export default function RootLayout({children}: {children: React.ReactNode}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body suppressHydrationWarning className="bg-slate-950 text-slate-100 antialiased selection:bg-blue-600 selection:text-white">
        {children}
      </body>
    </html>
  );
}
