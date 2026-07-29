/**
 * Типы контракта модуля Subscriptions (ручное повторение DTO backend,
 * как в dishes.ts). Enum-ы — строками (JsonStringEnumConverter).
 */

export type PlanKind = 'Base' | 'AddOn';
export type OfferKind = 'Trial' | 'Intro' | 'Standard' | 'Retention' | 'DunningFallback';
export type FeatureGrant =
  | 'FullRecipes'
  | 'PortionCalculator'
  | 'SeasonalRecipes'
  | 'SpecialCategories'
  | 'PromotionBasic'
  | 'PromotionAdvanced'
  | 'DashboardAds'
  | 'DashboardAdsExtended';
export type SubscriptionStatus = 'Scheduled' | 'Trialing' | 'Active' | 'PastDue' | 'Canceled' | 'Expired';
export type RecurringDisabledReason = 'Antifraud' | 'AttemptsExhausted' | 'UserCanceled' | 'PeriodEnded';

/** Позиция состава услуг плана на витрине (UC-SUB-040). */
export interface SubscriptionCatalogGrantDto {
  grant: FeatureGrant;
  quantity: number | null;
}

/** Витринный оффер плана (UC-SUB-040) — вариант периода и цены. */
export interface SubscriptionCatalogOfferDto {
  id: string;
  kind: OfferKind;
  publicName: string | null;
  amount: number;
  currency: string;
  compareAtAmount: number | null;
  discountPercent: number | null;
  durationDays: number | null;
  trialDays: number | null;
  isRecurring: boolean;
}

/** Витринная карточка тарифного плана (UC-SUB-040). Offers непуст по построению. */
export interface SubscriptionCatalogPlanDto {
  id: string;
  planKind: PlanKind;
  publicName: string;
  description: string | null;
  requiredRole: string | null;
  grants: SubscriptionCatalogGrantDto[];
  offers: SubscriptionCatalogOfferDto[];
}

/** Тело POST /api/user-subscriptions (UC-SUB-020). */
export interface SubscribeRequest {
  priceId: string;
  paymentMethodId: string;
  acceptedTermsAt: string;
}

/** Результат оформления подписки (201). */
export interface SubscribeResult {
  subscriptionId: string;
}

/** Карточка подписки (UC-SUB-021). Имя плана/оффера клиент берёт из витрины. */
export interface SubscriptionResponse {
  id: string;
  userId: string;
  planId: string;
  currentPriceId: string;
  status: SubscriptionStatus;
  snapshotAmount: number;
  snapshotCurrency: string;
  startsAt: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEnd: string | null;
  nextBillingAt: string | null;
  autoRenew: boolean;
  cancelAtPeriodEnd: boolean;
  recurringDisabledReason: RecurringDisabledReason | null;
  canceledAt: string | null;
  endedAt: string | null;
  createdAt: string;
  updatedAt: string;
}
