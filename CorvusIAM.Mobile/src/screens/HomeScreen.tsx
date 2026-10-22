import React from 'react';
import { View, Button, StyleSheet, Alert } from 'react-native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import * as SecureStore from 'expo-secure-store';
import { RootStackParamList } from '../navigation/AppNavigator';

type Props = {
  navigation: NativeStackNavigationProp<RootStackParamList, 'Home'>;
};

export default function HomeScreen({ navigation }: Props) {
  const handleLogout = async () => {
    await SecureStore.deleteItemAsync('userToken');
    await SecureStore.deleteItemAsync('refreshToken');
    await SecureStore.deleteItemAsync('userEmail');
    navigation.replace('Login');
  };

  return (
    <View style={styles.container}>
      <View style={styles.buttonContainer}><Button title="Minha conta" onPress={() => navigation.navigate('Account')} /></View>
      <View style={styles.buttonContainer}><Button title="Meus acessos/permissões" onPress={() => navigation.navigate('Access')} /></View>
      <View style={styles.buttonContainer}><Button title="Políticas de segurança" onPress={() => navigation.navigate('Policies')} /></View>
      <View style={styles.buttonContainer}><Button title="Suporte (Chamados)" onPress={() => navigation.navigate('Support')} /></View>
      <View style={[styles.buttonContainer, { marginTop: 40 }]}><Button title="Sair" color="red" onPress={handleLogout} /></View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 20, justifyContent: 'center' },
  buttonContainer: { marginBottom: 15 },
});
