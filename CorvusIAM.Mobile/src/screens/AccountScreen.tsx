import React, { useEffect, useState } from 'react';
import { ActivityIndicator, View, Text, StyleSheet, Alert } from 'react-native';
import api from '../services/api';

type Usuario = {
  id: number;
  nome: string;
  email: string;
  ativo: boolean;
  cargo?: string;
  departamento?: string;
  perfilId?: number;
};

export default function AccountScreen() {
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<Usuario>('/api/Usuarios/me')
      .then(response => setUsuario(response.data))
      .catch(() => Alert.alert('Erro', 'Não foi possível carregar seus dados.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <View style={styles.container}><ActivityIndicator size="large" /></View>;
  }

  if (!usuario) {
    return <View style={styles.container}><Text style={styles.text}>Dados indisponíveis.</Text></View>;
  }

  return (
    <View style={styles.content}>
      <Text style={styles.title}>Minha conta</Text>
      <Text style={styles.label}>Nome</Text><Text style={styles.value}>{usuario.nome}</Text>
      <Text style={styles.label}>Email</Text><Text style={styles.value}>{usuario.email}</Text>
      <Text style={styles.label}>Cargo</Text><Text style={styles.value}>{usuario.cargo || 'Não informado'}</Text>
      <Text style={styles.label}>Departamento</Text><Text style={styles.value}>{usuario.departamento || 'Não informado'}</Text>
      <Text style={styles.label}>Status</Text><Text style={styles.value}>{usuario.ativo ? 'Ativo' : 'Inativo'}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  content: { flex: 1, padding: 20 },
  title: { fontSize: 24, fontWeight: 'bold', marginBottom: 24 },
  label: { fontSize: 12, color: '#666', marginTop: 12 },
  value: { fontSize: 17, marginTop: 4 },
  text: { fontSize: 18 },
});
