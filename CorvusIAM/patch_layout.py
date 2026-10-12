import codecs

path = r'C:\Users\Kaiiske\Pictures\CorvusIAM-testes\CorvusIAM\Views\Shared\_Layout.cshtml'
content = codecs.open(path, 'r', 'utf-8').read()

menu_item = '''
                <div class="px-3 text-[10px] font-[600] text-gray-400 dark:text-gray-500 uppercase tracking-wider mt-6 mb-2">Administração</div>
                <li>
                    <a href="/Admin/Integracoes" class="flex items-center gap-3 px-3 py-2 rounded-[6px] text-[13px] font-[500] text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-[#1B1F24] transition-colors">
                        <i class="ph ph-plugs-connected text-[18px]"></i> Integrações
                    </a>
                </li>
'''

if 'ph-plugs-connected' not in content:
    content = content.replace('<div class="px-3 text-[10px] font-[600] text-gray-400 dark:text-gray-500 uppercase tracking-wider mt-6 mb-2">Administração</div>', menu_item)
    codecs.open(path, 'w', 'utf-8-sig').write(content)
