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

  useEffect(() => {
    fetch(`/api/users/${userId}/sessions/${sessionId}/messages`)
      .then((r) => r.json()).then(setMessages).catch(() => setMessages([]));
    streamingRef.current = '';
    setStreamingContent('');
  }, [userId, sessionId]);

  const handleEvent = useCallback((e: StreamEvent) => {
    if (e.type === 'delta') {
      streamingRef.current += e.content;
      setStreamingContent(streamingRef.current);
    } else if (e.type === 'done') {
      setMessages((ms) => [...ms, { id: e.messageId, role: 'assistant', content: streamingRef.current }]);
      streamingRef.current = '';
      setStreamingContent('');
    } else if (e.type === 'error') {
      setMessages((ms) => [...ms, { id: `err-${Date.now()}`, role: 'assistant', content: `Error: ${e.message}` }]);
      streamingRef.current = '';
      setStreamingContent('');
    }
  }, []);

  const { streaming, send, cancel } = useStream(userId, sessionId, handleEvent);

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
        <button type="submit" disabled={streaming || !input.trim()} data-testid="send-button">Send</button>
        {streaming && <button type="button" onClick={cancel} data-testid="cancel-button">Cancel</button>}
      </form>
    </div>
  );
}
