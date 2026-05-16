interface Session { id: string; agentId: string; createdAt: string; title?: string | null }
interface Props {
  sessions: Session[];
  activeSessionId: string | null;
  onSelect: (id: string) => void;
  onNew: () => void;
  onDelete: (id: string) => void;
}
export function SessionList({ sessions, activeSessionId, onSelect, onNew, onDelete }: Props) {
  return (
    <div className="session-list" data-testid="session-list">
      <div className="session-list-header">
        <span className="session-list-label">Sessions</span>
        <button onClick={onNew} aria-label="New session" className="btn-new-session">+ New</button>
      </div>
      <ul>
        {sessions.map((s) => (
          <li
            key={s.id}
            className={s.id === activeSessionId ? 'active' : ''}
            data-testid={`session-item-${s.id}`}
          >
            <button className="session-select" onClick={() => onSelect(s.id)}>
              {s.title || new Date(s.createdAt).toLocaleString()}
            </button>
            <button
              className="session-delete"
              aria-label={`Delete session ${s.id}`}
              onClick={() => onDelete(s.id)}
            >×</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
