import zipfile
import xml.etree.ElementTree as ET
import sys

def extract_text(docx_filename):
    try:
        with zipfile.ZipFile(docx_filename) as docx:
            xml_content = docx.read('word/document.xml')
            tree = ET.XML(xml_content)
            namespace = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
            paragraphs = []
            for paragraph in tree.iterfind('.//w:p', namespace):
                texts = [node.text for node in paragraph.iterfind('.//w:t', namespace) if node.text]
                if texts:
                    paragraphs.append(''.join(texts))
            return '\n'.join(paragraphs)
    except Exception as e:
        return str(e)

if __name__ == '__main__':
    text = extract_text(sys.argv[1])
    with open(sys.argv[2], 'w', encoding='utf-8') as f:
        f.write(text)
