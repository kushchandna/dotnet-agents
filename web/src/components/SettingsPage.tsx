import { useState } from 'react';
import { UsersTab } from './settings/UsersTab';
import { AgentsTab } from './settings/AgentsTab';
import { McpServersTab } from './settings/McpServersTab';
import { ToolsTab } from './settings/ToolsTab';

type Tab = 'users' | 'agents' | 'mcp-servers' | 'tools';

interface Props { onClose: () => void }

const TABS: { id: Tab; label: string }[] = [
  { id: 'users', label: 'Users' },
  { id: 'agents', label: 'Agents' },
  { id: 'mcp-servers', label: 'MCP Servers' },
  { id: 'tools', label: 'Built-in Tools' },
];

export function SettingsPage({ onClose }: Props) {
  const [activeTab, setActiveTab] = useState<Tab>('users');

  return (
    <div className="settings-page">
      <div className="settings-header">
        <span className="settings-title">Settings</span>
        <button className="icon-btn" onClick={onClose} aria-label="Close settings">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <line x1="18" y1="6" x2="6" y2="18"/>
            <line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
      </div>
      <div className="settings-tabs">
        {TABS.map((t) => (
          <button
            key={t.id}
            className={`settings-tab${activeTab === t.id ? ' active' : ''}`}
            onClick={() => setActiveTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>
      <div className="settings-content">
        {activeTab === 'users' && <div data-testid="tab-users"><UsersTab /></div>}
        {activeTab === 'agents' && <div data-testid="tab-agents"><AgentsTab /></div>}
        {activeTab === 'mcp-servers' && <div data-testid="tab-mcp-servers"><McpServersTab /></div>}
        {activeTab === 'tools' && <div data-testid="tab-tools"><ToolsTab /></div>}
      </div>
    </div>
  );
}
