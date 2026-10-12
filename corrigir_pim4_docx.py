import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ET.register_namespace("w", W)


def make_paragraph(text, style=None):
    p = ET.Element(f"{{{W}}}p")
    if style:
        ppr = ET.SubElement(p, f"{{{W}}}pPr")
        pstyle = ET.SubElement(ppr, f"{{{W}}}pStyle")
        pstyle.set(f"{{{W}}}val", style)
    run = ET.SubElement(p, f"{{{W}}}r")
    node = ET.SubElement(run, f"{{{W}}}t")
    node.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    node.text = text
    return p


def paragraph_text(p):
    return "".join(n.text or "" for n in p.iter(f"{{{W}}}t"))


def replace_text(p, text):
    nodes = list(p.iter(f"{{{W}}}t"))
    if not nodes:
        return
    nodes[0].text = text
    for node in nodes[1:]:
        node.text = ""


def main(source, destination):
    with zipfile.ZipFile(source, "r") as zin:
        root = ET.fromstring(zin.read("word/document.xml"))
        body = root.find(f"{{{W}}}body")
        sectpr = body.find(f"{{{W}}}sectPr")

        organization = [
            make_paragraph("3.1.1 - CARACTERIZACAO DA ORGANIZACAO", "Heading1"),
            make_paragraph("Para fins deste PIM, foi adotada uma organizacao ficticia do setor corporativo de tecnologia e servicos administrativos, denominada CorvusIAM. A organizacao utiliza sistemas digitais para autenticacao, gestao de usuarios, atendimento interno e acesso a informacoes institucionais."),
            make_paragraph("Seus principais servicos sao o gerenciamento de identidades e acessos, o suporte aos usuarios, a administracao de perfis e permissoes e o acompanhamento de solicitacoes por chamados. A estrutura considerada inclui administracao, recursos humanos, contabilidade, colaboradores e equipe de suporte tecnico."),
            make_paragraph("Os processos criticos sao o cadastro e desligamento de usuarios, a concessao e revisao de permissoes, a autenticacao, o atendimento de chamados e a auditoria das acoes realizadas. Os problemas identificados sao o excesso de permissoes, a falta de rastreabilidade, a dispersao de informacoes e a dificuldade de acompanhar solicitacoes internas."),
            make_paragraph("A oportunidade de melhoria consiste em centralizar esses processos no CorvusIAM, com perfis, permissoes, logs de auditoria, APIs, suporte web/mobile e mecanismos de seguranca. Como a organizacao e ficticia, essa caracterizacao deve ser validada pelo grupo ou substituida pelos dados reais definidos no PIM III."),
        ]

        validation = [
            make_paragraph("NOTA DE VALIDACAO TECNICA", "Heading1"),
            make_paragraph("A documentacao foi ajustada para diferenciar funcionalidades implementadas, configuradas e ainda pendentes de validacao. A PWA possui manifesto, service worker, cache local e fila de sincronizacao, mas a integracao deve ser comprovada apos a correcao da rota da API e do JavaScript de renderizacao."),
            make_paragraph("O projeto possui configuracoes de Docker, GitHub Actions, health check e metricas. Entretanto, os resultados de build, testes automatizados e execucao dos containers devem ser anexados somente depois de uma execucao bem-sucedida."),
            make_paragraph("As figuras e anexos devem conter evidencias reais do sistema, identificando tela, perfil utilizado, data e resultado do teste. Marcadores de insercao nao devem ser apresentados como se fossem capturas de tela ou validacoes realizadas."),
        ]

        # Correct claims in the existing conclusion and annex instructions.
        for p in list(body):
            text = paragraph_text(p).strip()
            if text.startswith("Conclui-se que o CorvusIAM atende"):
                replace_text(p, "Conclui-se que o CorvusIAM atende conceitualmente aos principais objetivos do PIM IV, mas a conformidade final depende da correção da PWA, da compilação do projeto, da atualização do script SQL e da apresentação dos resultados reais dos testes.")
            elif text.startswith("A aplicacao mobile em formato PWA") or text.startswith("A aplicação mobile em formato PWA"):
                replace_text(p, "A aplicação mobile em formato PWA foi estruturada com manifesto, service worker, cache local e fila de sincronização. Sua integração funcional deve ser validada após a correção da rota da API e do JavaScript da interface.")
            elif text.startswith("A infraestrutura também foi planejada") or text.startswith("A infraestrutura tamb\u00e9m foi planejada"):
                replace_text(p, "A infraestrutura foi configurada em arquivos Docker, Docker Compose e GitHub Actions. Esses artefatos representam a configuração planejada; a execução do pipeline e dos containers deve ser comprovada por registros anexados ao trabalho.")
            elif text.startswith("Inserir neste espaço") or text.startswith("Inserir neste espaco"):
                replace_text(p, "Anexo digital: inserir aqui o arquivo técnico correspondente e sua evidência real de execução.")
            elif text.startswith("Inserir o conteúdo do arquivo") or text.startswith("Inserir o conteudo do arquivo"):
                replace_text(p, "Anexo digital: incluir o conteúdo do arquivo técnico correspondente após conferir sua compatibilidade com o código atual.")
            elif text.startswith("Local para inserir imagem"):
                replace_text(p, "Inserir evidência real da tela ou do diagrama, com legenda, fonte e identificação do teste.")

        # Insert the organization subsection before section 3.2.
        target = next((p for p in list(body) if paragraph_text(p).strip().startswith("3.2")), None)
        if target is not None:
            index = list(body).index(target)
            for p in reversed(organization):
                body.insert(index, p)

        # Insert the validation note before the existing conclusion.
        target = next((p for p in list(body) if paragraph_text(p).strip().startswith("4 CONCLUS")), None)
        if target is not None:
            index = list(body).index(target)
            for p in reversed(validation):
                body.insert(index, p)

        matrix = [
            make_paragraph("ANEXO J - MATRIZ DE CONFORMIDADE COM O MANUAL", "Heading1"),
            make_paragraph("Etapas 1 e 2 - Caracterizacao e planejamento: documentadas no desenvolvimento; validar os dados da organizacao ficticia com o PIM III."),
            make_paragraph("Etapas 3 e 4 - Responsabilidade social e aplicacao web: documentadas e parcialmente comprovadas pelos arquivos do projeto; anexar testes de acessibilidade."),
            make_paragraph("Etapa 5 - Aplicacao mobile: existem PWA e projeto Expo; documentar cada um separadamente e comprovar a integracao."),
            make_paragraph("Etapas 6 e 7 - Arquitetura e banco: documentadas; conferir se o script SQL corresponde as migrations e ao modelo atual."),
            make_paragraph("Etapas 8 e 9 - Cloud, DevOps e gestao agil: configuradas como plano; anexar resultados reais do pipeline, containers, sprints e Kanban."),
        ]
        for p in matrix:
            body.insert(list(body).index(sectpr), p)

        document_xml = ET.tostring(root, encoding="utf-8", xml_declaration=True)
        with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as zout:
            for item in zin.infolist():
                data = document_xml if item.filename == "word/document.xml" else zin.read(item.filename)
                zout.writestr(item, data)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Uso: corrigir_pim4_docx.py origem.docx destino.docx")
    main(Path(sys.argv[1]), Path(sys.argv[2]))
