import re

content = open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Views\Admin\Index.cshtml', 'r', encoding='utf-8').read()

# Fix export button
content = content.replace('<button class="bg-white dark:bg-dark-card border border-gray-200 dark:border-dark-border text-gray-700 dark:text-gray-300 text-[12px] font-medium px-3 py-1.5 rounded-[6px] shadow-sm hover:bg-gray-50 dark:hover:bg-[#1B1F24] transition-colors flex items-center gap-1.5">\n        <i class="ph ph-download-simple"></i> Exportar Dados\n    </button>', '<a href="/Admin/DownloadRelatorio" class="bg-white dark:bg-dark-card border border-gray-200 dark:border-dark-border text-gray-700 dark:text-gray-300 text-[12px] font-medium px-3 py-1.5 rounded-[6px] shadow-sm hover:bg-gray-50 dark:hover:bg-[#1B1F24] transition-colors flex items-center gap-1.5">\n        <i class="ph ph-download-simple"></i> Exportar Dados\n    </a>')


# Replace Integracoes grid
old_integracoes = re.search(r'<div class="grid grid-cols-2 gap-4">.*?</div>\s*</div>\s*<!-- Acessos', content, re.DOTALL).group(0)
new_integracoes = '''<div class="grid grid-cols-2 gap-4">
            @foreach(var mod in Model.Modulos) {
            <div class="flex justify-between items-center p-3 border border-gray-100 dark:border-[#272B33] rounded-[6px]">
                <div class="flex items-center gap-3">
                    <i class="ph @mod.Icone text-[24px] text-gray-600 dark:text-gray-400"></i>
                    <div class="flex flex-col">
                        <span class="text-[13px] font-[500] text-gray-900 dark:text-white">@mod.Nome</span>
                        <span class="text-[10px] text-gray-500">@mod.Detalhe</span>
                    </div>
                </div>
                <span class="w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.8)]"></span>
            </div>
            }
            @if(!Model.Modulos.Any()) {
                <div class="col-span-2 text-center text-gray-500 text-[12px] p-4">Nenhuma integração cadastrada. Vá para Integrações no painel.</div>
            }
        </div>
    </div>
    <!-- Acessos'''
content = content.replace(old_integracoes, new_integracoes)

# Inject real data into JS Charts
content = content.replace('data: [120, 150, 180, 130, 170, 80, 60]', f'data: [{string.Join(",", Model.LoginsNormaisPorDia)}]')

import json
content = content.replace('data: [120, 150, 180, 130, 170, 80, 60]', 'data: @Html.Raw(Json.Serialize(Model.LoginsNormaisPorDia))')
content = content.replace('data: [5, 12, 8, 20, 15, 2, 1]', 'data: @Html.Raw(Json.Serialize(Model.BloqueiosPorDia))')
content = content.replace('data: [60, 25, 15]', 'data: @Html.Raw(Json.Serialize(Model.AcessosPlataforma))')


open(r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Views\Admin\Index.cshtml', 'w', encoding='utf-8').write(content)
