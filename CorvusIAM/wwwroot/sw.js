const CACHE_NAME = 'corvux-iam-pwa-v16';
const OFFLINE_URL = '/offline.html';

const PRECACHE_ASSETS = [
  OFFLINE_URL,
  '/manifest.json?v=20261018',
  '/mobile/manifest.webmanifest?v=20261021',
  '/mobile/app.css?v=20261018',
  '/mobile/app.js?v=20261018',
  '/icons/corvux-icon-192.png?v=20261018',
  '/icons/corvux-icon-512.png?v=20261018',
  '/icons/corvus-logo-light.png?v=20261018',
  '/icons/corvus-logo-dark.png?v=20261018',
  '/icons/favicon-32x32.png?v=20261018'
];

self.addEventListener('install', event => {
  self.skipWaiting();
  event.waitUntil(
    caches.open(CACHE_NAME).then(async cache => {
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

  // APIs transacionais, downloads de exportação e métricas do Prometheus
  if (
    url.pathname.startsWith('/api/') || 
    url.pathname.includes('Download') || 
    url.pathname.includes('Exportar') ||
    url.pathname.includes('/metrics')
  ) {
    return;
  }

  // Requisição de Navegação de Página (HTML) — Network First com Fallback Seguro para Offline
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

  // Manifest e Ícones usam Network-First para sempre refletir a identidade visual atualizada
  if (
    url.pathname.startsWith('/icons/') ||
    url.pathname.endsWith('manifest.json') ||
    url.pathname.endsWith('manifest.webmanifest') ||
    url.pathname.endsWith('.ico') ||
    url.pathname.endsWith('.png')
  ) {
    event.respondWith(
      fetch(event.request).then(response => {
        if (response && response.status === 200) {
          const clone = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
        }
        return response;
      }).catch(() => caches.match(event.request))
    );
    return;
  }

  // Demais Assets Estáticos (CSS, JS, Fontes) — Cache First com revalidação em segundo plano
  if (
    url.pathname.endsWith('.css') || 
    url.pathname.endsWith('.js') ||
    url.pathname.endsWith('.svg') ||
    url.pathname.endsWith('.woff2')
  ) {
    event.respondWith(
      caches.match(event.request).then(cachedResponse => {
        if (cachedResponse) {
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
