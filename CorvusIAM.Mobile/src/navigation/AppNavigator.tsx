import React, { useEffect, useState } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import * as SecureStore from 'expo-secure-store';

import LoginScreen from '../screens/LoginScreen';
import HomeScreen from '../screens/HomeScreen';
import AccountScreen from '../screens/AccountScreen';
import AccessScreen from '../screens/AccessScreen';
import PoliciesScreen from '../screens/PoliciesScreen';
import SupportScreen from '../screens/SupportScreen';

export type RootStackParamList = {
  Login: undefined;
  Home: undefined;
  Account: undefined;
  Access: undefined;
  Policies: undefined;
  Support: undefined;
};

const Stack = createNativeStackNavigator<RootStackParamList>();

export default function AppNavigator() {
  const [initialRoute, setInitialRoute] = useState<keyof RootStackParamList | null>(null);

  useEffect(() => {
    const checkToken = async () => {
      const token = await SecureStore.getItemAsync('userToken');
      setInitialRoute(token ? 'Home' : 'Login');
    };
    checkToken();
  }, []);

  if (!initialRoute) return null; // loading

  return (
    <NavigationContainer>
      <Stack.Navigator initialRouteName={initialRoute}>
        <Stack.Screen name="Login" component={LoginScreen} options={{ headerShown: false }} />
        <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Página inicial' }} />
        <Stack.Screen name="Account" component={AccountScreen} options={{ title: 'Minha conta' }} />
        <Stack.Screen name="Access" component={AccessScreen} options={{ title: 'Meus acessos/permissões' }} />
        <Stack.Screen name="Policies" component={PoliciesScreen} options={{ title: 'Políticas de segurança' }} />
        <Stack.Screen name="Support" component={SupportScreen} options={{ title: 'Abertura de chamado' }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
