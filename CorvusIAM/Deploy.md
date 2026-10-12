# Guia de execução e implantação — CorvusIAM

## Desenvolvimento

```powershell
dotnet restore
dotnet ef database update
dotnet run
```

O login inicial é `admin@corvus.com`. A senha inicial deve ser definida nas variáveis de ambiente na primeira inicialização. Altere-a imediatamente após o primeiro login.

## Containers

1. Copie `.env.example` para `.env`.
2. Troque `SA_PASSWORD` e `JWT_KEY` por valores secretos e fortes.
3. Execute `docker compose up --build`.

O Compose aguarda o health check do SQL Server antes de iniciar a aplicação. A aplicação é exposta em `http://localhost:8080`; o endpoint de saúde é `/health`.

## Cloud

- Azure: Azure Container Registry + App Service for Containers + Azure SQL Database.
- AWS: ECR + ECS/Fargate + RDS for SQL Server.

Em ambos os provedores, configure strings de conexão e chaves JWT no serviço de secrets/variáveis de ambiente, nunca no repositório. O pipeline GitHub Actions restaura, compila, testa e cria a imagem Docker a cada push ou pull request para `main`.
