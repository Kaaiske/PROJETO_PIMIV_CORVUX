#!/bin/bash
# ==============================================================================
# CorvuxIAM - Instalador Automático do Cron de Backup na VPS Linux
# ==============================================================================

set -euo pipefail

CRON_JOB="0 2 * * * cd /var/www/PROJETO_PIMIV_CORVUX && bash scripts/backup-db.sh >> /var/log/corvusiam-backup.log 2>&1"

echo "=================================================="
echo " [CorvuxIAM] Instalador da Rotina de Backup Automático"
echo " Horário Agendado: Diariamente às 02:00 BRT"
echo " Destino do Log: /var/log/corvusiam-backup.log"
echo "=================================================="

# Verifica se o cron já está cadastrado para evitar duplicidades
if crontab -l 2>/dev/null | grep -F "scripts/backup-db.sh" > /dev/null; then
    echo "[CorvuxIAM] O agendamento do backup já existe no crontab do sistema!"
else
    # Adiciona a linha preservando os jobs existentes
    (crontab -l 2>/dev/null || true; echo "${CRON_JOB}") | crontab -
    echo "[CorvuxIAM] Agendamento adicionado ao crontab com sucesso!"
fi

echo ""
echo "Rotinas ativas no Crontab:"
crontab -l | grep -F "backup-db.sh"
echo "=================================================="

