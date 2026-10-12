import React, { useEffect, useState } from "react";
import { View, Text, StyleSheet, FlatList, ActivityIndicator, TextInput, Button, Alert } from "react-native";
import api from "../services/api";

type Chamado = {
  id: number;
  assunto: string;
  descricao: string;
  categoria?: string;
  prioridade?: string;
  status: string;
  dataAbertura?: string;
};

export default function SupportScreen() {
  const [chamados, setChamados] = useState<Chamado[]>([]);
  const [loading, setLoading] = useState(true);
  const [assunto, setAssunto] = useState("");
  const [descricao, setDescricao] = useState("");
  const [saving, setSaving] = useState(false);

  const carregarChamados = () => {
    setLoading(true);
    api.get("/api/Chamados")
      .then(res => setChamados(res.data))
      .catch(err => console.error(err))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    carregarChamados();
  }, []);

  const abrirChamado = () => {
    if (!assunto || !descricao) {
      Alert.alert("Erro", "Preencha todos os campos");
      return;
    }
    setSaving(true);
    api.post("/api/Chamados", {
      assunto: assunto.trim(),
      descricao: descricao.trim(),
      categoria: "Acesso",
      prioridade: "Normal",
    })
      .then(() => {
        Alert.alert("Sucesso", "Chamado aberto com sucesso");
        setAssunto("");
        setDescricao("");
        carregarChamados();
      })
      .catch(err => {
        console.error(err);
        Alert.alert("Erro", "Não foi possível abrir o chamado");
      })
      .finally(() => setSaving(false));
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Meus Chamados</Text>
      
      {loading ? (
        <ActivityIndicator size="large" />
      ) : (
        <FlatList
          data={chamados}
          keyExtractor={(item) => item.id.toString()}
          renderItem={({ item }) => (
            <View style={styles.item}>
              <Text style={styles.itemTitle}>{item.assunto}</Text>
              <Text>Status: {item.status}</Text>
            </View>
          )}
          ListEmptyComponent={<Text>Nenhum chamado encontrado.</Text>}
        />
      )}

      <Text style={styles.title}>Novo Chamado</Text>
      <TextInput style={styles.input} placeholder="Assunto" value={assunto} onChangeText={setAssunto} />
      <TextInput style={styles.input} placeholder="Descrição" value={descricao} onChangeText={setDescricao} multiline />
      <Button title={saving ? "Salvando..." : "Abrir Chamado"} onPress={abrirChamado} disabled={saving} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20 },
  title: { fontSize: 20, fontWeight: "bold", marginVertical: 10 },
  item: { padding: 15, borderBottomWidth: 1, borderBottomColor: "#ccc", marginBottom: 10 },
  itemTitle: { fontWeight: "bold" },
  input: { borderWidth: 1, borderColor: "#ccc", padding: 10, marginBottom: 10, borderRadius: 5 }
});
