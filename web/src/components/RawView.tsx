import { useEffect, useState } from 'react';

interface Props {
  userId: string;
  sessionId: string;
}

export function RawView({ userId, sessionId }: Props) {
  const [data, setData] = useState<unknown>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const ac = new AbortController();
    setLoading(true);
    setError(null);
    setData(null);
    fetch(`/api/users/${userId}/sessions/${sessionId}/raw`, { signal: ac.signal })
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then((json) => { setData(json); setLoading(false); })
      .catch((err) => {
        if ((err as Error).name === 'AbortError') return;
        setError((err as Error).message);
        setLoading(false);
      });
    return () => { ac.abort(); };
  }, [userId, sessionId]);

  if (loading) return <div className="raw-view-loading">Loading…</div>;
  if (error) return <div className="raw-view-error">Failed to load raw view: {error}</div>;

  return (
    <pre className="raw-view-pre">{JSON.stringify(data, null, 2)}</pre>
  );
}
