import { useEffect, useState } from 'react';

export function ToolsTab() {
  const [tools, setTools] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/tools')
      .then(r => r.json())
      .then(setTools)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="settings-section">
      <div className="settings-section-header">
        <span className="settings-section-title">Built-in Tools</span>
      </div>
      <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 12, borderBottom: '1px solid var(--border-faint)', paddingBottom: 10 }}>
        Built-in tools are defined in the server. Enable them per agent in the Agents tab.
      </div>
      {loading && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>Loading…</div>
      )}
      {!loading && tools.length === 0 && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>No built-in tools available.</div>
      )}
      {tools.map(t => (
        <div className="settings-list-row" key={t}>
          <div className="settings-row-info">
            <div className="settings-row-id">{t}</div>
          </div>
          <div style={{ fontSize: 11, color: 'var(--dim)', fontFamily: 'JetBrains Mono, monospace' }}>built-in</div>
        </div>
      ))}
    </div>
  );
}
