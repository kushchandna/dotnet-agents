import { useEffect, useState } from 'react';
import { AgentPicker } from './components/AgentPicker';
import { ChatView } from './components/ChatView';
import { SessionList } from './components/SessionList';
import { UserPicker } from './components/UserPicker';

interface User { id: string; displayName: string }
interface Agent { id: string; name: string; description?: string | null }
interface Session { id: string; agentId: string; createdAt: string; title?: string | null }

export default function App() {
  const [users, setUsers] = useState<User[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [userId, setUserId] = useState<string | null>(null);
  const [agentId, setAgentId] = useState<string | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [sidebarOpen, setSidebarOpen] = useState(() => window.innerWidth >= 768);

  useEffect(() => {
    fetch('/api/users').then((r) => r.json()).then(setUsers);
    fetch('/api/agents').then((r) => r.json()).then(setAgents);
  }, []);

  useEffect(() => {
    if (!userId) { setSessions([]); return; }
    fetch(`/api/users/${userId}/sessions`).then((r) => r.json()).then(setSessions);
  }, [userId]);

  const handleNew = async () => {
    if (!userId || !agentId) return;
    const r = await fetch(`/api/users/${userId}/sessions`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ agentId }),
    });
    const s: Session = await r.json();
    setSessions((xs) => [s, ...xs]);
    setSessionId(s.id);
    setSidebarOpen(false);
  };

  const handleDelete = async (sid: string) => {
    if (!userId) return;
    await fetch(`/api/users/${userId}/sessions/${sid}`, { method: 'DELETE' });
    setSessions((xs) => xs.filter((x) => x.id !== sid));
    if (sessionId === sid) setSessionId(null);
  };

  const handleSelectSession = (sid: string) => {
    setSessionId(sid);
    setSidebarOpen(false);
  };

  const activeSession = sessions.find((s) => s.id === sessionId);

  return (
    <div className="app-layout" data-testid="app">

      <header className="mobile-header">
        <button
          className="icon-btn"
          onClick={() => setSidebarOpen((o) => !o)}
          aria-label="Toggle menu"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            {sidebarOpen
              ? <><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></>
              : <><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></>}
          </svg>
        </button>
        <span className="mobile-header-title">
          {activeSession?.title ?? (sessionId ? 'Chat' : 'dotnet-agents')}
        </span>
        <div style={{ width: 36 }} />
      </header>

      <div
        className={`sidebar-backdrop${sidebarOpen ? ' open' : ''}`}
        onClick={() => setSidebarOpen(false)}
      />

      <aside className={`sidebar${sidebarOpen ? ' open' : ''}`}>
        <div className="sidebar-brand">dotnet · agents</div>
        <UserPicker users={users} selectedUserId={userId} onSelect={setUserId} />
        <AgentPicker agents={agents} selectedAgentId={agentId} onSelect={setAgentId} />
        <SessionList
          sessions={sessions}
          activeSessionId={sessionId}
          onSelect={handleSelectSession}
          onNew={handleNew}
          onDelete={handleDelete}
        />
      </aside>

      <main className="chat-area">
        {userId && sessionId ? (
          <ChatView userId={userId} sessionId={sessionId} />
        ) : (
          <div className="empty-state" data-testid="empty-state">
            <div className="empty-state-icon">◈</div>
            <p>No session selected</p>
            <span>Pick a user and agent, then create or select a session from the sidebar</span>
          </div>
        )}
      </main>

    </div>
  );
}
