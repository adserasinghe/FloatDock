export default function Page() {
  return (
    <div style={{ fontFamily: 'monospace', padding: '24px', background: '#0d1117', color: '#c9d1d9', minHeight: '100vh' }}>
      <h1 style={{ color: '#58a6ff', fontSize: '18px', marginBottom: '8px' }}>FloatDock: Windows Native C# .NET 8 WPF Solution</h1>
      <p style={{ fontSize: '13px', color: '#8b949e', marginBottom: '16px' }}>
        Web view removed as requested. The complete native C# solution source files are located in <code>/FloatDock</code> and provided in full below.
      </p>
      <pre style={{ background: '#161b22', padding: '16px', borderRadius: '8px', border: '1px solid #30363d', fontSize: '12px' }}>
{`Publish command (Windows x64 Single-File):
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true`}
      </pre>
    </div>
  );
}
