content = '''@model IEnumerable<CorvusIAM.Entities.Usuario>
@{
    Layout = "_Layout";
    ViewData["Title"] = "Diretório de Usuários";
}

<div class="mb-[24px] flex justify-between items-end">
    <div>
        <h1 class="text-[16px] font-[600] text-gray-900 dark:text-white flex items-center gap-2">
            <i class="ph ph-users text-brand-500"></i> Diretório Corporativo
        </h1>
        <p class="text-[12px] text-gray-500 dark:text-gray-400 mt-1">Gerenciamento de identidades e acessos</p>
    </div>
    <div class="flex gap-2">
        <div class="relative">
            <i class="ph ph-magnifying-glass absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
            <input type="text" id="searchInput" placeholder="Buscar colaborador..." class="bg-white dark:bg-dark-card border border-gray-200 dark:border-dark-border text-gray-900 dark:text-gray-100 text-[12px] rounded-[6px] pl-9 pr-3 py-1.5 outline-none focus:border-brand-500 transition-colors w-[200px] shadow-sm" />
        </div>
        <button onclick="openModal('addUserModal')" class="bg-brand-600 hover:bg-[#1D4ED8] text-white text-[12px] font-medium px-3 py-1.5 rounded-[6px] shadow-sm transition-colors flex items-center gap-1.5">
            <i class="ph ph-user-plus"></i> Novo Usuário
        </button>
    </div>
</div>

<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-[24px]" id="userGrid">
    @foreach(var u in Model) {
        var initials = string.Join("", u.Nome.Split(' ').Take(2).Select(n => n.Substring(0,1)).ToArray()).ToUpper();
        <div class="user-card bento-card bg-white dark:bg-dark-card border border-gray-200 dark:border-dark-border rounded-[6px] p-[20px] flex flex-col relative overflow-hidden transition-all hover:border-brand-500 hover:shadow-lg">
            @if(!u.Ativo) {
                <div class="absolute top-0 right-0 bg-red-500 text-white text-[9px] font-bold px-2 py-1 rounded-bl-[6px] uppercase tracking-wider">Bloqueado</div>
            }
            <div class="flex items-center gap-4 mb-4">
                <div class="w-12 h-12 rounded-full flex items-center justify-center text-white font-bold text-[14px]" style="background: linear-gradient(135deg, #3b82f6, #8b5cf6)">
                    @initials
                </div>
                <div class="flex-1 overflow-hidden">
                    <h3 class="text-[14px] font-[600] text-gray-900 dark:text-white m-0 truncate" title="@u.Nome">@u.Nome</h3>
                    <p class="text-[11px] text-brand-600 dark:text-brand-400 font-mono truncate">@u.Email</p>
                </div>
            </div>
            
            <div class="flex flex-col gap-2 mb-4 text-[12px] text-gray-600 dark:text-gray-400">
                <div class="flex items-center gap-2">
                    <i class="ph ph-briefcase text-gray-400"></i> <span class="truncate">@u.Cargo - @u.Departamento</span>
                </div>
                <div class="flex items-center gap-2">
                    <i class="ph ph-shield-check text-emerald-500"></i> 
                    <span class="bg-gray-100 dark:bg-dark-bg px-2 py-0.5 rounded-[4px] text-[10px] font-[600] uppercase tracking-wider text-gray-700 dark:text-gray-300">
                        @(u.UsuarioPerfis?.FirstOrDefault()?.Perfil?.Nome ?? "Sem Perfil")
                    </span>
                </div>
            </div>

            <div class="mt-auto flex justify-between items-center pt-4 border-t border-gray-100 dark:border-dark-border">
                <div class="text-[10px] text-gray-400 flex items-center gap-1">
                    <i class="ph ph-clock"></i> Admissão: @(u.DataContratacao?.ToString("MMM/yy") ?? "-")
                </div>
                <div class="flex gap-2">
                    <button onclick="abrirEdit(@u.Id, '@u.Nome', '@u.CPF', '@u.Cargo', '@(u.UsuarioPerfis?.FirstOrDefault()?.Perfil?.Nome)', '@u.Departamento', '@u.Telefone')" class="text-gray-400 hover:text-brand-500 transition-colors" title="Editar">
                        <i class="ph ph-pencil-simple text-[16px]"></i>
                    </button>
                    <button onclick="inativarUsuario(@u.Id)" class="text-gray-400 hover:text-orange-500 transition-colors" title="@(u.Ativo ? "Bloquear" : "Desbloquear")">
                        <i class="ph @(u.Ativo ? "ph-lock-key" : "ph-lock-key-open") text-[16px]"></i>
                    </button>
                    <button onclick="excluirUsuario(@u.Id)" class="text-gray-400 hover:text-red-500 transition-colors" title="Excluir">
                        <i class="ph ph-trash text-[16px]"></i>
                    </button>
                </div>
            </div>
        </div>
    }
</div>

<!-- Modal Overlay -->
<div id="modalOverlay" class="fixed inset-0 bg-black/80 z-[100] hidden items-center justify-center p-4 backdrop-blur-sm transition-opacity">
    <!-- Modais omitidos para economia de log, manteremos a estrutura original dos modais de add/edit -->
</div>
'''

original = open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Views\Admin\Usuarios.cshtml', 'r', encoding='utf-8').read()
modais = original[original.find('<!-- Modal Overlay -->'):original.find('@section Scripts')]
scripts = original[original.find('@section Scripts'):]

final = content.replace('<!-- Modais omitidos para economia de log, manteremos a estrutura original dos modais de add/edit -->\n</div>', modais)
final += scripts

final = final.replace('<table', '<table style="display:none"') # hide old table if any leftovers

# Implement search script
search_script = '''
        document.getElementById('searchInput').addEventListener('input', function(e) {
            const term = e.target.value.toLowerCase();
            document.querySelectorAll('.user-card').forEach(card => {
                const text = card.innerText.toLowerCase();
                if(text.includes(term)) card.style.display = 'flex';
                else card.style.display = 'none';
            });
        });
'''
final = final.replace('</script>', search_script + '</script>')

open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Views\Admin\Usuarios.cshtml', 'w', encoding='utf-8').write(final)
