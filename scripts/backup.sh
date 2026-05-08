#!/bin/bash
# ════════════════════════════════════════════════════════════
#  BerberApp / ayarlıyo — PostgreSQL Yedek Alma Scripti
#  Kullanım: ./scripts/backup.sh
#  Cron (günlük saat 03:00): 0 3 * * * /root/BerberApp/scripts/backup.sh >> /root/BerberApp/logs/backup.log 2>&1
# ════════════════════════════════════════════════════════════
set -euo pipefail

CONTAINER_NAME="berberapp-db"
POSTGRES_USER="postgres"
POSTGRES_DB="BerberAppDb"
BACKUP_DIR="$(cd "$(dirname "$0")/.." && pwd)/backups"
RETAIN_DAYS="${RETAIN_DAYS:-7}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/berberapp_$TIMESTAMP.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] 🔄 Yedek alınıyor → $BACKUP_FILE"

# pg_dump'ı container içinde çalıştır, çıktıyı host'ta gzip ile sıkıştır
docker exec "$CONTAINER_NAME" \
    pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists \
    | gzip > "$BACKUP_FILE"

SIZE=$(du -sh "$BACKUP_FILE" | cut -f1)
echo "[$(date '+%Y-%m-%d %H:%M:%S')] ✅ Yedek tamamlandı: $BACKUP_FILE ($SIZE)"

# Eski yedekleri temizle (RETAIN_DAYS günden eski)
echo "[$(date '+%Y-%m-%d %H:%M:%S')] 🧹 $RETAIN_DAYS günden eski yedekler siliniyor..."
find "$BACKUP_DIR" -name "berberapp_*.sql.gz" -mtime "+$RETAIN_DAYS" -print -delete | \
    sed "s/^/[$(date '+%Y-%m-%d %H:%M:%S')] Silindi: /"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] 📋 Mevcut yedekler:"
ls -lh "$BACKUP_DIR"/berberapp_*.sql.gz 2>/dev/null || echo "  (yedek bulunamadı)"
