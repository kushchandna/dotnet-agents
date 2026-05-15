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
  };

  const handleDelete = async (sid: string) => {
    if (!userId) return;
    await fetch(`/api/users/${userId}/sessions/${sid}`, { method: 'DELETE' });
    setSessions((xs) => xs.filter((x) => x.id !== sid));
    if (sessionId === sid) setSessionId(null);
  };

  return (
    <div className="app-layout" data-testid="app">
      <aside className="sidebar">
        <UserPicker  users={users}   selectedUserId={userId}   onSelect={setUserId} />
        <AgentPicker agents={agents} selectedAgentId={agentId} onSelect={setAgentId} />
        <SessionList sessions={sessions} activeSessionId={sessionId}
                     onSelect={setSessionId} onNew={handleNew} onDelete={handleDelete} />
      </aside>
      <main className="chat-area">
        {userId && sessionId ? (
          <ChatView userId={userId} sessionId={sessionId} />
        ) : (
          <div className="empty-state" data-testid="empty-state">
            Select a user, agent, and session to start chatting.
          </div>
        )}
      </main>
    </div>
  );
}
