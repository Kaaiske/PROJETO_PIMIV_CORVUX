import React, { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import api from '../services/api';

type Policy = {
  id: number;
  nome: string;
  descricao?: string;
  ativa: boolean;
};

export default function PoliciesScreen() {
  const [policies, setPolicies] = useState<Policy[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<Policy[]>('/api/PoliticasAcesso')
      .then(response => setPolicies(response.data))
      .catch(error => console.error('Falha ao carregar políticas:', error))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <View style={styles.centered}><ActivityIndicator size="large" /></View>;
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={policies}
        keyExtractor={item => item.id.toString()}
        renderItem={({ item }) => (
          <View style={styles.item}>
            <View style={styles.titleRow}>
              <Text style={styles.title}>{item.nome}</Text>
              <Text style={[styles.status, item.ativa ? styles.active : styles.inactive]}>
                {item.ativa ? 'Ativa' : 'Inativa'}
              </Text>
            </View>
            {!!item.descricao && <Text style={styles.description}>{item.descricao}</Text>}
          </View>
        )}
        ListEmptyComponent={<Text style={styles.text}>Nenhuma política encontrada.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  item: { paddingVertical: 16, borderBottomWidth: 1, borderBottomColor: '#ddd' },
  titleRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  title: { flex: 1, fontSize: 17, fontWeight: 'bold' },
  description: { marginTop: 8, color: '#555', lineHeight: 20 },
  status: { marginLeft: 12, fontWeight: 'bold' },
  active: { color: '#16803c' },
  inactive: { color: '#9b1c1c' },
  text: { fontSize: 16, textAlign: 'center', marginTop: 24 },
});
