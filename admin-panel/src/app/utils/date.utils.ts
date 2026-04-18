export function toLocalTime(utcDateStr: string): Date {
  return new Date(utcDateStr);
  // JavaScript new Date() otomatik olarak yerel saate çevirir
}

export function formatLocalTime(utcDateStr: string): string {
  return new Date(utcDateStr).toLocaleTimeString('tr-TR', {
    hour:   '2-digit',
    minute: '2-digit'
  });
}

export function formatLocalDate(utcDateStr: string): string {
  return new Date(utcDateStr).toLocaleDateString('tr-TR', {
    day:     'numeric',
    month:   'long',
    year:    'numeric',
    weekday: 'long'
  });
}

export function formatLocalDateTime(utcDateStr: string): string {
  return new Date(utcDateStr).toLocaleString('tr-TR', {
    day:     'numeric',
    month:   'long',
    year:    'numeric',
    hour:    '2-digit',
    minute:  '2-digit'
  });
}

export function toLocalDateString(utcDateStr: string): string {
  // YYYY-MM-DD formatında yerel tarihi döner — filtre için
  const d = new Date(utcDateStr);
  const year  = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day   = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}