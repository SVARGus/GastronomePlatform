import type {
  FeatureGrant,
  SubscriptionCatalogOfferDto,
} from '../../shared/api/types/subscriptions';
import { pluralize } from '../../shared/lib/labels';

/**
 * Витринное представление тарифов: русские подписи грантов, стили
 * бейджей-«специй», форматирование цен и офферов. Сервер отдаёт enum-ы —
 * формулировки живут на клиенте (см. remarks SubscriptionCatalogGrantResponse).
 */

/** Подписи услуг плана (FeatureGrant) для списков с галочками. */
export const grantLabels: Record<FeatureGrant, string> = {
  FullRecipes: 'Полные рецепты с пошаговыми фото',
  PortionCalculator: 'Калькулятор порций',
  SeasonalRecipes: 'Сезонные подборки',
  SpecialCategories: 'Особые категории рецептов',
  PromotionBasic: 'Продвижение ваших блюд',
  PromotionAdvanced: 'Расширенное продвижение блюд',
  DashboardAds: 'Реклама на витринах платформы',
  DashboardAdsExtended: 'Расширенная реклама на витринах',
};

/** Стиль бейджа-«специи» по витринному имени плана (бренд-бук §6). */
export function planBadgeClasses(publicName: string): string {
  const name = publicName.toLowerCase();
  if (name.includes('шафран')) return 'bg-gold-bg text-gold-text';
  if (name.includes('трюфель')) return 'bg-sunken text-ink';
  return 'bg-success-bg text-success-text';
}

/** Цена без копеек с разделителем тысяч: 2690 → «2 690 ₽». */
export function formatPrice(amount: number): string {
  return `${amount.toLocaleString('ru-RU', { maximumFractionDigits: 0 })} ₽`;
}

/** Период оффера словом: 30 дней → «месяц», 365 → «год», иначе «N дней». */
export function formatDuration(durationDays: number | null): string {
  if (durationDays === null) return 'бессрочно';
  if (durationDays === 30 || durationDays === 31) return 'месяц';
  if (durationDays === 365 || durationDays === 366) return 'год';
  return `${durationDays} ${pluralize(durationDays, ['день', 'дня', 'дней'])}`;
}

/**
 * Подпись оффера для радио-строки витрины: имя (витринное или период) + цена,
 * триал — без цены («7 дней бесплатно»), со скидкой — со старой ценой:
 * «Год со скидкой 25% — 2 690 ₽ (было 3 588 ₽)».
 */
export function offerTitle(offer: SubscriptionCatalogOfferDto): string {
  if (offer.trialDays !== null) {
    return (
      offer.publicName ??
      `${offer.trialDays} ${pluralize(offer.trialDays, ['день', 'дня', 'дней'])} бесплатно`
    );
  }
  const period = formatDuration(offer.durationDays);
  const name = offer.publicName ?? `${period.charAt(0).toUpperCase()}${period.slice(1)}`;
  const base = `${name} — ${formatPrice(offer.amount)}`;
  if (offer.compareAtAmount !== null) {
    return `${base} (было ${formatPrice(offer.compareAtAmount)})`;
  }
  return base;
}

/** Вторая строка радио-оффера: автопродление и переход после триала. */
export function offerSubtitle(offer: SubscriptionCatalogOfferDto): string | null {
  if (offer.trialDays !== null) return 'дальше по обычной цене, продлевается автоматически';
  if (offer.isRecurring) return 'продлевается автоматически';
  return null;
}
