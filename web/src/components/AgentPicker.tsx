interface Props {
  agents: { id: string; name: string; description?: string | null }[];
  selectedAgentId: string | null;
  onSelect: (id: string) => void;
}
export function AgentPicker({ agents, selectedAgentId, onSelect }: Props) {
  return (
    <div className="agent-picker" data-testid="agent-picker">
      <label htmlFor="agent-select">Agent</label>
      <select id="agent-select" value={selectedAgentId ?? ''} onChange={(e) => onSelect(e.target.value)}>
        <option value="" disabled>Select an agent…</option>
        {agents.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
      </select>
    </div>
  );
}
