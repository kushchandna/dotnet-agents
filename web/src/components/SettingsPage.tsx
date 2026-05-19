import { useState } from 'react';
import { UsersTab } from './settings/UsersTab';
import { AgentsTab } from './settings/AgentsTab';
import { ModelsTab } from './settings/ModelsTab';
import { McpServersTab } from './settings/McpServersTab';
import { SkillsTab } from './settings/SkillsTab';
import { ToolsTab } from './settings/ToolsTab';

type Tab = 'users' | 'agents' | 'models' | 'mcp-servers' | 'skills' | 'tools';

const TABS: { id: Tab; label: string }[] = [
  { id: 'users', label: 'Users' },
  { id: 'agents', label: 'Agents' },
  { id: 'models', label: 'Models' },
  { id: 'mcp-servers', label: 'MCP Servers' },
  { id: 'skills', label: 'Skills' },
  { id: 'tools', label: 'Built-in Tools' },
];

export function SettingsPage({ onBack }: { onBack?: () => void }) {
  const [activeTab, setActiveTab] = useState<Tab>('users');

  return (
    <div className="settings-page">
      <div className="settings-header">
        {onBack && (
          <button className="icon-btn settings-back-btn" onClick={onBack} aria-label="Back to chat" title="Back to chat">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
          </button>
        )}
        <span className="settings-title">Settings</span>
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
        {activeTab === 'models' && <div data-testid="tab-models"><ModelsTab /></div>}
        {activeTab === 'mcp-servers' && <div data-testid="tab-mcp-servers"><McpServersTab /></div>}
        {activeTab === 'skills' && <div data-testid="tab-skills"><SkillsTab /></div>}
        {activeTab === 'tools' && <div data-testid="tab-tools"><ToolsTab /></div>}
      </div>
    </div>
  );
}
