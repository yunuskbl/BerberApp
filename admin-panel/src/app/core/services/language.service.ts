import { Injectable, signal } from '@angular/core';

export type Lang = 'tr' | 'en' | 'ru';

const T: Record<Lang, Record<string, string>> = {

  /* ══════════════════════════════════════ TÜRKÇE ══════════════════════════════════════ */
  tr: {
    // Nav
    'nav.home':         'Ana Sayfa',
    'nav.appointments': 'Randevular',
    'nav.staff':        'Personel',
    'nav.services':     'Hizmetler',
    'nav.customers':    'Müşteriler',
    'nav.settings':     'Ayarlar',
    'nav.reports':      'Raporlar',

    // Page titles (header)
    'page.dashboard':    'Dashboard',
    'page.appointments': 'Randevular',
    'page.staff':        'Personel',
    'page.services':     'Hizmetler',
    'page.customers':    'Müşteriler',
    'page.settings':     'Ayarlar',
    'page.reports':      'Raporlar',

    // Header
    'header.system': 'Sistem Aktif',

    // Dashboard
    'dashboard.subtitle':       'Hoş geldiniz! İşte bugünün özeti.',
    'dashboard.newAppointment': '+ Yeni Randevu',
    'dashboard.todayAppts':     'Bugünkü Randevular',
    'dashboard.viewAll':        'Tümünü Gör →',
    'dashboard.quickActions':   'Hızlı İşlemler',
    'dashboard.noAppts':        'Bugün randevu bulunmuyor.',
    'dashboard.q1':             'Yeni Randevu',
    'dashboard.q1sub':          'Randevu oluştur',
    'dashboard.q2':             'Müşteri Ekle',
    'dashboard.q2sub':          'Yeni müşteri kaydı',
    'dashboard.q3':             'Personel Ekle',
    'dashboard.q3sub':          'Yeni personel kaydı',
    'dashboard.q4':             'Hizmet Ekle',
    'dashboard.q4sub':          'Yeni hizmet tanımla',
    'stat.todayAppts':          'Bugünkü Randevular',
    'stat.totalCustomers':      'Toplam Müşteri',
    'stat.totalStaff':          'Toplam Personel',
    'stat.totalServices':       'Toplam Hizmet',
    'stat.todayRevenue':        'Bugünkü Gelir',

    // Appointment statuses
    'status.pending':   'Bekliyor',
    'status.confirmed': 'Onaylandı',
    'status.completed': 'Tamamlandı',
    'status.cancelled': 'İptal',
    'status.noShow':    'Gelmedi',

    // Settings — genel
    'settings.title':    'Ayarlar',
    'settings.subtitle': 'Salon ve hesap ayarlarınızı yönetin',
    'settings.logout':   '🚪 Çıkış Yap',

    // Settings — Salon Bilgileri
    'settings.salon.card':         'Salon Bilgileri',
    'settings.salon.cardSub':      'Müşterilerinizin göreceği salon bilgileri',
    'settings.salon.logo':         'Salon Logosu',
    'settings.salon.logoHint':     'JPG, PNG veya WebP · Maks 5MB',
    'settings.salon.logoUploading':'Yükleniyor...',
    'settings.salon.logoChange':   'Logoyu Değiştir',
    'settings.salon.logoSelect':   'Logo Seç',
    'settings.salon.name':         'Salon Adı *',
    'settings.salon.namePh':       'Salon adınız',
    'settings.salon.phone':        'Telefon',
    'settings.salon.phonePh':      '05XX XXX XX XX',
    'settings.salon.phoneErr':     'Geçerli bir telefon numarası girin (05XX XXX XX XX)',
    'settings.salon.whatsapp':     'WhatsApp Bildirim Numarası',
    'settings.salon.whatsappPh':   '05XX XXX XX XX',
    'settings.salon.whatsappHint': 'Yeni randevu taleplerinde bu numaraya WhatsApp bildirimi gönderilir.',
    'settings.salon.address':      'Adres',
    'settings.salon.addressPh':    'Salon adresiniz',
    'settings.salon.themeColor':   'Tema Rengi',
    'settings.salon.themeHint':    'Müşteri randevu sayfanızın tema rengini belirleyin',
    'settings.salon.bookingLink':  'Müşteri Booking Linki',
    'settings.salon.save':         'Kaydet',
    'settings.salon.saving':       'Kaydediliyor...',
    'settings.salon.saved':        'Salon bilgileri güncellendi!',
    'settings.salon.custom':       'Özel',

    // Settings — Fotoğraflar
    'settings.photos.card':    'Salon Fotoğrafları',
    'settings.photos.cardSub': 'Müşterilerin booking sayfasında göreceği fotoğraflar (maks. 6)',
    'settings.photos.add':     'Fotoğraf Ekle',

    // Settings — Profil
    'settings.profile.card':    'Profil Bilgileri',
    'settings.profile.cardSub': 'Hesap bilgileriniz',

    // Settings — Şifre
    'settings.password.card':        'Şifre Değiştir',
    'settings.password.cardSub':     'Hesap güvenliğiniz için şifrenizi güncelleyin',
    'settings.password.current':     'Mevcut Şifre',
    'settings.password.new':         'Yeni Şifre',
    'settings.password.confirm':     'Yeni Şifre Tekrar',
    'settings.password.change':      'Şifreyi Değiştir',
    'settings.password.changing':    'Değiştiriliyor...',
    'settings.password.changed':     'Şifre başarıyla değiştirildi!',
    'settings.password.rule.minLen': 'En az 8 karakter',
    'settings.password.rule.upper':  'En az 1 büyük harf (A-Z)',
    'settings.password.rule.number': 'En az 1 rakam (0-9)',
    'settings.password.noMatch':     'Şifreler eşleşmiyor.',
    'settings.password.tooShort':    'Şifre en az 8 karakter olmalı.',

    // Settings — Booking linki
    'settings.booking.card':    'Müşteri Randevu Linki',
    'settings.booking.cardSub': 'Bu linki müşterilerinizle paylaşın',
    'settings.booking.preview': 'Önizle',

    // Common
    'common.loading': 'Yükleniyor...',
    'common.copy':    'Kopyala',
    'common.copied':  'Kopyalandı',
  },

  /* ══════════════════════════════════════ ENGLISH ══════════════════════════════════════ */
  en: {
    'nav.home':         'Home',
    'nav.appointments': 'Appointments',
    'nav.staff':        'Staff',
    'nav.services':     'Services',
    'nav.customers':    'Customers',
    'nav.settings':     'Settings',
    'nav.reports':      'Reports',

    'page.dashboard':    'Dashboard',
    'page.appointments': 'Appointments',
    'page.staff':        'Staff',
    'page.services':     'Services',
    'page.customers':    'Customers',
    'page.settings':     'Settings',
    'page.reports':      'Reports',

    'header.system': 'System Active',

    'dashboard.subtitle':       "Welcome! Here's today's summary.",
    'dashboard.newAppointment': '+ New Appointment',
    'dashboard.todayAppts':     "Today's Appointments",
    'dashboard.viewAll':        'View All →',
    'dashboard.quickActions':   'Quick Actions',
    'dashboard.noAppts':        'No appointments today.',
    'dashboard.q1':             'New Appointment',
    'dashboard.q1sub':          'Create appointment',
    'dashboard.q2':             'Add Customer',
    'dashboard.q2sub':          'New customer record',
    'dashboard.q3':             'Add Staff',
    'dashboard.q3sub':          'New staff record',
    'dashboard.q4':             'Add Service',
    'dashboard.q4sub':          'Define new service',
    'stat.todayAppts':          "Today's Appointments",
    'stat.totalCustomers':      'Total Customers',
    'stat.totalStaff':          'Total Staff',
    'stat.totalServices':       'Total Services',
    'stat.todayRevenue':        "Today's Revenue",

    'status.pending':   'Pending',
    'status.confirmed': 'Confirmed',
    'status.completed': 'Completed',
    'status.cancelled': 'Cancelled',
    'status.noShow':    'No Show',

    'settings.title':    'Settings',
    'settings.subtitle': 'Manage your salon and account settings',
    'settings.logout':   '🚪 Sign Out',

    'settings.salon.card':         'Salon Information',
    'settings.salon.cardSub':      'Information visible to your customers',
    'settings.salon.logo':         'Salon Logo',
    'settings.salon.logoHint':     'JPG, PNG or WebP · Max 5MB',
    'settings.salon.logoUploading':'Uploading...',
    'settings.salon.logoChange':   'Change Logo',
    'settings.salon.logoSelect':   'Select Logo',
    'settings.salon.name':         'Salon Name *',
    'settings.salon.namePh':       'Your salon name',
    'settings.salon.phone':        'Phone',
    'settings.salon.phonePh':      'Phone number',
    'settings.salon.phoneErr':     'Please enter a valid phone number',
    'settings.salon.whatsapp':     'WhatsApp Notification Number',
    'settings.salon.whatsappPh':   'WhatsApp number',
    'settings.salon.whatsappHint': 'You will receive WhatsApp notifications for new appointment requests on this number.',
    'settings.salon.address':      'Address',
    'settings.salon.addressPh':    'Your salon address',
    'settings.salon.themeColor':   'Theme Color',
    'settings.salon.themeHint':    'Set the theme color for your customer booking page',
    'settings.salon.bookingLink':  'Customer Booking Link',
    'settings.salon.save':         'Save',
    'settings.salon.saving':       'Saving...',
    'settings.salon.saved':        'Salon information updated!',
    'settings.salon.custom':       'Custom',

    'settings.photos.card':    'Salon Photos',
    'settings.photos.cardSub': 'Photos visible on your booking page (max 6)',
    'settings.photos.add':     'Add Photo',

    'settings.profile.card':    'Profile Information',
    'settings.profile.cardSub': 'Your account details',

    'settings.password.card':        'Change Password',
    'settings.password.cardSub':     'Update your password for account security',
    'settings.password.current':     'Current Password',
    'settings.password.new':         'New Password',
    'settings.password.confirm':     'Confirm New Password',
    'settings.password.change':      'Change Password',
    'settings.password.changing':    'Changing...',
    'settings.password.changed':     'Password changed successfully!',
    'settings.password.rule.minLen': 'At least 8 characters',
    'settings.password.rule.upper':  'At least 1 uppercase letter (A-Z)',
    'settings.password.rule.number': 'At least 1 number (0-9)',
    'settings.password.noMatch':     'Passwords do not match.',
    'settings.password.tooShort':    'Password must be at least 8 characters.',

    'settings.booking.card':    'Customer Booking Link',
    'settings.booking.cardSub': 'Share this link with your customers',
    'settings.booking.preview': 'Preview',

    'common.loading': 'Loading...',
    'common.copy':    'Copy',
    'common.copied':  'Copied',
  },

  /* ══════════════════════════════════════ РУССКИЙ ══════════════════════════════════════ */
  ru: {
    'nav.home':         'Главная',
    'nav.appointments': 'Записи',
    'nav.staff':        'Персонал',
    'nav.services':     'Услуги',
    'nav.customers':    'Клиенты',
    'nav.settings':     'Настройки',
    'nav.reports':      'Отчёты',

    'page.dashboard':    'Панель управления',
    'page.appointments': 'Записи',
    'page.staff':        'Персонал',
    'page.services':     'Услуги',
    'page.customers':    'Клиенты',
    'page.settings':     'Настройки',
    'page.reports':      'Отчёты',

    'header.system': 'Система активна',

    'dashboard.subtitle':       'Добро пожаловать! Сводка за сегодня.',
    'dashboard.newAppointment': '+ Новая запись',
    'dashboard.todayAppts':     'Записи на сегодня',
    'dashboard.viewAll':        'Смотреть все →',
    'dashboard.quickActions':   'Быстрые действия',
    'dashboard.noAppts':        'На сегодня записей нет.',
    'dashboard.q1':             'Новая запись',
    'dashboard.q1sub':          'Создать запись',
    'dashboard.q2':             'Добавить клиента',
    'dashboard.q2sub':          'Новая запись клиента',
    'dashboard.q3':             'Добавить сотрудника',
    'dashboard.q3sub':          'Новая запись сотрудника',
    'dashboard.q4':             'Добавить услугу',
    'dashboard.q4sub':          'Создать новую услугу',
    'stat.todayAppts':          'Записи на сегодня',
    'stat.totalCustomers':      'Всего клиентов',
    'stat.totalStaff':          'Всего сотрудников',
    'stat.totalServices':       'Всего услуг',
    'stat.todayRevenue':        'Доход за сегодня',

    'status.pending':   'Ожидает',
    'status.confirmed': 'Подтверждено',
    'status.completed': 'Завершено',
    'status.cancelled': 'Отменено',
    'status.noShow':    'Не пришёл',

    'settings.title':    'Настройки',
    'settings.subtitle': 'Управляйте настройками салона и аккаунта',
    'settings.logout':   '🚪 Выйти',

    'settings.salon.card':         'Информация о салоне',
    'settings.salon.cardSub':      'Информация, видимая вашим клиентам',
    'settings.salon.logo':         'Логотип салона',
    'settings.salon.logoHint':     'JPG, PNG или WebP · Макс 5MB',
    'settings.salon.logoUploading':'Загрузка...',
    'settings.salon.logoChange':   'Изменить логотип',
    'settings.salon.logoSelect':   'Выбрать логотип',
    'settings.salon.name':         'Название салона *',
    'settings.salon.namePh':       'Название вашего салона',
    'settings.salon.phone':        'Телефон',
    'settings.salon.phonePh':      'Номер телефона',
    'settings.salon.phoneErr':     'Введите корректный номер телефона',
    'settings.salon.whatsapp':     'Номер WhatsApp для уведомлений',
    'settings.salon.whatsappPh':   'Номер WhatsApp',
    'settings.salon.whatsappHint': 'На этот номер будут приходить уведомления о новых запросах на запись.',
    'settings.salon.address':      'Адрес',
    'settings.salon.addressPh':    'Адрес вашего салона',
    'settings.salon.themeColor':   'Цвет темы',
    'settings.salon.themeHint':    'Установите цвет темы для страницы записи клиентов',
    'settings.salon.bookingLink':  'Ссылка для записи клиентов',
    'settings.salon.save':         'Сохранить',
    'settings.salon.saving':       'Сохранение...',
    'settings.salon.saved':        'Информация о салоне обновлена!',
    'settings.salon.custom':       'Свой',

    'settings.photos.card':    'Фотографии салона',
    'settings.photos.cardSub': 'Фотографии на странице записи (макс. 6)',
    'settings.photos.add':     'Добавить фото',

    'settings.profile.card':    'Данные профиля',
    'settings.profile.cardSub': 'Информация о вашем аккаунте',

    'settings.password.card':        'Изменить пароль',
    'settings.password.cardSub':     'Обновите пароль для безопасности аккаунта',
    'settings.password.current':     'Текущий пароль',
    'settings.password.new':         'Новый пароль',
    'settings.password.confirm':     'Подтвердите новый пароль',
    'settings.password.change':      'Изменить пароль',
    'settings.password.changing':    'Изменение...',
    'settings.password.changed':     'Пароль успешно изменён!',
    'settings.password.rule.minLen': 'Не менее 8 символов',
    'settings.password.rule.upper':  'Не менее 1 заглавной буквы (A-Z)',
    'settings.password.rule.number': 'Не менее 1 цифры (0-9)',
    'settings.password.noMatch':     'Пароли не совпадают.',
    'settings.password.tooShort':    'Пароль должен содержать не менее 8 символов.',

    'settings.booking.card':    'Ссылка для записи клиентов',
    'settings.booking.cardSub': 'Поделитесь этой ссылкой с клиентами',
    'settings.booking.preview': 'Просмотр',

    'common.loading': 'Загрузка...',
    'common.copy':    'Копировать',
    'common.copied':  'Скопировано',
  },
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly lang = signal<Lang>((localStorage.getItem('lang') as Lang) || 'tr');

  setLang(l: Lang): void {
    this.lang.set(l);
    localStorage.setItem('lang', l);
  }

  t(key: string): string {
    return T[this.lang()][key] ?? key;
  }

  get dateLocale(): string {
    return { tr: 'tr-TR', en: 'en-US', ru: 'ru-RU' }[this.lang()];
  }

  get langLabel(): string {
    return { tr: 'TR', en: 'EN', ru: 'RU' }[this.lang()];
  }
}
