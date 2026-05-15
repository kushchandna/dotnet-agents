import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { SessionList } from '../../src/components/SessionList';

const sessions = [
  { id: 'sess1', agentId: 'a', createdAt: new Date().toISOString() },
  { id: 'sess2', agentId: 'a', createdAt: new Date().toISOString() },
];

describe('SessionList', () => {
  it('renders sessions', () => {
    render(<SessionList sessions={sessions} activeSessionId={null}
                        onSelect={vi.fn()} onNew={vi.fn()} onDelete={vi.fn()} />);
    expect(screen.getByTestId('session-item-sess1')).toBeInTheDocument();
    expect(screen.getByTestId('session-item-sess2')).toBeInTheDocument();
  });

  it('invokes onNew', async () => {
    const onNew = vi.fn();
    render(<SessionList sessions={[]} activeSessionId={null}
                        onSelect={vi.fn()} onNew={onNew} onDelete={vi.fn()} />);
    await userEvent.click(screen.getByRole('button', { name: /New session/ }));
    expect(onNew).toHaveBeenCalledOnce();
  });

  it('invokes onDelete with session id', async () => {
    const onDelete = vi.fn();
    render(<SessionList sessions={sessions} activeSessionId={null}
                        onSelect={vi.fn()} onNew={vi.fn()} onDelete={onDelete} />);
    await userEvent.click(screen.getByLabelText('Delete session sess1'));
    expect(onDelete).toHaveBeenCalledWith('sess1');
  });

  it('marks active', () => {
    render(<SessionList sessions={sessions} activeSessionId="sess2"
                        onSelect={vi.fn()} onNew={vi.fn()} onDelete={vi.fn()} />);
    expect(screen.getByTestId('session-item-sess2')).toHaveClass('active');
  });
});
