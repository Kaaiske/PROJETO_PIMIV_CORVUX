#!/bin/bash
# ==============================================================================
# CorvuxIAM - Procedimento de Restauração de Desastre (Disaster Recovery)
# RTO Alvo: < 30 minutos
# ==============================================================================

set -euo pipefail

if [ -z "${1:-}" ]; then
    echo "Uso: $0 <caminho_do_arquivo_bak>"
    echo "Exemplo: $0 backups/CorvusIAMDb_20261012_120000.bak"
    echo ""
    echo "Backups disponíveis em ./backups:"
    ls -lh ./backups/*.bak 2>/dev/null || echo "Nenhum arquivo .bak encontrado."
    exit 1
fi

HOST_BAK_FILE="$1"
BAK_FILENAME=$(basename "${HOST_BAK_FILE}")
CONTAINER_BAK_FILE="/var/opt/mssql/backup/${BAK_FILENAME}"

if [ ! -f "${HOST_BAK_FILE}" ]; then
    echo "ERRO: Arquivo ${HOST_BAK_FILE} não encontrado!"
    exit 1
fi

echo "=================================================="
echo " [CorvuxIAM] ATENÇÃO: INICIANDO RESTAURAÇÃO DE BANCO"
echo " Arquivo selecionado: ${BAK_FILENAME}"
echo " Banco de Destino: CorvusIAMDb"
echo "=================================================="

read -p "Deseja realmente restaurar e sobrescrever o banco atual? (s/N): " CONFIRM
if [[ "${CONFIRM}" != "s" && "${CONFIRM}" != "S" ]]; then
    echo "Operação cancelada pelo operador."
    exit 0
fi

# 1. Coloca o banco em SINGLE_USER para encerrar conexões ativas
echo "[CorvuxIAM] Finalizando conexões ativas no banco de dados..."
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SQL_SA_PASSWORD:-CorvuxAdmin@2026!}" -Q "
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'CorvusIAMDb')
BEGIN
    ALTER DATABASE [CorvusIAMDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
"

# 2. Executa a restauração completa com substituição (REPLACE)
echo "[CorvuxIAM] Restaurando banco de dados a partir do arquivo de backup..."
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SQL_SA_PASSWORD:-CorvuxAdmin@2026!}" -Q "
RESTORE DATABASE [CorvusIAMDb] 
FROM DISK = N'${CONTAINER_BAK_FILE}' 
WITH REPLACE, STATS = 10;
"

# 3. Retorna o banco para MULTI_USER
echo "[CorvuxIAM] Reativando modo multiusuário..."
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SQL_SA_PASSWORD:-CorvuxAdmin@2026!}" -Q "
ALTER DATABASE [CorvusIAMDb] SET MULTI_USER;
"

echo "=================================================="
echo " [CorvuxIAM] Restauração concluída com sucesso!"
echo " A integridade do banco foi restabelecida."
echo "=================================================="

