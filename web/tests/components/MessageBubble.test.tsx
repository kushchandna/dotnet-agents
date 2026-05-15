import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MessageBubble } from '../../src/components/MessageBubble';

describe('MessageBubble', () => {
  it('renders user message', () => {
    render(<MessageBubble message={{ id: '1', role: 'user', content: 'Hi' }} />);
    expect(screen.getByText('Hi')).toBeInTheDocument();
    expect(screen.getByText('user')).toBeInTheDocument();
  });

  it('applies role-specific class', () => {
    const { container } = render(
      <MessageBubble message={{ id: '2', role: 'assistant', content: 'Hello' }} />);
    expect(container.firstChild).toHaveClass('message-assistant');
  });

  it('shows tool calls summary', () => {
    render(<MessageBubble message={{
      id: '3', role: 'tool', content: null,
      toolCalls: [{ callId: 'c1', name: 'echo', arguments: '{}', result: 'hi' }]
    }} />);
    expect(screen.getByText(/Tool calls/)).toBeInTheDocument();
  });
});
