import { useEffect, useState } from 'react';

interface User { id: string; displayName: string }
interface FormState { id: string; displayName: string }
const empty = (): FormState => ({ id: '', displayName: '' });

export function UsersTab() {
  const [users, setUsers] = useState<User[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(empty());
  const [error, setError] = useState<string | null>(null);

  const load = () => fetch('/api/config/users').then(r => r.json()).then(setUsers)
    .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Failed to load'));
  useEffect(() => { load(); }, []);

  const handleError = async (res: Response) => {
    const body = await res.json().catch(() => null);
    const msg = Array.isArray(body)
      ? body.map((e: { path: string; message: string }) => `${e.path}: ${e.message}`).join('; ')
      : `Error ${res.status}`;
    setError(msg);
  };

  const submitAdd = async () => {
    setError(null);
    const res = await fetch('/api/config/users', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form),
    });
    if (!res.ok) { await handleError(res); return; }
    setShowAdd(false); setForm(empty()); load();
  };

  const submitEdit = async (id: string) => {
    setError(null);
    const res = await fetch(`/api/config/users/${id}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form),
    });
    if (!res.ok) { await handleError(res); return; }
    setEditingId(null); setForm(empty()); load();
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm(`Delete "${id}"? This cannot be undone.`)) return;
    setError(null);
    const res = await fetch(`/api/config/users/${id}`, { method: 'DELETE' });
    if (!res.ok) { await handleError(res); return; }
    load();
  };

  const startEdit = (u: User) => {
    setEditingId(u.id); setShowAdd(false);
    setForm({ id: u.id, displayName: u.displayName }); setError(null);
  };

  const cancelForm = () => { setShowAdd(false); setEditingId(null); setForm(empty()); setError(null); };

  return (
    <div className="settings-section">
      <div className="settings-section-header">
        <span className="settings-section-title">Users</span>
        {!showAdd && editingId === null && (
          <button className="btn-settings-add" onClick={() => { setShowAdd(true); setEditingId(null); setForm(empty()); setError(null); }}>
            + Add User
          </button>
        )}
      </div>
      {error && <div className="settings-error section">{error}</div>}
      {showAdd && (
        <div className="settings-form">
          <div className="settings-form-row">
            <label className="settings-form-label">ID</label>
            <input className="settings-input" value={form.id} onChange={e => setForm(f => ({ ...f, id: e.target.value }))} placeholder="user-id" />
          </div>
          <div className="settings-form-row">
            <label className="settings-form-label">Display Name</label>
            <input className="settings-input" value={form.displayName} onChange={e => setForm(f => ({ ...f, displayName: e.target.value }))} placeholder="Alice" />
          </div>
          <div className="settings-form-actions">
            <button className="btn-settings-action" onClick={cancelForm}>Cancel</button>
            <button className="btn-settings-add" onClick={submitAdd}>Save</button>
          </div>
        </div>
      )}
      {users.map(u => (
        editingId === u.id ? (
          <div className="settings-form" key={u.id}>
            <div className="settings-form-row">
              <label className="settings-form-label">Display Name</label>
              <input className="settings-input" value={form.displayName} onChange={e => setForm(f => ({ ...f, displayName: e.target.value }))} />
            </div>
            <div className="settings-form-actions">
              <button className="btn-settings-action" onClick={cancelForm}>Cancel</button>
              <button className="btn-settings-add" onClick={() => submitEdit(u.id)}>Save</button>
            </div>
          </div>
        ) : (
          <div className="settings-list-row" key={u.id}>
            <div className="settings-row-info">
              <div className="settings-row-id">{u.displayName}</div>
              <div className="settings-row-sub">{u.id}</div>
            </div>
            <div className="settings-row-actions">
              <button className="btn-settings-action" onClick={() => startEdit(u)}>Edit</button>
              <button className="btn-settings-action danger" onClick={() => handleDelete(u.id)}>Delete</button>
            </div>
          </div>
        )
      ))}
    </div>
  );
}
