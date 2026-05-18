import { useCallback, useEffect, useRef, useState } from 'react';
import { StreamEvent } from '../api/stream';
import { useStream } from '../hooks/useStream';
import { MessageBubble } from './MessageBubble';

interface Message { id: string; role: 'user' | 'assistant' | 'tool'; content: string | null; toolCalls?: unknown[] | null }

interface Props {
  userId: string;
  sessionId: string;
  initialMessages?: Message[];
}

export function ChatView({ userId, sessionId, initialMessages = [] }: Props) {
  const [messages, setMessages] = useState<Message[]>(initialMessages);
  const [input, setInput] = useState('');
  const streamingRef = useRef('');
  const [streamingContent, setStreamingContent] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);
  const pendingToolCallsRef = useRef<{ callId: string; name: string; arguments: string; result?: string }[]>([]);

  const handleEvent = useCallback((e: StreamEvent) => {
    if (e.type === 'delta') {
      streamingRef.current += e.content;
      setStreamingContent(streamingRef.current);
    } else if (e.type === 'tool_call') {
      pendingToolCallsRef.current.push({ callId: e.callId, name: e.name, arguments: e.arguments });
    } else if (e.type === 'tool_result') {
      const tc = pendingToolCallsRef.current.find((t) => t.callId === e.callId);
      if (tc) tc.result = e.result;
    } else if (e.type === 'done') {
      const content = streamingRef.current;
      const toolCalls = pendingToolCallsRef.current.length > 0 ? [...pendingToolCallsRef.current] : null;
      streamingRef.current = '';
      pendingToolCallsRef.current = [];
      setMessages((ms) => [...ms, { id: e.messageId, role: 'assistant', content, toolCalls }]);
      setStreamingContent('');
    } else if (e.type === 'error') {
      setMessages((ms) => [...ms, { id: `err-${Date.now()}`, role: 'assistant', content: `Error: ${e.message}` }]);
      streamingRef.current = '';
      pendingToolCallsRef.current = [];
      setStreamingContent('');
    }
  }, []);

  const { streaming, send, cancel } = useStream(userId, sessionId, handleEvent);

  useEffect(() => {
    cancel();
    const ac = new AbortController();
    fetch(`/api/users/${userId}/sessions/${sessionId}/messages`, { signal: ac.signal })
      .then((r) => r.json()).then(setMessages)
      .catch((err) => {
        if ((err as Error).name !== 'AbortError') setMessages([]);
      });
    streamingRef.current = '';
    pendingToolCallsRef.current = [];
    setStreamingContent('');
    return () => { ac.abort(); cancel(); };
  }, [userId, sessionId, cancel]);

  const submit = async (ev: React.FormEvent) => {
    ev.preventDefault();
    const msg = input.trim();
    if (!msg || streaming) return;
    setInput('');
    setMessages((ms) => [...ms, { id: `u-${Date.now()}`, role: 'user', content: msg }]);
    await send(msg);
  };

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages, streamingContent]);

  return (
    <div className="chat-view" data-testid="chat-view">
      <div className="messages" data-testid="messages-list">
        {messages.map((m) => <MessageBubble key={m.id} message={m as Parameters<typeof MessageBubble>[0]['message']} />)}
        {streamingContent && (
          <div className="message-bubble message-assistant streaming" data-testid="streaming-bubble">
            <div className="message-role">assistant</div>
            <div className="message-content">{streamingContent}</div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>
      <form onSubmit={submit} className="chat-input-form" data-testid="chat-form">
        <input value={input} onChange={(e) => setInput(e.target.value)}
               placeholder="Type a message…" disabled={streaming}
               data-testid="message-input" aria-label="Message input" />
        <button type="submit" disabled={streaming || !input.trim()} className="btn-send" data-testid="send-button" aria-label="Send">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="22" y1="2" x2="11" y2="13"/>
            <polygon points="22 2 15 22 11 13 2 9 22 2"/>
          </svg>
        </button>
        {streaming && <button type="button" onClick={cancel} className="btn-cancel" data-testid="cancel-button">Stop</button>}
      </form>
    </div>
  );
}
