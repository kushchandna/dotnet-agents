import { useCallback, useEffect, useState } from 'react';

export type Route = 'chat' | 'settings';

function pathToRoute(pathname: string): Route {
  return pathname.startsWith('/settings') ? 'settings' : 'chat';
}

function routeToPath(route: Route): string {
  return route === 'settings' ? '/settings' : '/';
}

export function useRoute(): { route: Route; navigate: (route: Route) => void } {
  const [route, setRoute] = useState<Route>(() =>
    typeof window === 'undefined' ? 'chat' : pathToRoute(window.location.pathname),
  );

  useEffect(() => {
    const onPop = () => setRoute(pathToRoute(window.location.pathname));
    window.addEventListener('popstate', onPop);
    return () => window.removeEventListener('popstate', onPop);
  }, []);

  const navigate = useCallback((next: Route) => {
    const path = routeToPath(next);
    if (window.location.pathname !== path) {
      window.history.pushState({}, '', path);
    }
    setRoute(next);
  }, []);

  return { route, navigate };
}
