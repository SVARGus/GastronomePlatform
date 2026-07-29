import { Check, ChevronRight, CircleAlert, ShieldCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { getErrorCode } from '../features/auth/apiErrors';
import {
  formatPrice,
  offerTitle,
  planBadgeClasses,
} from '../features/subscriptions/planPresentation';
import {
  useSubscribeMutation,
  useSubscriptionCatalogQuery,
} from '../features/subscriptions/subscriptionsApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import { pluralize } from '../shared/lib/labels';
import { Button } from '../shared/ui/Button';

/** Заглушка токена способа оплаты: реального шлюза в Phase A нет (тестовый режим). */
const DEMO_PAYMENT_METHOD_ID = 'demo-test-card-4242';

/**
 * Оформление подписки (приоритет 1, макет SubscriptionPages 2c/2d/2e):
 * выбор из витрины приходит как ?priceId=…, оплата — тестовый режим,
 * чекбокс оферты обязателен (UC-SUB-020 требует AcceptedTermsAt).
 * Ошибки контракта: 409 ALREADY_HAS_BASE и 403 FORBIDDEN_ROLE_REQUIRED — плашками.
 */
export function SubscribePage() {
  const [searchParams] = useSearchParams();
  const priceId = searchParams.get('priceId');

  const navigate = useNavigate();
  const location = useLocation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  const { data: plans, isLoading: isCatalogLoading } = useSubscriptionCatalogQuery();
  const [subscribe, { isLoading: isSubscribing, error, isSuccess }] = useSubscribeMutation();
  const [agreed, setAgreed] = useState(false);

  // Оформление требует входа — гость уходит на /login с возвратом сюда.
  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: `${location.pathname}${location.search}` } });
    }
  }, [isAuthenticated, navigate, location.pathname, location.search]);

  const plan = plans?.find((p) => p.offers.some((o) => o.id === priceId)) ?? null;
  const offer = plan?.offers.find((o) => o.id === priceId) ?? null;

  if (!isAuthenticated) return null;

  if (isCatalogLoading) {
    return (
      <section className="mx-auto w-full max-w-[960px] animate-pulse px-6 py-10">
        <div className="h-4 w-44 rounded-pill bg-sunken" />
        <div className="mt-8 h-9 w-80 rounded-pill bg-sunken" />
        <div className="mt-8 h-40 w-full max-w-[560px] rounded-card bg-sunken" />
      </section>
    );
  }

  // priceId отсутствует или оффер ушёл с витрины — вернуть к выбору тарифа.
  if (!plan || !offer) {
    return (
      <section className="mx-auto w-full max-w-[640px] px-6 py-16 text-center">
        <h1 className="text-[30px] font-[560]">Тариф не выбран</h1>
        <p className="mt-3 text-ink-secondary">
          Похоже, ссылка устарела или вариант оплаты снят с витрины. Выберите тариф заново.
        </p>
        <Link
          to="/pricing"
          className="mt-6 inline-flex h-11 items-center justify-center rounded-control bg-action px-5 font-medium text-on-action hover:bg-action-hover hover:text-on-action"
        >
          К тарифам
        </Link>
      </section>
    );
  }

  if (isSuccess) {
    return <SubscribeSuccess planName={plan.publicName} />;
  }

  const isTrial = offer.trialDays !== null;
  const accessDays = offer.trialDays ?? offer.durationDays;
  const accessUntil =
    accessDays !== null
      ? new Date(Date.now() + accessDays * 24 * 60 * 60 * 1000).toLocaleDateString('ru-RU')
      : null;

  const errorCode = getErrorCode(error);
  const payLabel = isTrial
    ? 'Активировать бесплатный период'
    : `Оплатить ${formatPrice(offer.amount)}`;

  async function handleSubscribe() {
    try {
      await subscribe({
        priceId: offer!.id,
        paymentMethodId: DEMO_PAYMENT_METHOD_ID,
        acceptedTermsAt: new Date().toISOString(),
      }).unwrap();
    } catch {
      // Ошибка отражена в error — разбирается плашками ниже.
    }
  }

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 pb-14">
      <nav aria-label="Хлебные крошки" className="flex items-center gap-1.5 py-5 text-sm">
        <Link to="/pricing" className="text-link hover:text-link-hover">
          Тарифы
        </Link>
        <ChevronRight className="h-3.5 w-3.5 text-ink-muted" strokeWidth={1.75} aria-hidden />
        <span className="text-ink-secondary">Оформление</span>
      </nav>

      {errorCode === 'SUBS.ALREADY_HAS_BASE' && (
        <p className="mb-6 flex items-start gap-3 rounded-card bg-danger-bg p-4 text-[15px] text-danger-text">
          <CircleAlert className="mt-0.5 h-5 w-5 shrink-0" strokeWidth={1.75} aria-hidden />
          <span>
            У вас уже есть базовая подписка. Управлять ею можно в личном кабинете.{' '}
            <Link to="/account" className="font-medium text-danger-text underline hover:text-danger-text">
              Открыть кабинет
            </Link>
          </span>
        </p>
      )}
      {errorCode === 'SUBS.FORBIDDEN_ROLE_REQUIRED' && (
        <p className="mb-6 flex items-start gap-3 rounded-card bg-warning-bg p-4 text-[15px] text-warning-text">
          <CircleAlert className="mt-0.5 h-5 w-5 shrink-0" strokeWidth={1.75} aria-hidden />
          Этот тариф доступен только подтверждённым поварам. Подтверждение статуса появится в кабинете позже.
        </p>
      )}
      {error !== undefined && errorCode !== 'SUBS.ALREADY_HAS_BASE' && errorCode !== 'SUBS.FORBIDDEN_ROLE_REQUIRED' && (
        <p className="mb-6 flex items-start gap-3 rounded-card bg-danger-bg p-4 text-[15px] text-danger-text">
          <CircleAlert className="mt-0.5 h-5 w-5 shrink-0" strokeWidth={1.75} aria-hidden />
          Не получилось оформить подписку. Попробуйте ещё раз или зайдите позже.
        </p>
      )}

      <div className="grid items-start gap-10 md:grid-cols-[1fr_360px]">
        <div>
          <h1 className="text-[32px] font-[560] leading-[1.2] md:text-[36px]">Оформление подписки</h1>

          {/* Ваш выбор */}
          <div className="mt-7 rounded-card border border-line bg-surface p-6 shadow-card">
            <div className="flex items-center justify-between">
              <span className="text-sm font-semibold text-ink-secondary">Ваш выбор</span>
              <Link to="/pricing" className="text-sm font-medium">
                Изменить тариф
              </Link>
            </div>
            <div className="mt-4 flex items-center gap-3">
              <span className={`rounded-pill px-3 py-1 text-sm font-medium ${planBadgeClasses(plan.publicName)}`}>
                {plan.publicName}
              </span>
              <span className="font-medium">{offerTitle(offer)}</span>
            </div>
            <div className="mt-3 flex items-baseline gap-3">
              <span className="tabular font-display text-[36px] font-[560]">{formatPrice(offer.amount)}</span>
              {offer.compareAtAmount !== null && (
                <span className="tabular text-lg text-ink-muted line-through">
                  {formatPrice(offer.compareAtAmount)}
                </span>
              )}
            </div>
            {accessDays !== null && (
              <p className="tabular mt-2 text-sm text-ink-secondary">
                {isTrial ? 'Пробный период' : 'Оплаченный период'}: {accessDays}{' '}
                {pluralize(accessDays, ['день', 'дня', 'дней'])}
                {offer.isRecurring && ' · продлевается автоматически'}
              </p>
            )}
          </div>

          {/* Способ оплаты — тестовый режим */}
          <div className="mt-5 rounded-card bg-trust-bg p-6">
            <div className="flex items-center gap-2.5">
              <ShieldCheck className="h-5 w-5 text-trust" strokeWidth={1.75} aria-hidden />
              <span className="text-sm font-semibold text-trust-text">Способ оплаты</span>
            </div>
            <div className="mt-4 flex items-start gap-3 rounded-control border-[1.5px] border-trust bg-surface p-3.5">
              <span
                aria-hidden
                className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 border-trust"
              >
                <span className="h-2.5 w-2.5 rounded-full bg-trust" />
              </span>
              <span>
                <span className="tabular block text-[15px] font-medium text-trust-text">
                  Тестовая карта •••• 4242
                </span>
                <span className="block text-[13px] text-trust">
                  демонстрационный платёж, деньги не списываются
                </span>
              </span>
            </div>
            <p className="mt-3 text-[13px] text-trust">Платёжный модуль работает в тестовом режиме.</p>
          </div>

          {/* Согласие с офертой */}
          <label className="mt-6 flex cursor-pointer items-start gap-3">
            <input
              type="checkbox"
              checked={agreed}
              onChange={(e) => setAgreed(e.target.checked)}
              className="mt-1 h-4.5 w-4.5 shrink-0 cursor-pointer accent-[#C25423]"
            />
            <span className="text-[15px] text-ink">
              Принимаю условия оферты{offer.isRecurring && ' и согласен на автоматическое продление'}
            </span>
          </label>

          <Button
            variant="primary"
            size="lg"
            className="mt-6 w-full md:max-w-[420px]"
            disabled={!agreed || isSubscribing}
            onClick={handleSubscribe}
          >
            {isSubscribing ? 'Оформляем…' : payLabel}
          </Button>
        </div>

        {/* Сводка */}
        <aside className="rounded-card border border-line bg-surface p-6 shadow-card md:sticky md:top-6">
          <h2 className="text-sm font-semibold text-ink-secondary">Сводка</h2>
          <dl>
            <div className="flex justify-between border-b border-line py-2.5">
              <dt className="text-ink-secondary">Тариф</dt>
              <dd className="font-medium">{plan.publicName}</dd>
            </div>
            <div className="flex justify-between border-b border-line py-2.5">
              <dt className="text-ink-secondary">Вариант</dt>
              <dd className="tabular max-w-[60%] text-right font-medium">{offerTitle(offer)}</dd>
            </div>
            {accessUntil && (
              <div className="flex justify-between border-b border-line py-2.5">
                <dt className="text-ink-secondary">Доступ до</dt>
                <dd className="tabular font-medium">{accessUntil}</dd>
              </div>
            )}
          </dl>
          <div className="flex items-baseline justify-between pt-4">
            <span className="font-semibold">Итого</span>
            <span className="tabular font-display text-[26px] font-[560]">{formatPrice(offer.amount)}</span>
          </div>
        </aside>
      </div>
    </section>
  );
}

/** Экран успеха (фрейм 2d): тарелка-галочка с «паром», CTA к рецептам. */
function SubscribeSuccess({ planName }: { planName: string }) {
  return (
    <section className="mx-auto flex w-full max-w-[640px] flex-col items-center px-6 py-20 text-center">
      <div className="relative mb-7">
        <svg
          viewBox="0 0 70 44"
          className="absolute -top-9 left-1/2 h-11 w-[70px] -translate-x-1/2 text-brand"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.75"
          strokeLinecap="round"
          aria-hidden
        >
          <path d="M20 40c-3-6 3-9 0-15" />
          <path d="M35 42c-3-6 3-9 0-15" />
          <path d="M50 40c-3-6 3-9 0-15" />
        </svg>
        <div className="flex h-[132px] w-[132px] items-center justify-center rounded-full bg-success-bg shadow-card ring-8 ring-surface">
          <Check className="h-14 w-14 text-success" strokeWidth={2} aria-hidden />
        </div>
      </div>
      <h1 className="text-[30px] font-[540]">Подписка {planName} оформлена</h1>
      <p className="mt-3 max-w-[420px] text-[17px] text-ink-secondary">
        Полные рецепты уже открыты. Приятного аппетита!
      </p>
      <Link
        to="/catalog"
        className="mt-7 inline-flex h-[52px] items-center justify-center rounded-control bg-action px-6 font-medium text-on-action hover:bg-action-hover hover:text-on-action"
      >
        Открыть рецепты
      </Link>
      <Link to="/account" className="mt-4 text-[15px] font-medium">
        Мои подписки
      </Link>
    </section>
  );
}
