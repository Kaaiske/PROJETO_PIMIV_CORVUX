import React, { useState } from 'react';
import { View, Text, TextInput, Button, StyleSheet, Alert } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { RootStackParamList } from '../navigation/AppNavigator';
import api from '../services/api';

type Props = {
  navigation: NativeStackNavigationProp<RootStackParamList, 'Login'>;
};

export default function LoginScreen({ navigation }: Props) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (!username.trim() || !password) {
      Alert.alert('Erro', 'Informe email e senha.');
      return;
    }

    setLoading(true);
    try {
      const response = await api.post('/api/AuthApi/login', {
        email: username.trim(),
        senha: password,
      });
      const { token } = response.data;
      if (token) {
        await SecureStore.setItemAsync('userToken', token);
        navigation.replace('Home');
      } else {
        Alert.alert('Erro', 'Token não recebido');
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Falha ao conectar com a API.';
      Alert.alert('Erro de Login', message);
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>CorvusIAM Login</Text>
      <TextInput style={styles.input} placeholder="Usuário" value={username} onChangeText={setUsername} autoCapitalize="none" />
      <TextInput style={styles.input} placeholder="Senha" value={password} onChangeText={setPassword} secureTextEntry />
      <Button title={loading ? 'Carregando...' : 'Entrar'} onPress={handleLogin} disabled={loading} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 20 },
  title: { fontSize: 24, marginBottom: 20, textAlign: 'center' },
  input: { borderWidth: 1, borderColor: '#ccc', padding: 10, marginBottom: 10, borderRadius: 5 },
});

