import { useCallback, useRef, useState } from 'react';
import { streamChat, StreamEvent } from '../api/stream';

export function useStream(
  userId: string | null,
  sessionId: string | null,
  onEvent: (e: StreamEvent) => void,
) {
  const [streaming, setStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const send = useCallback(async (msg: string) => {
    if (!userId || !sessionId) return;
    abortRef.current?.abort();
    const ctl = new AbortController();
    abortRef.current = ctl;
    setStreaming(true); setError(null);
    try {
      for await (const evt of streamChat(userId, sessionId, msg, ctl.signal)) {
        onEvent(evt);
        if (evt.type === 'done' || evt.type === 'error') break;
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') setError((err as Error).message);
    } finally {
      setStreaming(false);
    }
  }, [userId, sessionId, onEvent]);

  const cancel = useCallback(() => abortRef.current?.abort(), []);
  return { streaming, error, send, cancel };
}
