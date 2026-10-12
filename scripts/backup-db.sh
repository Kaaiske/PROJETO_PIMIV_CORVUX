#!/bin/bash
# ==============================================================================
# CorvuxIAM - Script de Backup Automatizado do SQL Server
# Conformidade com a Política de Continuidade de Negócios (PIM IV)
# RPO: 24h (Full) / RTO: < 30min
# ==============================================================================

set -euo pipefail

BACKUP_DIR="$(pwd)/backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE_CONTAINER="/var/opt/mssql/backup/CorvusIAMDb_${TIMESTAMP}.bak"
BACKUP_FILE_HOST="${BACKUP_DIR}/CorvusIAMDb_${TIMESTAMP}.bak"
RETENTION_DAYS=30

mkdir -p "${BACKUP_DIR}"

echo "=================================================="
echo " [CorvuxIAM] Iniciando Backup do Banco de Dados"
echo " Data/Hora: $(date)"
echo " Arquivo: CorvusIAMDb_${TIMESTAMP}.bak"
echo "=================================================="

# 1. Executa o backup com compressão no SQL Server via docker exec
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SQL_SA_PASSWORD:-CorvuxAdmin@2026!}" -Q "
BACKUP DATABASE [CorvusIAMDb] 
TO DISK = N'${BACKUP_FILE_CONTAINER}' 
WITH NOFORMAT, NOINIT, 
NAME = N'CorvusIAMDb-Full Database Backup', 
SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10;
"

# 2. Valida a integridade do arquivo gerado
echo "[CorvuxIAM] Validando integridade física do backup gerado..."
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SQL_SA_PASSWORD:-CorvuxAdmin@2026!}" -Q "
RESTORE VERIFYONLY FROM DISK = N'${BACKUP_FILE_CONTAINER}';
"

echo "=================================================="
echo " [CorvuxIAM] Backup Concluído e Validado com Sucesso!"
echo " Localização Host: ${BACKUP_FILE_HOST}"
ls -lh "${BACKUP_FILE_HOST}" 2>/dev/null || true
echo "=================================================="

# 3. Política de retenção: remove backups com mais de 30 dias
echo "[CorvuxIAM] Aplicando política de retenção (${RETENTION_DAYS} dias)..."
find "${BACKUP_DIR}" -type f -name "CorvusIAMDb_*.bak" -mtime +${RETENTION_DAYS} -exec rm -f {} \; || true
echo "[CorvuxIAM] Limpeza de arquivos antigos concluída."

