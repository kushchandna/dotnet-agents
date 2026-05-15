export type StreamEvent =
  | { type: 'delta'; content: string }
  | { type: 'tool_call'; callId: string; name: string; arguments: string }
  | { type: 'tool_result'; callId: string; name: string; result: string }
  | { type: 'done'; messageId: string }
  | { type: 'error'; message: string };

export async function* streamChat(
  userId: string,
  sessionId: string,
  content: string,
  signal: AbortSignal,
): AsyncIterable<StreamEvent> {
  const resp = await fetch(`/api/users/${userId}/sessions/${sessionId}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: JSON.stringify({ content }),
    signal,
  });
  if (!resp.ok || !resp.body) throw new Error(`HTTP ${resp.status}`);

  const reader = resp.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    let separator: number;
    while ((separator = buffer.indexOf('\n\n')) !== -1) {
      const eventBlock = buffer.slice(0, separator).trim();
      buffer = buffer.slice(separator + 2);
      if (!eventBlock.startsWith('data:')) continue;
      const json = eventBlock.slice(5).trim();
      if (!json) continue;
      yield JSON.parse(json) as StreamEvent;
    }
  }
}
