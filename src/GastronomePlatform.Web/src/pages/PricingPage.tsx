import { Check, Info } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import {
  formatPrice,
  grantLabels,
  offerSubtitle,
  offerTitle,
  planBadgeClasses,
} from '../features/subscriptions/planPresentation';
import { useSubscriptionCatalogQuery } from '../features/subscriptions/subscriptionsApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import type { SubscriptionCatalogPlanDto } from '../shared/api/types/subscriptions';
import { Button } from '../shared/ui/Button';

/** Бесплатный уровень «Соль» — статика, в каталоге API его нет (бриф 4). */
const SALT_FEATURES = ['Каталог блюд и поиск', 'Карточки блюд с описанием', 'История и описание рецептов'];

/**
 * Тарифы «Специи» (приоритет 1, макет SubscriptionPages 2a): «Соль» (статично),
 * планы витрины UC-SUB-040 (Шафран выделен, Трюфель с пометкой о роли).
 * «Оформить» ведёт на /subscribe?priceId=…; гостя чекаут сам отправит на вход.
 */
export function PricingPage() {
  const { data: plans, isLoading, isError } = useSubscriptionCatalogQuery();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Порядок как в макете: массовый план (без роль-гейта) в центре с подсветкой,
  // профессиональные (requiredRole) — после. Каталог порядок не гарантирует.
  const orderedPlans = [...(plans ?? [])].sort(
    (a, b) => Number(a.requiredRole !== null) - Number(b.requiredRole !== null),
  );
  const highlightedId = orderedPlans.find((p) => p.requiredRole === null)?.id ?? null;

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-14">
      <div className="text-center">
        <h1 className="text-[38px] font-[560] leading-[1.15] md:text-[44px]">Тарифы</h1>
        <p className="mx-auto mt-3 max-w-[560px] text-lg text-ink-secondary">
          Как специи: базовый вкус бесплатно, самое ценное — по подписке.
        </p>
      </div>

      {isError && (
        <p className="mt-10 rounded-card bg-surface p-8 text-center text-ink-secondary shadow-card">
          Не получилось загрузить тарифы. Обновите страницу или зайдите чуть позже.
        </p>
      )}

      <div className="mt-12 grid items-stretch gap-6 md:grid-cols-3">
        {/* Соль — бесплатный уровень */}
        <div className="flex flex-col rounded-card border border-line bg-surface p-7 shadow-card">
          <span className="self-start rounded-pill bg-success-bg px-3 py-1 text-sm font-medium text-success-text">
            Соль
          </span>
          <div className="tabular mt-4 font-display text-[32px] font-[560]">0 ₽</div>
          <div className="text-sm text-ink-muted">всегда</div>
          <FeatureList features={SALT_FEATURES} className="mt-5 flex-1" />
          <Link
            to={isAuthenticated ? '/catalog' : '/register'}
            className="mt-6 inline-flex h-11 items-center justify-center rounded-control border border-line-strong font-medium text-ink hover:bg-sunken hover:text-ink"
          >
            {isAuthenticated ? 'Открыть каталог' : 'Начать бесплатно'}
          </Link>
        </div>

        {isLoading && (
          <>
            <PlanCardSkeleton highlighted />
            <PlanCardSkeleton />
          </>
        )}

        {orderedPlans.map((plan) => (
          <CatalogPlanCard key={plan.id} plan={plan} highlighted={plan.id === highlightedId} />
        ))}
      </div>

      <p className="mt-8 text-center text-sm text-ink-muted">
        Отмена в любой момент; доступ сохраняется до конца оплаченного периода.
      </p>
    </section>
  );
}

/** Карточка плана из каталога: гранты, радио-выбор оффера, CTA оформления. */
function CatalogPlanCard({ plan, highlighted }: { plan: SubscriptionCatalogPlanDto; highlighted: boolean }) {
  const navigate = useNavigate();
  const [selectedOfferId, setSelectedOfferId] = useState(plan.offers[0]?.id ?? '');

  const features = plan.grants.map((g) => {
    const label = grantLabels[g.grant] ?? g.grant;
    return g.quantity !== null ? `${label} — ${g.quantity} в месяц` : label;
  });

  const singleOffer = plan.offers.length === 1 ? plan.offers[0] : null;

  return (
    <div
      className={`relative flex flex-col rounded-card bg-surface p-7 ${
        highlighted ? 'border-[1.5px] border-gold shadow-lifted md:-translate-y-2' : 'border border-line shadow-card'
      }`}
    >
      {highlighted && (
        <span className="absolute -top-3.5 left-1/2 -translate-x-1/2 rounded-pill border border-gold bg-gold-bg px-4 py-1 text-[13px] font-semibold whitespace-nowrap text-gold-text">
          Рекомендуем
        </span>
      )}

      <span className={`self-start rounded-pill px-3 py-1 text-sm font-medium ${planBadgeClasses(plan.publicName)}`}>
        {plan.publicName}
      </span>

      {singleOffer && (
        <>
          <div className="tabular mt-4 font-display text-[32px] font-[560]">{formatPrice(singleOffer.amount)}</div>
          <div className="text-sm text-ink-muted">
            {singleOffer.durationDays !== null ? `за ${formatDurationAccusative(singleOffer.durationDays)}` : 'бессрочно'}
          </div>
        </>
      )}

      <FeatureList features={features} className="mt-5 flex-1" />

      {plan.offers.length > 1 && (
        <div className="mt-5 flex flex-col gap-2" role="radiogroup" aria-label={`Варианты тарифа ${plan.publicName}`}>
          {plan.offers.map((offer) => {
            const selected = offer.id === selectedOfferId;
            return (
              <button
                key={offer.id}
                type="button"
                role="radio"
                aria-checked={selected}
                onClick={() => setSelectedOfferId(offer.id)}
                className={`flex w-full cursor-pointer items-start gap-3 rounded-control border p-3 text-left ${
                  selected ? 'border-action bg-saffron-50' : 'border-line bg-surface hover:bg-sunken'
                }`}
              >
                <span
                  aria-hidden
                  className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 ${
                    selected ? 'border-action' : 'border-line-strong'
                  }`}
                >
                  {selected && <span className="h-2.5 w-2.5 rounded-full bg-action" />}
                </span>
                <span className="min-w-0">
                  <span className="tabular block text-[15px] font-medium">{offerTitle(offer)}</span>
                  {offerSubtitle(offer) && (
                    <span className="block text-[13px] text-ink-muted">{offerSubtitle(offer)}</span>
                  )}
                </span>
              </button>
            );
          })}
        </div>
      )}

      {plan.requiredRole !== null && (
        <p className="mt-4 flex items-start gap-2.5 rounded-control bg-warning-bg p-3 text-[13px] leading-normal text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          Для профессиональных поваров — потребуется подтверждение статуса.
        </p>
      )}

      <Button
        variant={highlighted ? 'primary' : 'secondary'}
        size={highlighted ? 'lg' : 'md'}
        className="mt-6 w-full"
        disabled={selectedOfferId === ''}
        onClick={() => navigate(`/subscribe?priceId=${selectedOfferId}`)}
      >
        Оформить {plan.publicName}
      </Button>
    </div>
  );
}

/** Список услуг с зелёными галочками. */
function FeatureList({ features, className }: { features: string[]; className?: string }) {
  return (
    <ul className={`flex flex-col gap-3 ${className ?? ''}`}>
      {features.map((feature) => (
        <li key={feature} className="flex items-start gap-2.5 text-[15px]">
          <Check className="mt-0.5 h-[19px] w-[19px] shrink-0 text-success" strokeWidth={1.75} aria-hidden />
          {feature}
        </li>
      ))}
    </ul>
  );
}

/** «за месяц» / «за год» / «за N дней» — подпись под ценой единственного оффера. */
function formatDurationAccusative(durationDays: number): string {
  if (durationDays === 30 || durationDays === 31) return 'месяц';
  if (durationDays === 365 || durationDays === 366) return 'год';
  return `${durationDays} дней`;
}

/** Скелетон карточки плана на время загрузки каталога. */
function PlanCardSkeleton({ highlighted }: { highlighted?: boolean }) {
  return (
    <div
      className={`flex animate-pulse flex-col rounded-card border border-line bg-surface p-7 shadow-card ${
        highlighted ? 'md:-translate-y-2' : ''
      }`}
    >
      <div className="h-6 w-20 rounded-pill bg-sunken" />
      <div className="mt-5 h-8 w-28 rounded-pill bg-sunken" />
      <div className="mt-6 space-y-3">
        <div className="h-4 w-full rounded-pill bg-sunken" />
        <div className="h-4 w-4/5 rounded-pill bg-sunken" />
        <div className="h-4 w-3/5 rounded-pill bg-sunken" />
      </div>
      <div className="mt-auto pt-6">
        <div className="h-11 w-full rounded-control bg-sunken" />
      </div>
    </div>
  );
}
