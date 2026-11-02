/**
 * Plan adları, sıralaması ve hangi sayfanın hangi planı istediği — tek kaynak.
 *
 * Bu dosyadan önce aynı bilgi üç yerde ayrı ayrı yazılıydı: rota tanımlarında
 * (`data.requiredPlan`), kenar çubuğunun kendi `menuItems` dizisinde ve
 * raporlar bileşenindeki `isPremium` getter'ında. Kaçınılmaz olan oldu ve
 * saptılar: kenar çubuğu raporlar için Premium isterken rota Profesyonel
 * istiyordu, yani Profesyonel'e ödeme yapan işletme menüde kilit görüyor ama
 * sayfayı açabiliyordu.
 */

export type PlanName = 'Baslangic' | 'Profesyonel' | 'Premium';

/** Plan hiyerarşisi. Eski token'lardaki İngilizce adlar da karşılanıyor. */
export const PLAN_LEVELS: Record<string, number> = {
  Baslangic:   1,
  Profesyonel: 2,
  Premium:     3,
  // Geriye dönük uyumluluk — eski sürümlerin token'ları
  Basic:    1,
  Standard: 2,
  Full:     3,
};

/**
 * Sayfa yolu → en düşük gerekli plan. Burada olmayan yol herkese açık.
 * Anahtar, rotanın `path` değeri (baştaki eğik çizgi olmadan).
 */
export const PLAN_GATES: Record<string, PlanName> = {
  reports:  'Profesyonel',
  reviews:  'Profesyonel',
  branches: 'Premium',
};

/** Kullanıcının planı, istenen plana eşit ya da üstünde mi. */
export function planAtLeast(userPlan: string, required: PlanName | null | undefined): boolean {
  if (!required) return true;
  return (PLAN_LEVELS[userPlan] ?? 1) >= (PLAN_LEVELS[required] ?? 1);
}

/** Bir yolun istediği plan; yoksa null. */
export function planGateFor(route: string): PlanName | null {
  return PLAN_GATES[route.replace(/^\//, '')] ?? null;
}
