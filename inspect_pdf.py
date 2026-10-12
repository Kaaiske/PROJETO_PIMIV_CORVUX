import fitz
p = r'c:/Users/Kaiiske/Desktop/1.pdf'
print('opening', p)
doc = fitz.open(p)
print('pages', len(doc))
for i in range(min(3, len(doc))):
    page = doc.load_page(i)
    txt = page.get_text('text')
    print('PAGE', i+1, 'chars', len(txt))
    print(txt[:3000])
    print('---END---')
