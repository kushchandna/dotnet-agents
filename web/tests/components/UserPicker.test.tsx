import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { UserPicker } from '../../src/components/UserPicker';

const users = [
  { id: 'alice', displayName: 'Alice' },
  { id: 'bob', displayName: 'Bob' },
];

describe('UserPicker', () => {
  it('renders all users', () => {
    render(<UserPicker users={users} selectedUserId={null} onSelect={vi.fn()} />);
    expect(screen.getByRole('option', { name: 'Alice' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Bob' })).toBeInTheDocument();
  });

  it('calls onSelect on change', async () => {
    const onSelect = vi.fn();
    render(<UserPicker users={users} selectedUserId={null} onSelect={onSelect} />);
    await userEvent.selectOptions(screen.getByRole('combobox'), 'alice');
    expect(onSelect).toHaveBeenCalledWith('alice');
  });

  it('reflects selectedUserId', () => {
    render(<UserPicker users={users} selectedUserId="bob" onSelect={vi.fn()} />);
    expect(screen.getByRole('combobox')).toHaveValue('bob');
  });
});
