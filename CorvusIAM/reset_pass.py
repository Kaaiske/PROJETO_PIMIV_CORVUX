import sqlite3

conn = sqlite3.connect('corvusiam.db')
cur = conn.cursor()
new_hash = '$2a$11$Wx90/bOem6Ugit.brPVUiu5rOWUL03tbG35cvCjUI8uSeu.ep0WiO'
cur.execute('UPDATE Usuarios SET SenhaHash = ? WHERE Email = ?', (new_hash, 'admin@corvus.com'))
conn.commit()
print("UPDATED!")
conn.close()
