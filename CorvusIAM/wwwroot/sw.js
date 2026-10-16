const CACHE_NAME = 'corvux-iam-pwa-v8';
const OFFLINE_URL = '/offline.html';

const PRECACHE_ASSETS = [
  OFFLINE_URL,
  '/manifest.json',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/icons/corvus-logo.png',
  '/icons/favicon-32x32.png'
];

self.addEventListener('install', event => {
  self.skipWaiting();
  event.waitUntil(
    caches.open(CACHE_NAME).then(async cache => {
      // Adiciona cada asset individualmente para evitar que uma falha isolada cancele todo o precache
      await Promise.allSettled(
        PRECACHE_ASSETS.map(url => 
          cache.add(new Request(url, { cache: 'reload' })).catch(err => {
            console.warn(`[PWA SW] Precache aviso para ${url}:`, err);
          })
        )
      );
    })
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys => {
      return Promise.all(
        keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') return;

  const url = new URL(event.request.url);

  // Ignora rotas de autenticação, APIs, downloads e métricas
  if (
    url.pathname.startsWith('/api/') || 
    url.pathname.startsWith('/Auth/') || 
    url.pathname.includes('Download') || 
    url.pathname.includes('Exportar') ||
    url.pathname.includes('/metrics')
  ) {
    return;
  }

  // Requisição de Navegação de Página (HTML)
  if (event.request.mode === 'navigate') {
    event.respondWith(
      fetch(event.request).catch(async () => {
        const cache = await caches.open(CACHE_NAME);
        const cachedOffline = await cache.match(OFFLINE_URL);
        return cachedOffline || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain' } });
      })
    );
    return;
  }

  // Assets Estáticos (Ícones, CSS, JS, Fontes)
  if (
    url.pathname.startsWith('/icons/') || 
    url.pathname.endsWith('.css') || 
    url.pathname.endsWith('.js') ||
    url.pathname.endsWith('.png') ||
    url.pathname.endsWith('.svg') ||
    url.pathname.endsWith('.woff2')
  ) {
    event.respondWith(
      caches.match(event.request).then(cachedResponse => {
        if (cachedResponse) {
          // Busca atualização em segundo plano (Stale-While-Revalidate)
          fetch(event.request).then(networkResponse => {
            if (networkResponse && networkResponse.status === 200) {
              caches.open(CACHE_NAME).then(cache => cache.put(event.request, networkResponse));
            }
          }).catch(() => {});
          return cachedResponse;
        }

        return fetch(event.request).then(response => {
          if (response && response.status === 200) {
            const clone = response.clone();
            caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
          }
          return response;
        });
      })
    );
  }
});
