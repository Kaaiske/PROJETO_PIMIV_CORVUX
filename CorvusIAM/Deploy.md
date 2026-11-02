# Guia de Execução e Implantação — CorvuxIAM (`CorvusIAM`)

> **Convenção de Nomenclatura:** `CorvuxIAM` é o nome oficial da plataforma e do produto corporativo (`https://corvuxiam.tech`), enquanto `CorvusIAM` é o identificador técnico mantido na solução .NET (`CorvusIAM.sln`), nos projetos (`CorvusIAM/CorvusIAM.csproj`, `CorvusIAM.Mobile`, `CorvusIAM.Tests`), nos namespaces C# e no banco relacional (`CorvusIAMDb`).

## 1. Ambiente de Desenvolvimento Local (.NET 8 CLI)

A partir da raiz do repositório:

```powershell
dotnet restore CorvusIAM.sln
dotnet build CorvusIAM.sln --configuration Release
dotnet test CorvusIAM.sln --configuration Release --collect:"XPlat Code Coverage"
dotnet run --project CorvusIAM/CorvusIAM.csproj
```

Em desenvolvimento local, a aplicação utiliza SQLite por padrão (`http://localhost:5052`) e habilita a especificação interativa OpenAPI em `http://localhost:5052/swagger`. A conta administrativa inicial (`admin@corvus.com`) recebe a senha definida na variável de ambiente `ADMIN_INITIAL_PASSWORD`.

## 2. Aplicativo Mobile (PWA e React Native / Expo)

- **PWA Offline-First:** acessível diretamente em `/Mobile` (ou `/Mobile/Login`) com manifesto em `/mobile/manifest.webmanifest`, Service Worker e fila local `IndexedDB` (`CorvusOfflineDB`).
- **React Native (Expo 51):** localizado no diretório `CorvusIAM.Mobile/`:

```powershell
cd CorvusIAM.Mobile
npm install
npx expo start
```

## 3. Implantação Padronizada com Docker Compose (Produção / VPS)

1. Copie `.env.example` para `.env` na raiz do repositório.
2. Defina segredos exclusivos para `SQL_SA_PASSWORD`, `JWT_KEY`, `ADMIN_INITIAL_PASSWORD`, `API_GATEWAY_TOKEN`, `TURNSTILE_SITEKEY` e `TURNSTILE_SECRETKEY`.
3. Construa e inicie os contêineres:

```bash
docker compose up -d --build
docker compose ps
```

O serviço `sqlserver` opera isolado na rede interna `corvus-network` sem exposição da porta `1433` no host externo, enquanto `corvusiam.web` faz bind por padrão em `127.0.0.1:8080:8080` atrás de proxy reverso TLS/Cloudflare. O monitoramento de integridade responde em `/health` e as métricas Prometheus em `/metrics` (restritas à política `RequireAdmin`).
