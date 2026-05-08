#!/bin/bash
# ════════════════════════════════════════════════════════════
#  BerberApp / ayarlıyo — PostgreSQL Geri Yükleme Scripti
#  Kullanım: ./scripts/restore.sh backups/berberapp_20260509_030000.sql.gz
#  ⚠️  UYARI: Mevcut veritabanı SILINIR ve yedekle değiştirilir!
# ════════════════════════════════════════════════════════════
set -euo pipefail

BACKUP_FILE="${1:-}"
CONTAINER_NAME="berberapp-db"
POSTGRES_USER="postgres"
POSTGRES_DB="BerberAppDb"

if [ -z "$BACKUP_FILE" ]; then
    echo "Kullanım: $0 <yedek_dosyası.sql.gz>"
    echo ""
    echo "Mevcut yedekler:"
    ls -lth "$(dirname "$0")/../backups"/berberapp_*.sql.gz 2>/dev/null || echo "  (yedek bulunamadı)"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Hata: Yedek dosyası bulunamadı: $BACKUP_FILE"
    exit 1
fi

echo ""
echo "════════════════════════════════════════"
echo "  ⚠️  UYARI — GERİ YÜKLEME"
echo "════════════════════════════════════════"
echo "  Veritabanı : $POSTGRES_DB"
echo "  Container  : $CONTAINER_NAME"
echo "  Yedek      : $BACKUP_FILE"
echo ""
echo "  Bu işlem mevcut veritabanını SİLECEK ve"
echo "  yedekle değiştirecek. GERİ ALINAMAZ!"
echo ""
echo "  Devam etmek için 'EVET' yazın:"
read -r CONFIRM

if [ "$CONFIRM" != "EVET" ]; then
    echo "İşlem iptal edildi."
    exit 0
fi

echo ""
echo "[$(date '+%Y-%m-%d %H:%M:%S')] 🔄 Geri yükleme başlıyor: $BACKUP_FILE"

gunzip -c "$BACKUP_FILE" | docker exec -i "$CONTAINER_NAME" \
    psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" --quiet

echo "[$(date '+%Y-%m-%d %H:%M:%S')] ✅ Geri yükleme tamamlandı."
echo ""
echo "  API container'ı yeniden başlatmayı unutma:"
echo "  docker restart berberapp-api"
