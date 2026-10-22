/**
 * CORVUX IAM — PWA MOBILE NATIVE JAVASCRIPT
 * Offline-First • IndexedDB Queue • Real-time Sync • Touch UI
 */

const api = '/api/Chamados';
let chamadosData = [];
let currentFilter = 'todos';
let deferredPrompt = null;

// IndexedDB Setup for Offline Resilience
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

// ==========================================
// NETWORK STATUS & ONLINE/OFFLINE LISTENERS
// ==========================================
function atualizarStatusRede() {
    const netPill = document.getElementById('netStatusPill');
    if (!netPill) return;

    if (navigator.onLine) {
        netPill.className = 'net-pill online';
        netPill.querySelector('.net-text').textContent = 'Online';
    } else {
        netPill.className = 'net-pill offline';
        netPill.querySelector('.net-text').textContent = 'Offline';
    }
}

window.addEventListener('online', () => {
    atualizarStatusRede();
    mostrarToast('Conexão restabelecida. Sincronizando chamados...', 'sucesso');
    syncData();
});

window.addEventListener('offline', () => {
    atualizarStatusRede();
    mostrarToast('Você está offline. Novos chamados serão salvos no dispositivo.', 'info');
});

// ==========================================
// TOAST NOTIFICATIONS
// ==========================================
let toastTimer = null;
function mostrarToast(mensagem, tipo = 'info') {
    const toast = document.getElementById('toast');
    if (!toast) return;

    clearTimeout(toastTimer);
    toast.textContent = mensagem;
    toast.className = `toast ${tipo}`;

    toastTimer = setTimeout(() => {
        toast.className = 'toast hidden';
    }, 3200);
}

// ==========================================
// TAB NAVIGATION (NATIVE BOTTOM DOCK)
// ==========================================
function mudarAbaMobile(aba) {
    const views = ['home', 'new', 'tickets', 'profile'];
    views.forEach(v => {
        const viewEl = document.getElementById(`view-${v}`);
        if (viewEl) {
            if (v === aba) {
                viewEl.classList.add('active');
            } else {
                viewEl.classList.remove('active');
            }
        }
    });

    const tabs = document.querySelectorAll('.app-bottom-nav .nav-tab');
    tabs.forEach(t => {
        if (t.getAttribute('data-tab') === aba) {
            t.classList.add('active');
        } else {
            t.classList.remove('active');
        }
    });

    // Scroll to top of viewport
    const viewport = document.querySelector('.app-viewport');
    if (viewport) viewport.scrollTo({ top: 0, behavior: 'smooth' });

    try {
        localStorage.setItem('corvux_mobile_last_tab', aba);
    } catch (e) {}

    // Feedback haptics if supported
    if ('vibrate' in navigator) {
        try { navigator.vibrate(10); } catch(e) {}
    }
}

// ==========================================
// TOUCH CHIPS FOR CATEGORY & PRIORITY
// ==========================================
function inicializarTouchChips() {
    const catChips = document.querySelectorAll('#categoryChips .chip');
    const catInput = document.getElementById('categoria');
    catChips.forEach(chip => {
        chip.addEventListener('click', () => {
            catChips.forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            if (catInput) catInput.value = chip.getAttribute('data-cat');
        });
    });

    const prioChips = document.querySelectorAll('#priorityChips .pill-chip');
    const prioInput = document.getElementById('prioridade');
    prioChips.forEach(chip => {
        chip.addEventListener('click', () => {
            prioChips.forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            if (prioInput) prioInput.value = chip.getAttribute('data-prio');
        });
    });

    // Filter pills in Tickets view
    const filterPills = document.querySelectorAll('#ticketFilterPills .filter-pill');
    filterPills.forEach(pill => {
        pill.addEventListener('click', () => {
            filterPills.forEach(p => p.classList.remove('active'));
            pill.classList.add('active');
            currentFilter = pill.getAttribute('data-filter');
            filtrarChamados();
        });
    });
}

function preencherCategoria(cat) {
    const catChips = document.querySelectorAll('#categoryChips .chip');
    const catInput = document.getElementById('categoria');
    if (catInput) catInput.value = cat;
    catChips.forEach(chip => {
        if (chip.getAttribute('data-cat') === cat) {
            chip.classList.add('active');
        } else {
            chip.classList.remove('active');
        }
    });
}

// ==========================================
// VOICE DICTATION (SPEECH-TO-TEXT)
// ==========================================
function inicializarDitadoVoz() {
    const btnVoice = document.getElementById('btnVoiceDictation');
    const textarea = document.getElementById('descricao');
    if (!btnVoice || !textarea) return;

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
        btnVoice.style.display = 'none';
        return;
    }

    const recognition = new SpeechRecognition();
    recognition.lang = 'pt-BR';
    recognition.continuous = false;
    recognition.interimResults = false;

    let isRecording = false;

    btnVoice.addEventListener('click', () => {
        if (isRecording) {
            recognition.stop();
            return;
        }

        try {
            recognition.start();
            isRecording = true;
            btnVoice.classList.add('recording');
            btnVoice.innerHTML = '<i class="ph ph-stop-circle"></i> Ouvindo...';
            mostrarToast('Fale agora para ditar seu chamado...', 'info');
        } catch (e) {
            isRecording = false;
            btnVoice.classList.remove('recording');
            btnVoice.innerHTML = '<i class="ph ph-microphone"></i> Ditar';
        }
    });

    recognition.onresult = event => {
        const transcript = event.results[0][0].transcript;
        if (transcript) {
            textarea.value = textarea.value ? `${textarea.value} ${transcript}` : transcript;
            mostrarToast('Texto ditado adicionado com sucesso.', 'sucesso');
        }
    };

    recognition.onerror = () => {
        mostrarToast('Não foi possível captar a voz. Tente novamente.', 'erro');
    };

    recognition.onend = () => {
        isRecording = false;
        btnVoice.classList.remove('recording');
        btnVoice.innerHTML = '<i class="ph ph-microphone"></i> Ditar';
    };
}

// ==========================================
// FETCH & RENDER TICKETS
// ==========================================
async function carregarChamados() {
    try {
        const resposta = await fetch(api);
        if (resposta.status === 401) {
            mostrarToast('Sessão expirada. Redirecionando para login...', 'erro');
            setTimeout(() => window.location.href = '/Auth/Login', 1500);
            return;
        }
        if (!resposta.ok) throw new Error('API indisponível');

        const data = await resposta.json();
        chamadosData = data.dados || data || [];
        localStorage.setItem('chamadosCache', JSON.stringify(chamadosData));
        renderizarTelas();
    } catch (err) {
        const cache = localStorage.getItem('chamadosCache');
        if (cache) {
            chamadosData = JSON.parse(cache);
            mostrarToast('Modo offline: exibindo chamados em cache.', 'info');
            renderizarTelas();
        } else {
            const listEl = document.getElementById('lista');
            if (listEl) {
                listEl.innerHTML = `
                    <div class="empty-state">
                        <i class="ph ph-wifi-slash"></i>
                        <p>Sem conexão de rede e sem dados locais armazenados.</p>
                    </div>`;
            }
        }
    }
}

function renderizarTelas() {
    // 1. Atualizar contadores no Início e na Tab bar
    const total = chamadosData.length;
    const abertos = chamadosData.filter(c => c.status !== 'Resolvido' && c.status !== 'Fechado').length;

    const elTotal = document.getElementById('homeTotalTickets');
    const elAtivos = document.getElementById('homeActiveTickets');
    const elBadge = document.getElementById('navTicketsCount');

    if (elTotal) elTotal.textContent = total;
    if (elAtivos) elAtivos.textContent = abertos;

    if (elBadge) {
        if (abertos > 0) {
            elBadge.textContent = abertos;
            elBadge.classList.remove('hidden');
        } else {
            elBadge.classList.add('hidden');
        }
    }

    // 2. Renderizar lista recente no Início (últimos 3)
    const homeRecentEl = document.getElementById('homeRecentList');
    if (homeRecentEl) {
        const ultimos = chamadosData.slice(0, 3);
        if (!ultimos.length) {
            homeRecentEl.innerHTML = `
                <div class="empty-state">
                    <i class="ph ph-check-circle"></i>
                    <p>Você não possui nenhum chamado registrado.</p>
                </div>`;
        } else {
            homeRecentEl.innerHTML = ultimos.map(c => renderCardChamado(c)).join('');
        }
    }

    // 3. Renderizar lista completa na aba Meus Chamados com filtros
    filtrarChamados();
}

function filtrarChamados() {
    const listEl = document.getElementById('lista');
    if (!listEl) return;

    const searchInput = document.getElementById('ticketSearchInput');
    const termo = (searchInput ? searchInput.value : '').toLowerCase().trim();

    let filtrados = chamadosData.filter(c => {
        // Filtro por status
        if (currentFilter === 'abertos') {
            if (c.status === 'Resolvido' || c.status === 'Fechado') return false;
        } else if (currentFilter === 'concluidos') {
            if (c.status !== 'Resolvido' && c.status !== 'Fechado') return false;
        }

        // Filtro por texto
        if (termo) {
            const assunto = (c.assunto || '').toLowerCase();
            const desc = (c.descricao || '').toLowerCase();
            const cat = (c.categoria || '').toLowerCase();
            return assunto.includes(termo) || desc.includes(termo) || cat.includes(termo);
        }

        return true;
    });

    if (!filtrados.length) {
        listEl.innerHTML = `
            <div class="empty-state">
                <i class="ph ph-magnifying-glass"></i>
                <p>Nenhum chamado encontrado com os filtros atuais.</p>
            </div>`;
    } else {
        listEl.innerHTML = filtrados.map(c => renderCardChamado(c)).join('');
    }
}

function renderCardChamado(c) {
    const statusClass = c.status === 'Resolvido' ? 'status-resolvido' :
                        c.status === 'Fechado' ? 'status-fechado' :
                        c.status === 'Pendente (Offline)' ? 'status-pendente' : 'status-aberto';

    const catIcon = c.categoria === 'Acesso' ? 'ph-lock-key' :
                    c.categoria === 'Hardware' ? 'ph-cpu' :
                    c.categoria === 'Software' ? 'ph-package' :
                    c.categoria === 'Rede' ? 'ph-wifi-high' : 'ph-shield-warning';

    const dataFormatada = c.dataAbertura ? new Date(c.dataAbertura).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }) : '-';

    return `
        <article class="ticket-card">
            <div class="ticket-top">
                <h4 class="ticket-title">${escapeHtml(c.assunto)}</h4>
                <span class="ticket-badge ${statusClass}">${escapeHtml(c.status || 'Aberto')}</span>
            </div>
            <p class="ticket-desc">${escapeHtml(c.descricao)}</p>
            <div class="ticket-meta">
                <span class="ticket-cat"><i class="ph ${catIcon}"></i> ${escapeHtml(c.categoria || 'Geral')} • Prio: ${escapeHtml(c.prioridade || 'Normal')}</span>
                <span class="font-mono">${dataFormatada}</span>
            </div>
        </article>
    `;
}

function escapeHtml(valor) {
    const el = document.createElement('div');
    el.textContent = valor || '';
    return el.innerHTML;
}

// ==========================================
// SUBMIT FORM WITH OFFLINE QUEUE
// ==========================================
document.getElementById('chamado-form')?.addEventListener('submit', async event => {
    event.preventDefault();

    const btnSubmit = document.getElementById('btnSubmitTicket');
    if (btnSubmit) {
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<div class="spinner" style="width:18px;height:18px;border-width:2px;"></div> Enviando...';
    }

    const catEl = document.getElementById('categoria');
    const prioEl = document.getElementById('prioridade');
    const assuntoEl = document.getElementById('assunto');
    const descEl = document.getElementById('descricao');

    const payload = {
        assunto: assuntoEl.value.trim(),
        descricao: descEl.value.trim(),
        categoria: catEl ? catEl.value : 'Acesso',
        prioridade: prioEl ? prioEl.value : 'Normal',
        clientIdempotencyKey: (window.crypto && crypto.randomUUID) ? crypto.randomUUID() : ('idmp-' + Date.now() + '-' + Math.random().toString(36).substring(2, 9)),
        timestamp: new Date().toISOString()
    };

    if (!navigator.onLine) {
        salvarNoIndexedDB(payload);
        event.target.reset();
        preencherCategoria('Acesso');
        mostrarToast('Sem conexão. Chamado salvo localmente e será enviado ao reconectar.', 'info');

        chamadosData.unshift({ ...payload, status: 'Pendente (Offline)', dataAbertura: payload.timestamp });
        localStorage.setItem('chamadosCache', JSON.stringify(chamadosData));
        renderizarTelas();
        mudarAbaMobile('tickets');

        if (btnSubmit) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = '<i class="ph ph-paper-plane-tilt"></i> <span>Enviar Chamado</span>';
        }
        return;
    }

    try {
        const resposta = await fetch(api, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Idempotency-Key': payload.clientIdempotencyKey
            },
            body: JSON.stringify(payload)
        });

        if (resposta.status === 401) {
            mostrarToast('Sessão expirada. Faça login novamente.', 'erro');
            setTimeout(() => window.location.href = '/Auth/Login', 1500);
            return;
        }

        if (!resposta.ok) {
            const errData = await resposta.json().catch(() => ({}));
            mostrarToast(errData.message || 'Erro ao processar chamado. Salvando offline...', 'erro');
            salvarNoIndexedDB(payload);
            return;
        }

        event.target.reset();
        preencherCategoria('Acesso');
        mostrarToast('Chamado registrado com sucesso!', 'sucesso');
        await carregarChamados();
        mudarAbaMobile('tickets');
    } catch (e) {
        salvarNoIndexedDB(payload);
        mostrarToast('Falha na conexão. Chamado enfileirado para sincronização.', 'info');
        chamadosData.unshift({ ...payload, status: 'Pendente (Offline)', dataAbertura: payload.timestamp });
        localStorage.setItem('chamadosCache', JSON.stringify(chamadosData));
        renderizarTelas();
        mudarAbaMobile('tickets');
    } finally {
        if (btnSubmit) {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = '<i class="ph ph-paper-plane-tilt"></i> <span>Enviar Chamado</span>';
        }
    }
});

function salvarNoIndexedDB(item) {
    if (!db) return;
    const tx = db.transaction('syncQueue', 'readwrite');
    tx.objectStore('syncQueue').add(item);
}

function syncData() {
    if (!db) return;
    const tx = db.transaction('syncQueue', 'readwrite');
    const store = tx.objectStore('syncQueue');
    const getAll = store.getAll();

    getAll.onsuccess = async () => {
        const chamadosPendentes = getAll.result;
        if (!chamadosPendentes.length) return;

        mostrarToast(`Sincronizando ${chamadosPendentes.length} chamado(s) salvo(s) offline...`, 'info');

        for (const c of chamadosPendentes) {
            try {
                const res = await fetch(api, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Idempotency-Key': c.clientIdempotencyKey
                    },
                    body: JSON.stringify(c)
                });
                if (res.ok || res.status === 409) {
                    const deleteTx = db.transaction('syncQueue', 'readwrite');
                    deleteTx.objectStore('syncQueue').delete(c.id);
                }
            } catch (err) {
                break;
            }
        }
        await carregarChamados();
    };
}

// ==========================================
// PWA INSTALL BANNER
// ==========================================
function checkIsMobileDevice() {
    const ua = navigator.userAgent || navigator.vendor || window.opera;
    const isMobileUA = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile/i.test(ua);
    const isTouch = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    const isNarrow = window.innerWidth <= 850;
    return isMobileUA || (isTouch && isNarrow);
}

window.addEventListener('beforeinstallprompt', e => {
    e.preventDefault();
    deferredPrompt = e;
    const banner = document.getElementById('pwa-install-banner');
    if (checkIsMobileDevice() && banner && localStorage.getItem('corvux_pwa_banner_closed') !== 'true') {
        banner.style.display = 'flex';
    }
});

document.getElementById('pwa-install-btn')?.addEventListener('click', async () => {
    if (!deferredPrompt) return;
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    if (outcome === 'accepted') {
        const banner = document.getElementById('pwa-install-banner');
        if (banner) banner.style.display = 'none';
    }
    deferredPrompt = null;
});

function fecharBannerPwa() {
    const banner = document.getElementById('pwa-install-banner');
    if (banner) banner.style.display = 'none';
    localStorage.setItem('corvux_pwa_banner_closed', 'true');
}

// ==========================================
// THEME & SETTINGS
// ==========================================
document.getElementById('btnThemeToggle')?.addEventListener('click', () => {
    const isDark = document.documentElement.classList.toggle('dark');
    localStorage.setItem('corvux-theme', isDark ? 'dark' : 'light');
    atualizarIconeTema();
});

function atualizarIconeTema() {
    const icon = document.getElementById('themeIcon');
    if (!icon) return;
    if (document.documentElement.classList.contains('dark')) {
        icon.className = 'ph ph-sun text-lg text-amber-400';
    } else {
        icon.className = 'ph ph-moon text-lg text-slate-700';
    }
}

function limparCacheOffline() {
    if (confirm('Deseja limpar todos os dados locais e sincronizar novamente da nuvem?')) {
        localStorage.removeItem('chamadosCache');
        if (db) {
            const tx = db.transaction('syncQueue', 'readwrite');
            tx.objectStore('syncQueue').clear();
        }
        mostrarToast('Cache local limpo com sucesso!', 'sucesso');
        carregarChamados();
    }
}

function limparSessaoLogout() {
    localStorage.removeItem('chamadosCache');
    localStorage.removeItem('corvux_mobile_last_tab');
}

// ==========================================
// INITIALIZATION
// ==========================================
window.addEventListener('DOMContentLoaded', () => {
    atualizarStatusRede();
    atualizarIconeTema();
    inicializarTouchChips();
    inicializarDitadoVoz();

    const lastTab = localStorage.getItem('corvux_mobile_last_tab') || 'home';
    mudarAbaMobile(lastTab);

    carregarChamados();
});
