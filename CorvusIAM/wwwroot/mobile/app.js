const api = '/api/Chamados';
const lista = document.getElementById('lista');
const mensagem = document.getElementById('mensagem');

// IndexedDB Setup
const dbName = 'CorvusOfflineDB';
let db;
const request = indexedDB.open(dbName, 1);
request.onupgradeneeded = event => {
  db = event.target.result;
  if (!db.objectStoreNames.contains('syncQueue')) {
    db.createObjectStore('syncQueue', { keyPath: 'id', autoIncrement: true });
  }
};
request.onsuccess = event => {
  db = event.target.result;
  if (navigator.onLine) syncData();
};

window.addEventListener('online', syncData);
window.addEventListener('offline', () => mostrarMensagem('Você está offline. Os chamados serão salvos localmente.', 'info'));

async function carregar() {
  try {
    const resposta = await fetch(api);
    if (resposta.status === 401) {
        mostrarMensagem('Sessão expirada. Redirecionando para login...', 'erro');
        setTimeout(() => window.location.href = '/Auth/Login', 2000);
        return;
    }
    if (!resposta.ok) throw new Error('API indisponível');
    
    const data = await resposta.json();
    const chamados = data.dados || data;
    localStorage.setItem('chamadosCache', JSON.stringify(chamados));
    renderChamados(chamados);
  } catch (err) {
    const cache = localStorage.getItem('chamadosCache');
    if (cache) {
        mostrarMensagem('Modo offline: exibindo dados em cache.', 'info');
        renderChamados(JSON.parse(cache));
    } else {
        mostrarMensagem('Não foi possível carregar os chamados (erro de rede).', 'erro');
    }
  }
}

function renderChamados(chamados) {
  lista.innerHTML = chamados.length 
    ? chamados.map(c => `<article class="item"><strong>${escapeHtml(c.assunto)}</strong><p>${escapeHtml(c.descricao)}</p><span class="status">${escapeHtml(c.status)} • ${new Date(c.dataAbertura).toLocaleDateString('pt-BR')}</span></article>`).join('') 
    : '<p>Nenhum chamado aberto.</p>';
}

function escapeHtml(valor) { 
  const el = document.createElement('div'); 
  el.textContent = valor || ''; 
  return el.innerHTML; 
}

function mostrarMensagem(texto, tipo) {
    mensagem.textContent = texto;
    mensagem.style.color = tipo === 'erro' ? 'red' : tipo === 'sucesso' ? 'green' : 'orange';
}

let deferredPrompt = null;
const installBanner = document.getElementById('pwa-install-banner');
const installBtn = document.getElementById('pwa-install-btn');

function checkIsMobileDevice() {
    const ua = navigator.userAgent || navigator.vendor || window.opera;
    const isMobileUA = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile/i.test(ua);
    const isTouch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    const isNarrow = window.innerWidth <= 850;
    return isMobileUA || (isTouch && isNarrow);
}

window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e;
    if (checkIsMobileDevice() && installBanner) installBanner.style.display = 'flex';
});

if (installBtn) {
    installBtn.addEventListener('click', async () => {
        if (!deferredPrompt) return;
        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        if (outcome === 'accepted') {
            if (installBanner) installBanner.style.display = 'none';
        }
        deferredPrompt = null;
    });
}

window.addEventListener('appinstalled', () => {
    if (installBanner) installBanner.style.display = 'none';
    deferredPrompt = null;
});

document.getElementById('chamado-form').addEventListener('submit', async event => {
  event.preventDefault(); 
  mensagem.textContent = '';
  
  const catEl = document.getElementById('categoria');
  const prioEl = document.getElementById('prioridade');

  const payload = { 
    assunto: document.getElementById('assunto').value, 
    descricao: document.getElementById('descricao').value, 
    categoria: catEl ? catEl.value : 'Acesso',
    prioridade: prioEl ? prioEl.value : 'Normal',
    timestamp: new Date().toISOString() 
  };
  
  if (!navigator.onLine) {
      salvarNoIndexedDB(payload);
      event.target.reset();
      mostrarMensagem('Sem conexão. Chamado salvo localmente e será enviado quando houver internet.', 'info');
      
      const cache = JSON.parse(localStorage.getItem('chamadosCache') || '[]');
      cache.unshift({ ...payload, status: 'Pendente (Offline)', dataAbertura: payload.timestamp });
      renderChamados(cache);
      return;
  }

  try {
      const resposta = await fetch(api, { 
          method:'POST', 
          headers:{'Content-Type':'application/json'}, 
          body:JSON.stringify(payload) 
      });
      
      if (resposta.status === 401) {
          mostrarMensagem('Sessão expirada. Faça login novamente.', 'erro');
          setTimeout(() => window.location.href = '/Auth/Login', 2000);
          return;
      }
      if (!resposta.ok) {
          const errData = await resposta.json().catch(() => ({}));
          mostrarMensagem(errData.message || 'Erro ao processar chamado. Salvando localmente...', 'erro');
          salvarNoIndexedDB(payload);
          return;
      }
      
      event.target.reset(); 
      mostrarMensagem('Chamado enviado com sucesso.', 'sucesso'); 
      carregar();
  } catch(e) {
      salvarNoIndexedDB(payload);
      mostrarMensagem('Erro de rede. Chamado salvo localmente.', 'info');
  }
});

function salvarNoIndexedDB(payload) {
    if(!db) return;
    const tx = db.transaction('syncQueue', 'readwrite');
    tx.objectStore('syncQueue').add(payload);
}

async function syncData() {
    if (!db) return;
    const tx = db.transaction('syncQueue', 'readonly');
    const request = tx.objectStore('syncQueue').getAll();
    request.onsuccess = async () => {
        const chamados = request.result;
        if(chamados.length === 0) {
            carregar();
            return;
        }
        
        mostrarMensagem('Sincronizando dados offline...', 'info');
        for (const c of chamados) {
            try {
                const resposta = await fetch(api, {
                    method: 'POST',
                    headers:{'Content-Type':'application/json'},
                    body: JSON.stringify(c)
                });
                
                if (resposta.status === 409 || resposta.ok) {
                    removerDoIndexedDB(c.id);
                } else if (resposta.status === 401) {
                    mostrarMensagem('Sessão expirada. Faça login para continuar a sincronização.', 'erro');
                    return; 
                }
            } catch(e) {
                return;
            }
        }
        mostrarMensagem('Sincronização concluída!', 'sucesso');
        setTimeout(() => mostrarMensagem('', ''), 3000);
        carregar();
    };
}

function removerDoIndexedDB(id) {
    db.transaction('syncQueue', 'readwrite').objectStore('syncQueue').delete(id);
}

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(() => {
        navigator.serviceWorker.register('/mobile/service-worker.js');
    });
}
