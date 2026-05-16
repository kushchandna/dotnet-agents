interface Props {
  users: { id: string; displayName: string }[];
  selectedUserId: string | null;
  onSelect: (id: string) => void;
}
export function UserPicker({ users, selectedUserId, onSelect }: Props) {
  return (
    <div className="picker-group" data-testid="user-picker">
      <label className="picker-label" htmlFor="user-select">User</label>
      <select
        id="user-select"
        className="custom-select"
        value={selectedUserId ?? ''}
        onChange={(e) => { if (e.target.value) onSelect(e.target.value); }}
      >
        <option value="" disabled>Select a user…</option>
        {users.map((u) => <option key={u.id} value={u.id}>{u.displayName}</option>)}
      </select>
    </div>
  );
}
