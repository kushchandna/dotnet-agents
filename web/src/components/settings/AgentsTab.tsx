import { useEffect, useState } from 'react';

interface Agent {
  id: string; name: string; description?: string | null; modelId: string;
  systemPrompt?: string | null; tools: string[];
  mcpServersInheritance: 'all' | 'none' | 'custom'; mcpServers: string[];
}
interface McpServer { id: string }
interface Model { id: string; modelName: string }
interface FormState {
  id: string; name: string; description: string; modelId: string; systemPrompt: string;
  tools: string[]; mcpServersInheritance: 'all' | 'none' | 'custom'; mcpServers: string[];
}
const empty = (): FormState => ({ id: '', name: '', description: '', modelId: '', systemPrompt: '', tools: [], mcpServersInheritance: 'all', mcpServers: [] });

export function AgentsTab() {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [availableTools, setAvailableTools] = useState<string[]>([]);
  const [availableMcpServers, setAvailableMcpServers] = useState<McpServer[]>([]);
  const [availableModels, setAvailableModels] = useState<Model[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(empty());
  const [error, setError] = useState<string | null>(null);

  const load = () => fetch('/api/config/agents').then(r => r.json()).then(setAgents)
    .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load'));
  useEffect(() => {
    load();
    fetch('/api/tools').then(r => r.json()).then(setAvailableTools).catch(console.error);
    fetch('/api/config/mcp-servers').then(r => r.json()).then(setAvailableMcpServers).catch(console.error);
    fetch('/api/config/models').then(r => r.json()).then(setAvailableModels).catch(console.error);
  }, []);

  const handleError = async (res: Response) => {
    const body = await res.json().catch(() => null);
    setError(Array.isArray(body)
      ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
      : `Error ${res.status}`);
  };

  const toPayload = (f: FormState) => ({
    id: f.id, name: f.name, description: f.description || null, modelId: f.modelId,
    systemPrompt: f.systemPrompt || null, tools: f.tools,
    mcpServersInheritance: f.mcpServersInheritance,
    mcpServers: f.mcpServersInheritance === 'custom' ? f.mcpServers : [],
  });

  const submitAdd = async () => {
    setError(null);
    const res = await fetch('/api/config/agents', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setShowAdd(false); setForm(empty()); load();
  };

  const submitEdit = async (id: string) => {
    setError(null);
    const res = await fetch(`/api/config/agents/${id}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setEditingId(null); setForm(empty()); load();
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(`Delete "${id}"? This cannot be undone.`)) return;
    setError(null);
    const res = await fetch(`/api/config/agents/${id}`, { method: 'DELETE' });
    if (!res.ok) { await handleError(res); return; }
    load();
  };

  const startEdit = (a: Agent) => {
    setEditingId(a.id); setShowAdd(false); setError(null);
    setForm({ id: a.id, name: a.name, description: a.description ?? '', modelId: a.modelId, systemPrompt: a.systemPrompt ?? '', tools: a.tools, mcpServersInheritance: a.mcpServersInheritance, mcpServers: a.mcpServers });
  };

  const cancelForm = () => { setShowAdd(false); setEditingId(null); setForm(empty()); setError(null); };

  const toggleTool = (toolId: string) =>
    setForm(f => ({ ...f, tools: f.tools.includes(toolId) ? f.tools.filter(t => t !== toolId) : [...f.tools, toolId] }));

  const toggleMcpServer = (serverId: string) =>
    setForm(f => ({ ...f, mcpServers: f.mcpServers.includes(serverId) ? f.mcpServers.filter(s => s !== serverId) : [...f.mcpServers, serverId] }));

  const renderForm = (isEdit: boolean, onSave: () => void) => (
    <div className="settings-form">
      {!isEdit && (
        <div className="settings-form-row">
          <label className="settings-form-label">ID</label>
          <input className="settings-input" value={form.id} onChange={e => setForm(f => ({ ...f, id: e.target.value }))} placeholder="my-agent" />
        </div>
      )}
      <div className="settings-form-row">
        <label className="settings-form-label">Name</label>
        <input className="settings-input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="My Agent" />
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Model ID</label>
        {availableModels.length > 0 ? (
          <select
            className="settings-input"
            value={form.modelId}
            onChange={e => setForm(f => ({ ...f, modelId: e.target.value }))}
          >
            <option value="">-- select model --</option>
            {availableModels.map(m => (
              <option key={m.id} value={m.id}>{m.id} ({m.modelName})</option>
            ))}
          </select>
        ) : (
          <input
            className="settings-input"
            value={form.modelId}
            onChange={e => setForm(f => ({ ...f, modelId: e.target.value }))}
            placeholder="local"
          />
        )}
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Description</label>
        <input className="settings-input" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="(optional)" />
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">System Prompt</label>
        <textarea className="settings-textarea" value={form.systemPrompt} onChange={e => setForm(f => ({ ...f, systemPrompt: e.target.value }))} placeholder="You are a helpful assistant." />
      </div>
      {availableTools.length > 0 && (
        <div className="settings-form-row">
          <label className="settings-form-label">Built-in Tools</label>
          <div className="settings-checkbox-group">
            {availableTools.map(t => (
              <label key={t} className="settings-checkbox-item">
                <input type="checkbox" checked={form.tools.includes(t)} onChange={() => toggleTool(t)} />
                {t}
              </label>
            ))}
          </div>
        </div>
      )}
      <div className="settings-form-row">
        <label className="settings-form-label">MCP Servers</label>
        <div className="settings-radio-group">
          {(['all', 'none', 'custom'] as const).map(v => (
            <label key={v} className="settings-radio-item">
              <input type="radio" name="mcpInheritance" value={v} checked={form.mcpServersInheritance === v} onChange={() => setForm(f => ({ ...f, mcpServersInheritance: v }))} />
              {v}
            </label>
          ))}
        </div>
        {form.mcpServersInheritance === 'custom' && availableMcpServers.length > 0 && (
          <div className="settings-checkbox-group" style={{ marginTop: 8 }}>
            {availableMcpServers.map(s => (
              <label key={s.id} className="settings-checkbox-item">
                <input type="checkbox" checked={form.mcpServers.includes(s.id)} onChange={() => toggleMcpServer(s.id)} />
                {s.id}
              </label>
            ))}
          </div>
        )}
        {form.mcpServersInheritance === 'custom' && availableMcpServers.length === 0 && (
          <div style={{ marginTop: 8, fontSize: 12, color: 'var(--muted)' }}>No MCP servers configured. Add them in the MCP Servers tab.</div>
        )}
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
        <span className="settings-section-title">Agents</span>
        {!showAdd && editingId === null && (
          <button className="btn-settings-add" onClick={() => { setShowAdd(true); setEditingId(null); setForm(empty()); setError(null); }}>
            + Add Agent
          </button>
        )}
      </div>
      {error && <div className="settings-error section">{error}</div>}
      {showAdd && renderForm(false, submitAdd)}
      {agents.map(a => (
        editingId === a.id ? (
          <div key={a.id}>
            {renderForm(true, () => submitEdit(a.id))}
          </div>
        ) : (
          <div className="settings-list-row" key={a.id}>
            <div className="settings-row-info">
              <div className="settings-row-id">{a.name}</div>
              <div className="settings-row-sub">{a.id} · {a.modelId}</div>
            </div>
            <div className="settings-row-actions">
              <button className="btn-settings-action" onClick={() => startEdit(a)}>Edit</button>
              <button className="btn-settings-action danger" onClick={() => handleDelete(a.id)}>Delete</button>
            </div>
          </div>
        )
      ))}
    </div>
  );
}
