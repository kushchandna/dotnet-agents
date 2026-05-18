import { useEffect, useState } from 'react';

interface ModelConfig {
  id: string;
  provider: 'openai' | 'openai-compatible' | 'ollama';
  modelName: string;
  endpoint?: string | null;
  apiKeyEnvVar?: string | null;
}

interface FormState {
  id: string;
  provider: 'openai' | 'openai-compatible' | 'ollama';
  modelName: string;
  endpoint: string;
  apiKeyEnvVar: string;
}

const empty = (): FormState => ({ id: '', provider: 'ollama', modelName: '', endpoint: '', apiKeyEnvVar: '' });

export function ModelsTab() {
  const [models, setModels] = useState<ModelConfig[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(empty());
  const [error, setError] = useState<string | null>(null);

  const load = () => fetch('/api/config/models')
    .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json(); })
    .then(setModels)
    .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load'));

  useEffect(() => { load(); }, []);

  const handleError = async (res: Response) => {
    const body = await res.json().catch(() => null);
    setError(Array.isArray(body)
      ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
      : `Error ${res.status}`);
  };

  const toPayload = (f: FormState) => ({
    id: f.id,
    provider: f.provider,
    modelName: f.modelName,
    endpoint: f.endpoint || null,
    apiKeyEnvVar: f.apiKeyEnvVar || null,
  });

  const submitAdd = async () => {
    setError(null);
    const res = await fetch('/api/config/models', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setShowAdd(false); setForm(empty()); load();
  };

  const submitEdit = async (id: string) => {
    setError(null);
    const res = await fetch(`/api/config/models/${id}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(toPayload(form)),
    });
    if (!res.ok) { await handleError(res); return; }
    setEditingId(null); setForm(empty()); load();
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(`Delete "${id}"? This cannot be undone.`)) return;
    setError(null);
    const res = await fetch(`/api/config/models/${id}`, { method: 'DELETE' });
    if (!res.ok) { await handleError(res); return; }
    load();
  };

  const startEdit = (m: ModelConfig) => {
    setEditingId(m.id); setShowAdd(false); setError(null);
    setForm({ id: m.id, provider: m.provider, modelName: m.modelName, endpoint: m.endpoint ?? '', apiKeyEnvVar: m.apiKeyEnvVar ?? '' });
  };

  const cancelForm = () => { setShowAdd(false); setEditingId(null); setForm(empty()); setError(null); };

  const renderForm = (isEdit: boolean, onSave: () => void) => (
    <div className="settings-form">
      {!isEdit && (
        <div className="settings-form-row">
          <label className="settings-form-label">ID</label>
          <input className="settings-input" value={form.id} onChange={e => setForm(f => ({ ...f, id: e.target.value }))} placeholder="my-model" />
        </div>
      )}
      <div className="settings-form-row">
        <label className="settings-form-label">Provider</label>
        <select className="settings-input" value={form.provider} onChange={e => setForm(f => ({ ...f, provider: e.target.value as FormState['provider'] }))}>
          <option value="ollama">ollama</option>
          <option value="openai">openai</option>
          <option value="openai-compatible">openai-compatible</option>
        </select>
      </div>
      <div className="settings-form-row">
        <label className="settings-form-label">Model Name</label>
        <input className="settings-input" value={form.modelName} onChange={e => setForm(f => ({ ...f, modelName: e.target.value }))} placeholder="gemma4:e2b" />
      </div>
      {(form.provider === 'openai-compatible' || form.provider === 'ollama') && (
        <div className="settings-form-row">
          <label className="settings-form-label">Endpoint</label>
          <input className="settings-input" value={form.endpoint} onChange={e => setForm(f => ({ ...f, endpoint: e.target.value }))} placeholder="http://localhost:11434" />
        </div>
      )}
      {(form.provider === 'openai' || form.provider === 'openai-compatible') && (
        <div className="settings-form-row">
          <label className="settings-form-label">API Key Env Var</label>
          <input className="settings-input" value={form.apiKeyEnvVar} onChange={e => setForm(f => ({ ...f, apiKeyEnvVar: e.target.value }))} placeholder="OPENAI_API_KEY" />
        </div>
      )}
      <div className="settings-form-actions">
        <button className="btn-settings-action" onClick={cancelForm}>Cancel</button>
        <button className="btn-settings-add" onClick={onSave}>Save</button>
      </div>
    </div>
  );

  return (
    <div className="settings-section">
      <div className="settings-section-header">
        <span className="settings-section-title">Models</span>
        {!showAdd && editingId === null && (
          <button className="btn-settings-add" onClick={() => { setShowAdd(true); setEditingId(null); setForm(empty()); setError(null); }}>
            + Add Model
          </button>
        )}
      </div>
      {error && <div className="settings-error section">{error}</div>}
      {showAdd && renderForm(false, submitAdd)}
      {models.length === 0 && !showAdd && (
        <div className="settings-list-row" style={{ color: 'var(--muted)', fontSize: 13 }}>No models configured.</div>
      )}
      {models.map(m => (
        editingId === m.id ? (
          <div key={m.id}>{renderForm(true, () => submitEdit(m.id))}</div>
        ) : (
          <div className="settings-list-row" key={m.id}>
            <div className="settings-row-info">
              <div className="settings-row-id">{m.id}</div>
              <div className="settings-row-sub">{m.provider} · {m.modelName}</div>
            </div>
            <div className="settings-row-actions">
              <button className="btn-settings-action" onClick={() => startEdit(m)}>Edit</button>
              <button className="btn-settings-action danger" onClick={() => handleDelete(m.id)}>Delete</button>
            </div>
          </div>
        )
      ))}
    </div>
  );
}
