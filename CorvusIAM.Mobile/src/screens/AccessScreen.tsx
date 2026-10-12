import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, FlatList, ActivityIndicator } from 'react-native';
import api from '../services/api';

type Permission = {
  name: string;
  description?: string;
};

export default function AccessScreen() {
  const [accessList, setAccessList] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/api/Access')
      .then(res => setAccessList(res.data))
      .catch(err => console.error(err))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <View style={styles.centered}><ActivityIndicator size="large" /></View>;

  return (
    <View style={styles.container}>
      <FlatList
        data={accessList}
        keyExtractor={(item, index) => index.toString()}
        renderItem={({ item }) => (
          <View style={styles.item}>
            <Text style={styles.name}>{item.name}</Text>
            {!!item.description && <Text>{item.description}</Text>}
          </View>
        )}
        ListEmptyComponent={<Text>Nenhum acesso encontrado.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20 },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  item: { padding: 15, borderBottomWidth: 1, borderBottomColor: '#ccc' },
  name: { fontWeight: 'bold' }
});
