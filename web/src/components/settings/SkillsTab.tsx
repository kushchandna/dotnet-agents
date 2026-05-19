import { useEffect, useState } from 'react';

interface Skill {
  id: string;
  description: string | null;
}

export function SkillsTab() {
  const [directories, setDirectories] = useState<string[]>([]);
  const [skills, setSkills] = useState<Skill[]>([]);
  const [newDir, setNewDir] = useState('');
  const [error, setError] = useState<string | null>(null);

  const loadDirectories = () =>
    fetch('/api/config/skill-directories')
      .then(r => r.json())
      .then(setDirectories)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load directories'));

  const loadSkills = () =>
    fetch('/api/config/skills')
      .then(r => r.json())
      .then(setSkills)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load skills'));

  const reload = () => { loadDirectories(); loadSkills(); };

  useEffect(() => { reload(); }, []);

  const handleAdd = async () => {
    const path = newDir.trim();
    if (!path) return;
    setError(null);
    const res = await fetch('/api/config/skill-directories', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path }),
    });
    if (!res.ok) {
      const body = await res.json().catch(() => null);
      setError(Array.isArray(body)
        ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
        : `Error ${res.status}`);
      return;
    }
    setNewDir('');
    reload();
  };

  const handleRemove = async (dir: string) => {
    if (!window.confirm(`Remove "${dir}"?`)) return;
    setError(null);
    const res = await fetch('/api/config/skill-directories', {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ path: dir }),
    });
    if (!res.ok) {
      const body = await res.json().catch(() => null);
      setError(Array.isArray(body)
        ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
        : `Error ${res.status}`);
      return;
    }
    reload();
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') handleAdd();
  };

  return (
    <div className="settings-section">
      <div className="settings-section-header">
        <span className="settings-section-title">Skill Directories</span>
        <button className="btn-settings-action" onClick={reload}>&#8635; Refresh</button>
      </div>
      {error && <div className="settings-error section">{error}</div>}

      {directories.length === 0 && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>
          No skill directories configured.
        </div>
      )}
      {directories.map(dir => (
        <div className="settings-list-row" key={dir}>
          <div className="settings-row-info">
            <div className="settings-row-id" style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: 13 }}>{dir}</div>
          </div>
          <div className="settings-row-actions">
            <button className="btn-settings-action danger" onClick={() => handleRemove(dir)}>Remove</button>
          </div>
        </div>
      ))}

      <div className="settings-list-row" style={{ gap: 8 }}>
        <input
          className="settings-input"
          value={newDir}
          onChange={e => setNewDir(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="/path/to/skills"
          style={{ flex: 1 }}
        />
        <button className="btn-settings-add" onClick={handleAdd}>+ Add Directory</button>
      </div>

      <div className="settings-section-header" style={{ marginTop: 24 }}>
        <span className="settings-section-title">Discovered Skills</span>
      </div>
      {skills.length === 0 && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>
          No skills discovered. Add a directory above.
        </div>
      )}
      {skills.map(s => (
        <div className="settings-list-row" key={s.id}>
          <div className="settings-row-info">
            <div className="settings-row-id">{s.id}</div>
            {s.description && <div className="settings-row-sub" style={{ whiteSpace: 'normal', overflow: 'visible', textOverflow: 'unset' }}>{s.description}</div>}
          </div>
          <div style={{ fontSize: 11, color: 'var(--dim)', fontFamily: 'JetBrains Mono, monospace' }}>skill</div>
        </div>
      ))}
    </div>
  );
}
