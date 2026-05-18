import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RawView } from '../../src/components/RawView';

const mockData = {
  systemPrompt: 'You are helpful.',
  agent: { id: 'a1', name: 'TestAgent', description: null, modelId: 'm1', tools: [], skills: [], mcpServers: [] },
  model: { id: 'm1', provider: 'openai', modelName: 'gpt-4o', endpoint: null },
  messages: [{ id: 'msg1', role: 'user', content: 'Hello', toolCalls: null, timestamp: '2025-01-01T00:00:00Z' }],
};

describe('RawView', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders JSON stringified data when fetch resolves', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => mockData,
    } as Response);

    render(<RawView userId="u1" sessionId="s1" />);

    await waitFor(() => {
      expect(screen.getByText((_, el) =>
        el?.tagName === 'PRE' && el.textContent === JSON.stringify(mockData, null, 2)
      )).toBeInTheDocument();
    });
  });

  it('renders error message when fetch rejects', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error('network failure'));

    render(<RawView userId="u1" sessionId="s1" />);

    await waitFor(() => {
      expect(screen.getByText(/Failed to load raw view/)).toBeInTheDocument();
      expect(screen.getByText(/network failure/)).toBeInTheDocument();
    });
  });

  it('renders error message when fetch returns non-ok response', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: false,
      status: 404,
    } as Response);

    render(<RawView userId="u1" sessionId="s1" />);

    await waitFor(() => {
      expect(screen.getByText(/Failed to load raw view/)).toBeInTheDocument();
      expect(screen.getByText(/HTTP 404/)).toBeInTheDocument();
    });
  });

  it('calls the correct URL exactly once', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => mockData,
    } as Response);

    render(<RawView userId="u1" sessionId="s1" />);

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledTimes(1);
      expect(fetch).toHaveBeenCalledWith(
        '/api/users/u1/sessions/s1/raw',
        expect.objectContaining({ signal: expect.any(AbortSignal) }),
      );
    });
  });
});
