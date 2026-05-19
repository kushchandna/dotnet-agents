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

export function SettingsPage() {
  const [activeTab, setActiveTab] = useState<Tab>('users');

  return (
    <div className="settings-page">
      <div className="settings-header">
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
