import { useEffect, useState } from 'react';
import { AgentPicker } from './components/AgentPicker';
import { ChatView } from './components/ChatView';
import { RawView } from './components/RawView';
import { SessionList } from './components/SessionList';
import { SettingsPage } from './components/SettingsPage';
import { UserPicker } from './components/UserPicker';
import { useRoute } from './hooks/useRoute';
import { getCookie, setCookie } from './lib/storage';

interface User { id: string; displayName: string }
interface Agent { id: string; name: string; description?: string | null }
interface Session { id: string; agentId: string; createdAt: string; title?: string | null }

const COOKIE_USER = 'da_last_user';
const COOKIE_AGENT = 'da_last_agent';
const LS_SIDEBAR_PINNED = 'da_sidebar_pinned';

const isDesktop = () => typeof window !== 'undefined' && window.innerWidth >= 769;

export default function App() {
  const { route, navigate } = useRoute();
  const [users, setUsers] = useState<User[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [userId, setUserId] = useState<string | null>(null);
  const [agentId, setAgentId] = useState<string | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [chatView, setChatView] = useState<'chat' | 'raw'>('chat');
  const [pinned, setPinned] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false;
    return localStorage.getItem(LS_SIDEBAR_PINNED) === '1';
  });
  const [sidebarOpen, setSidebarOpen] = useState(() => isDesktop());

  useEffect(() => {
    fetch('/api/users').then((r) => r.json()).then(setUsers);
    fetch('/api/agents').then((r) => r.json()).then(setAgents);
  }, []);

  useEffect(() => {
    if (users.length === 0 || userId) return;
    const saved = getCookie(COOKIE_USER);
    if (saved && users.some((u) => u.id === saved)) setUserId(saved);
  }, [users, userId]);

  useEffect(() => {
    if (agents.length === 0 || agentId) return;
    const saved = getCookie(COOKIE_AGENT);
    if (saved && agents.some((a) => a.id === saved)) setAgentId(saved);
  }, [agents, agentId]);

  useEffect(() => {
    if (!userId) { setSessions([]); return; }
    fetch(`/api/users/${userId}/sessions`).then((r) => r.json()).then(setSessions);
  }, [userId]);

  useEffect(() => { setChatView('chat'); }, [userId, sessionId]);

  const handleSelectUser = (id: string) => {
    setUserId(id);
    setCookie(COOKIE_USER, id);
  };

  const handleSelectAgent = (id: string) => {
    setAgentId(id);
    setCookie(COOKIE_AGENT, id);
  };

  const handleNew = async () => {
    if (!userId || !agentId) return;
    const r = await fetch(`/api/users/${userId}/sessions`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ agentId }),
    });
    const s: Session = await r.json();
    setSessions((xs) => [s, ...xs]);
    setSessionId(s.id);
    if (!pinned) setSidebarOpen(false);
  };

  const handleDelete = async (sid: string) => {
    if (!userId) return;
    await fetch(`/api/users/${userId}/sessions/${sid}`, { method: 'DELETE' });
    setSessions((xs) => xs.filter((x) => x.id !== sid));
    if (sessionId === sid) setSessionId(null);
  };

  const handleSelectSession = (sid: string) => {
    setSessionId(sid);
    if (!pinned) setSidebarOpen(false);
  };

  const togglePin = () => {
    setPinned((p) => {
      const next = !p;
      localStorage.setItem(LS_SIDEBAR_PINNED, next ? '1' : '0');
      if (next) setSidebarOpen(true);
      return next;
    });
  };

  const activeSession = sessions.find((s) => s.id === sessionId);
  const effectivePinned = pinned && isDesktop();

  return (
    <div
      className={`app-layout${effectivePinned ? ' sidebar-pinned' : ''}`}
      data-testid="app"
    >

      <header className="mobile-header">
        {effectivePinned ? <div style={{ width: 36 }} /> : (
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
        )}
        <span className="mobile-header-title">
          {route === 'settings' ? 'Settings' : activeSession?.title ?? (sessionId ? 'Chat' : 'dotnet-agents')}
        </span>
        <div style={{ width: 36 }} />
      </header>

      <div
        className={`sidebar-backdrop${sidebarOpen && !effectivePinned ? ' open' : ''}`}
        onClick={() => setSidebarOpen(false)}
      />

      <aside className={`sidebar${sidebarOpen ? ' open' : ''}${effectivePinned ? ' pinned' : ''}`}>
        <div className="sidebar-brand-row">
          <div className="sidebar-brand">dotnet · agents</div>
          <button
            className={`icon-btn pin-btn${pinned ? ' active' : ''}`}
            onClick={togglePin}
            aria-label={pinned ? 'Unpin sidebar' : 'Pin sidebar'}
            title={pinned ? 'Unpin sidebar' : 'Pin sidebar'}
          >
            <svg viewBox="0 0 24 24" fill={pinned ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 17v5"/>
              <path d="M9 10.76V4h6v6.76a2 2 0 0 0 .59 1.41l1.7 1.7A1 1 0 0 1 16.59 16H7.41a1 1 0 0 1-.7-1.71l1.7-1.7A2 2 0 0 0 9 10.76z"/>
            </svg>
          </button>
        </div>
        <UserPicker users={users} selectedUserId={userId} onSelect={handleSelectUser} />
        <AgentPicker agents={agents} selectedAgentId={agentId} onSelect={handleSelectAgent} />
        <SessionList
          sessions={sessions}
          activeSessionId={sessionId}
          onSelect={handleSelectSession}
          onNew={handleNew}
          onDelete={handleDelete}
        />
        <button
          className="icon-btn settings-btn"
          onClick={() => { navigate(route === 'settings' ? 'chat' : 'settings'); if (!pinned) setSidebarOpen(false); }}
          aria-label="Settings"
          title="Settings"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
          </svg>
        </button>
      </aside>

      <main className="chat-area">
        {route === 'settings' ? (
          <SettingsPage />
        ) : (
          <>
            {userId && sessionId && (
              <div className="chat-view-tabs" data-testid="chat-view-tabs" role="tablist" aria-label="View">
                <button
                  className={`chat-view-tab${chatView === 'chat' ? ' active' : ''}`}
                  onClick={() => setChatView('chat')}
                  role="tab"
                  aria-selected={chatView === 'chat'}
                >Chat</button>
                <button
                  className={`chat-view-tab${chatView === 'raw' ? ' active' : ''}`}
                  onClick={() => setChatView('raw')}
                  role="tab"
                  aria-selected={chatView === 'raw'}
                >Raw</button>
              </div>
            )}
            {userId && sessionId ? (
              chatView === 'raw'
                ? <RawView userId={userId} sessionId={sessionId} />
                : <ChatView userId={userId} sessionId={sessionId} />
            ) : (
              <div className="empty-state" data-testid="empty-state">
                <div className="empty-state-icon">◈</div>
                <p>No session selected</p>
                <span>Pick a user and agent, then create or select a session from the sidebar</span>
              </div>
            )}
          </>
        )}
      </main>

    </div>
  );
}
