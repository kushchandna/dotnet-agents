interface ToolCall { callId: string; name: string; arguments: string; result?: string }
interface Message { id: string; role: 'user' | 'assistant' | 'tool' | 'system'; content: string | null; toolCalls?: ToolCall[] | null }

export function MessageBubble({ message }: { message: Message }) {
  return (
    <div className={`message-bubble message-${message.role}`} data-testid={`message-${message.id}`}>
      <div className="message-role">{message.role}</div>
      {message.content && <div className="message-content">{message.content}</div>}
      {message.toolCalls && message.toolCalls.length > 0 && (
        <details className="tool-calls">
          <summary>Tool calls ({message.toolCalls.length})</summary>
          {message.toolCalls.map((tc) => (
            <div key={tc.callId} className="tool-call">
              <code>{tc.name}({tc.arguments})</code>
              {tc.result && <pre>{tc.result}</pre>}
            </div>
          ))}
        </details>
      )}
    </div>
  );
}
