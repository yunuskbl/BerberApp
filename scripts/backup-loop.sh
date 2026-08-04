#!/bin/bash
# ════════════════════════════════════════════════════════════
#  BerberApp / ayarlıyo — Otomatik yedekleme döngüsü (container içi)
#
#  docker-compose.yml'deki db-backup servisi bunu entrypoint olarak çalıştırır.
#  postgres:16 imajı Debian tabanlı; burada apk/crond yok, o yüzden zamanlama
#  cron yerine düz bir bekleme döngüsüyle yapılıyor.
#
#  Elle yedek almak için scripts/backup.sh kullanılır (o host üzerinde,
#  docker exec ile çalışır — bu dosya ise container içinden ağ üzerinden).
# ════════════════════════════════════════════════════════════
set -uo pipefail

BACKUP_DIR="${BACKUP_DIR:-/backups}"
DB_HOST="${DB_HOST:-db}"
DB_USER="${DB_USER:-postgres}"
DB_NAME="${DB_NAME:-BerberAppDb}"
RETAIN_DAYS="${RETAIN_DAYS:-7}"
BACKUP_HOUR="${BACKUP_HOUR:-03}"
MIN_FREE_MB="${MIN_FREE_MB:-1024}"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S %Z')] $*"; }

# Yedek alınabilir mi? Disk dolu bir sunucuda yarım bir dump almak, hiç
# almamaktan daha tehlikeli: dosya var görünür ama geri yüklenemez.
has_room() {
    local free_mb
    free_mb=$(df -Pm "$BACKUP_DIR" | awk 'NR==2 {print $4}')
    if [ -z "$free_mb" ] || [ "$free_mb" -lt "$MIN_FREE_MB" ]; then
        log "❌ Yedek alınmadı: yalnızca ${free_mb:-0}MB boş alan var, en az ${MIN_FREE_MB}MB gerekli."
        return 1
    fi
    return 0
}

run_backup() {
    mkdir -p "$BACKUP_DIR" || return 1
    has_room || return 1

    local ts final partial size
    ts=$(date +"%Y%m%d_%H%M%S")
    final="$BACKUP_DIR/berberapp_$ts.sql.gz"
    # Önce .partial'a yazılır, ancak eksiksiz biterse asıl adına taşınır.
    # Böylece yarıda kesilen bir dump geçerli yedek gibi görünmez.
    partial="$final.partial"

    log "🔄 Yedek alınıyor → $final"

    if ! ( set -o pipefail
           pg_dump -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" --clean --if-exists | gzip > "$partial" ); then
        log "❌ pg_dump başarısız oldu, eksik dosya siliniyor."
        rm -f "$partial"
        return 1
    fi

    mv "$partial" "$final"
    size=$(du -h "$final" | cut -f1)
    log "✅ Yedek tamamlandı: $final ($size)"

    log "🧹 ${RETAIN_DAYS} günden eski yedekler siliniyor..."
    find "$BACKUP_DIR" -name 'berberapp_*.sql.gz' -mtime "+$RETAIN_DAYS" -print -delete

    # Yarım kalmış eski denemeler de birikmesin
    find "$BACKUP_DIR" -name '*.sql.gz.partial' -mmin +120 -print -delete

    return 0
}

# Son 24 saatte yedek var mı? Yoksa açılışta hemen bir tane al —
# aksi halde servis 03:00'a kadar hiç yedeği olmayan bir sistemi bekler.
needs_initial_backup() {
    [ -z "$(find "$BACKUP_DIR" -name 'berberapp_*.sql.gz' -mtime -1 2>/dev/null | head -n 1)" ]
}

log "Yedekleme servisi başladı — hedef saat ${BACKUP_HOUR}:00, saklama ${RETAIN_DAYS} gün."

if needs_initial_backup; then
    log "Son 24 saatte yedek bulunamadı, açılış yedeği alınıyor."
    run_backup || log "Açılış yedeği alınamadı, zamanlanmış turda tekrar denenecek."
fi

while true; do
    now=$(date +%s)
    target=$(date -d "today ${BACKUP_HOUR}:00" +%s)
    [ "$target" -le "$now" ] && target=$(date -d "tomorrow ${BACKUP_HOUR}:00" +%s)

    log "⏳ Sıradaki yedek: $(date -d "@$target" '+%Y-%m-%d %H:%M:%S %Z')"
    sleep $((target - now))

    run_backup || log "Yedek alınamadı, yarın tekrar denenecek."
done
