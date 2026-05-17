import { useEffect, useRef, useState } from 'react';

interface McpServer {
  id: string;
  command?: string | null;
  args: string[];
  env: Record<string, string>;
  url?: string | null;
  enabled: boolean;
  retryLimit: number;
  retryInterval: number;
}

interface McpServerStatusDto {
  id: string;
  state: 'disabled' | 'connecting' | 'connected' | 'failed';
  error: string | null;
  lastConnectedAt: string | null;
  retryAttempt: number;
  toolNames: string[];
}

type Transport = 'stdio' | 'sse';
interface EnvRow { uid: number; key: string; value: string }
interface FormState {
  id: string;
  transport: Transport;
  command: string;
  args: string;
  env: EnvRow[];
  url: string;
  enabled: boolean;
  retryLimit: number;
  retryInterval: number;
}

const empty = (): FormState => ({
  id: '', transport: 'stdio', command: '', args: '', env: [], url: '',
  enabled: true, retryLimit: 3, retryInterval: 5,
});

let globalUid = 0;
const nextUid = () => ++globalUid;

const serverToForm = (s: McpServer): FormState => ({
  id: s.id, transport: s.url ? 'sse' : 'stdio',
  command: s.command ?? '', args: s.args.join(' '),
  env: Object.entries(s.env ?? {}).map(([key, value]) => ({ uid: nextUid(), key, value })),
  url: s.url ?? '',
  enabled: s.enabled ?? true,
  retryLimit: s.retryLimit ?? 3,
  retryInterval: s.retryInterval ?? 5,
});

function StatusBadge({ status }: { status: McpServerStatusDto }) {
  const [expanded, setExpanded] = useState(false);
  const labels: Record<McpServerStatusDto['state'], string> = {
    disabled: 'Disabled',
    connecting: 'Connecting…',
    connected: 'Connected',
    failed: 'Failed',
  };
  const tooltip = status.state === 'connected' && status.toolNames.length > 0
    ? `Tools: ${status.toolNames.join(', ')}`
    : status.error ?? '';
  return (
    <>
      <span
        className={`status-badge ${status.state}`}
        title={tooltip}
        onClick={() => status.state === 'failed' && setExpanded(e => !e)}
        role={status.state === 'failed' ? 'button' : undefined}
        tabIndex={status.state === 'failed' ? 0 : undefined}
      >
        {labels[status.state]}
      </span>
      {expanded && status.error && (
        <div className="status-error-panel">{status.error}</div>
      )}
    </>
  );
}

export function McpServersTab() {
  const [servers, setServers] = useState<McpServer[]>([]);
  const [statuses, setStatuses] = useState<McpServerStatusDto[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(empty());
  const [error, setError] = useState<string | null>(null);
  const uidRef = useRef(globalUid);

  const load = () => fetch('/api/config/mcp-servers').then(r => r.json()).then(setServers)
    .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load'));

  const loadStatus = () => fetch('/api/config/mcp-servers/status')
    .then(r => r.json())
    .then(setStatuses)
    .catch(() => {});

  useEffect(() => { load(); loadStatus(); }, []);

  const handleError = async (res: Response) => {
    const body = await res.json().catch(() => null);
    setError(Array.isArray(body)
      ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
      : `Error ${res.status}`);
  };

  const toPayload = (f: FormState) => ({
    id: f.id,
    command: f.transport === 'stdio' ? f.command || null : null,
    args: f.transport === 'stdio' ? f.args.trim().split(/\s+/).filter(Boolean) : [],
    env: Object.fromEntries(f.env.filter(e => e.key).map(e => [e.key, e.value])),
    url: f.transport === 'sse' ? f.url || null : null,
    enabled: f.enabled,
    retryLimit: f.retryLimit,
    retryInterval: f.retryInterval,
  });

  const submitAdd = async () => {
    setError(null);
    const res = await fetch('/api/config/mcp-servers', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setShowAdd(false); setForm(empty()); load(); loadStatus();
  };

  const submitEdit = async (id: string) => {
    setError(null);
    const res = await fetch(`/api/config/mcp-servers/${id}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setEditingId(null); setForm(empty()); load(); loadStatus();
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(`Delete "${id}"? This cannot be undone.`)) return;
    setError(null);
    const res = await fetch(`/api/config/mcp-servers/${id}`, { method: 'DELETE' });
    if (!res.ok) { await handleError(res); return; }
    load(); loadStatus();
  };

  const handleRetry = async (id: string) => {
    setError(null);
    const res = await fetch(`/api/config/mcp-servers/${id}/retry`, { method: 'POST' });
    if (!res.ok) { await handleError(res); return; }
    setTimeout(() => loadStatus(), 800);
  };

  const startEdit = (s: McpServer) => { setEditingId(s.id); setShowAdd(false); setForm(serverToForm(s)); setError(null); };
  const cancelForm = () => { setShowAdd(false); setEditingId(null); setForm(empty()); setError(null); };

  const addEnvRow = () => {
    const uid = ++uidRef.current;
    globalUid = uidRef.current;
    setForm(f => ({ ...f, env: [...f.env, { uid, key: '', value: '' }] }));
  };
  const removeEnvRow = (uid: number) => setForm(f => ({ ...f, env: f.env.filter(e => e.uid !== uid) }));
  const updateEnv = (uid: number, field: 'key' | 'value', val: string) =>
    setForm(f => ({ ...f, env: f.env.map(e => e.uid === uid ? { ...e, [field]: val } : e) }));

  const statusFor = (id: string) => statuses.find(s => s.id === id);

  const renderForm = (isEdit: boolean, onSave: () => void) => (
    <div className="settings-form">
      {!isEdit && (
        <div className="settings-form-row">
          <label className="settings-form-label">ID</label>
          <input className="settings-input" value={form.id} onChange={e => setForm(f => ({ ...f, id: e.target.value }))} placeholder="my-server" />
        </div>
      )}
      <div className="settings-form-row">
        <label className="settings-form-label">Transport</label>
        <div className="settings-radio-group">
          <label className="settings-radio-item">
            <input type="radio" name="transport" value="stdio" checked={form.transport === 'stdio'} onChange={() => setForm(f => ({ ...f, transport: 'stdio' }))} />
            stdio
          </label>
          <label className="settings-radio-item">
            <input type="radio" name="transport" value="sse" checked={form.transport === 'sse'} onChange={() => setForm(f => ({ ...f, transport: 'sse' }))} />
            SSE / HTTP
          </label>
        </div>
      </div>
      {form.transport === 'stdio' ? (
        <>
          <div className="settings-form-row">
            <label className="settings-form-label">Command</label>
            <input className="settings-input" value={form.command} onChange={e => setForm(f => ({ ...f, command: e.target.value }))} placeholder="node" />
          </div>
          <div className="settings-form-row">
            <label className="settings-form-label">Args (space-separated)</label>
            <input className="settings-input" value={form.args} onChange={e => setForm(f => ({ ...f, args: e.target.value }))} placeholder="server.js --port 3000" />
          </div>
        </>
      ) : (
        <div className="settings-form-row">
          <label className="settings-form-label">URL</label>
          <input className="settings-input" value={form.url} onChange={e => setForm(f => ({ ...f, url: e.target.value }))} placeholder="http://localhost:3000/sse" />
        </div>
      )}
      <div className="settings-form-row">
        <label className="settings-form-label">Environment Variables</label>
        {form.env.map(e => (
          <div key={e.uid} className="env-var-row" style={{ marginBottom: 6 }}>
            <input className="settings-input" value={e.key} onChange={ev => updateEnv(e.uid, 'key', ev.target.value)} placeholder="KEY" style={{ flex: '0 0 38%' }} />
            <input className="settings-input" value={e.value} onChange={ev => updateEnv(e.uid, 'value', ev.target.value)} placeholder="value" />
            <button className="btn-settings-action danger" onClick={() => removeEnvRow(e.uid)} style={{ flexShrink: 0 }}>×</button>
          </div>
        ))}
        <button className="btn-settings-action" onClick={addEnvRow} style={{ marginTop: 4 }}>+ Add Env Var</button>
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Enabled</label>
        <label className="settings-radio-item">
          <input
            type="checkbox"
            checked={form.enabled}
            onChange={e => setForm(f => ({ ...f, enabled: e.target.checked }))}
          />
          Enable this server
        </label>
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Retry limit</label>
        <input
          className="settings-input"
          type="number"
          min={0}
          value={form.retryLimit}
          onChange={e => setForm(f => ({ ...f, retryLimit: Math.max(0, parseInt(e.target.value, 10) || 0) }))}
          style={{ maxWidth: 100 }}
        />
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Retry interval (seconds)</label>
        <input
          className="settings-input"
          type="number"
          min={1}
          value={form.retryInterval}
          onChange={e => setForm(f => ({ ...f, retryInterval: Math.max(1, parseInt(e.target.value, 10) || 1) }))}
          style={{ maxWidth: 100 }}
        />
      </div>
      <div className="settings-form-actions">
        <button className="btn-settings-action" onClick={cancelForm}>Cancel</button>
        <button className="btn-settings-add" onClick={onSave}>Save</button>
      </div>
    </div>
  );

  return (
    <div className="settings-section">
      <div className="settings-section-header">
        <span className="settings-section-title">MCP Servers</span>
        <button className="btn-settings-action" onClick={() => { load(); loadStatus(); }}>&#8635; Refresh</button>
        {!showAdd && editingId === null && (
          <button className="btn-settings-add" onClick={() => { setShowAdd(true); setEditingId(null); setForm(empty()); setError(null); }}>
            + Add Server
          </button>
        )}
      </div>
      {error && <div className="settings-error section">{error}</div>}
      {showAdd && renderForm(false, submitAdd)}
      {servers.length === 0 && !showAdd && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>No MCP servers configured.</div>
      )}
      {servers.map(s => (
        editingId === s.id ? (
          <div key={s.id}>{renderForm(true, () => submitEdit(s.id))}</div>
        ) : (
          <div className="settings-list-row-wrap" key={s.id}>
            <div className="settings-list-row">
              <div className="settings-row-info">
                <div className="settings-row-id">{s.id}</div>
                <div className="settings-row-sub">{s.url ? `SSE: ${s.url}` : `stdio: ${s.command}`}</div>
              </div>
              <div className="settings-row-actions">
                {statusFor(s.id) && <StatusBadge status={statusFor(s.id)!} />}
                <button className="btn-settings-action" onClick={() => startEdit(s)}>Edit</button>
                {statusFor(s.id)?.state === 'failed' && (
                  <button className="btn-settings-action" onClick={() => handleRetry(s.id)}>Retry</button>
                )}
                <button className="btn-settings-action danger" onClick={() => handleDelete(s.id)}>Delete</button>
              </div>
            </div>
          </div>
        )
      ))}
    </div>
  );
}
