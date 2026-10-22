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
  const [mfaSessionToken, setMfaSessionToken] = useState<string | null>(null);
  const [totpCode, setTotpCode] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (mfaSessionToken && !/^\d{6}$/.test(totpCode)) {
      Alert.alert('Erro', 'Informe o código MFA de 6 dígitos.');
      return;
    }

    if (!mfaSessionToken && (!username.trim() || !password)) {
      Alert.alert('Erro', 'Informe email e senha.');
      return;
    }

    setLoading(true);
    try {
      const response = mfaSessionToken
        ? await api.post('/api/Auth/mfa-verify', { mfaSessionToken, codigoTotp: totpCode })
        : await api.post('/api/Auth/login', { username: username.trim(), password });
      const { token, refreshToken, mfaRequerido, mfaSessionToken: nextMfaToken } = response.data;
      if (mfaRequerido && nextMfaToken) {
        setMfaSessionToken(nextMfaToken);
        setPassword('');
        Alert.alert('MFA necessário', 'Informe o código do seu autenticador.');
        return;
      }
      if (token) {
        await SecureStore.setItemAsync('userToken', token);
        if (refreshToken) await SecureStore.setItemAsync('refreshToken', refreshToken);
        await SecureStore.setItemAsync('userEmail', username.trim());
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
      {mfaSessionToken && <TextInput style={styles.input} placeholder="Código MFA" value={totpCode} onChangeText={setTotpCode} keyboardType="number-pad" maxLength={6} />}
      <Button title={loading ? 'Carregando...' : mfaSessionToken ? 'Validar MFA' : 'Entrar'} onPress={handleLogin} disabled={loading} />
      {mfaSessionToken && <Button title="Voltar" onPress={() => { setMfaSessionToken(null); setTotpCode(''); }} disabled={loading} />}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 20 },
  title: { fontSize: 24, marginBottom: 20, textAlign: 'center' },
  input: { borderWidth: 1, borderColor: '#ccc', padding: 10, marginBottom: 10, borderRadius: 5 },
});

