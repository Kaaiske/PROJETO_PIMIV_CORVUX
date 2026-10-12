#!/bin/bash
# ==============================================================================
# CorvuxIAM - Sincronização de Backups com Nuvem (Amazon S3 / Object Storage)
# Camada de Armazenamento Off-site com Criptografia em Trânsito e Repouso
# Regra de Segurança 3-2-1
# ==============================================================================

set -euo pipefail

BACKUP_DIR="$(pwd)/backups"
S3_BUCKET="${S3_BUCKET_NAME:-corvusiam-backups-corporate}"
S3_PREFIX="${S3_BACKUP_PREFIX:-sqlserver}"

echo "=================================================="
echo " [CorvuxIAM] Sincronização Off-site com Amazon S3"
echo " Bucket Destino: s3://${S3_BUCKET}/${S3_PREFIX}/"
echo "=================================================="

# Verifica se a ferramenta AWS CLI está disponível
if ! command -v aws &> /dev/null; then
    echo "[AVISO] AWS CLI não encontrado no ambiente do servidor."
    echo "[AVISO] Para habilitar replicação em nuvem S3, instale: apt install awscli"
    echo "[AVISO] e configure as chaves de acesso com privilégio mínimo: aws configure"
    exit 0
fi

# Valida se as variáveis de ambiente ou credenciais AWS existem
if [ -z "${AWS_ACCESS_KEY_ID:-}" ] && [ ! -f ~/.aws/credentials ]; then
    echo "[AVISO] Credenciais AWS não configuradas. A sincronização S3 foi ignorada com segurança."
    exit 0
fi

# Sincroniza arquivos .bak com criptografia do lado do servidor (SSE-S3 ou SSE-KMS)
echo "[CorvuxIAM] Enviando backups para o bucket Amazon S3 com criptografia SSE-S3..."
aws s3 sync "${BACKUP_DIR}" "s3://${S3_BUCKET}/${S3_PREFIX}/" \
    --exclude "*" \
    --include "*.bak" \
    --sse AES256 \
    --storage-class STANDARD_IA

echo "[CorvuxIAM] Sincronização com Amazon S3 concluída com sucesso!"
