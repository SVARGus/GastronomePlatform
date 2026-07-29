import { useState } from 'react';
import { Link } from 'react-router-dom';
import { formatPrice, planBadgeClasses } from '../subscriptions/planPresentation';
import {
  useCancelSubscriptionMutation,
  useMySubscriptionsQuery,
  useSubscriptionCatalogQuery,
} from '../subscriptions/subscriptionsApi';
import type { SubscriptionResponse, SubscriptionStatus } from '../../shared/api/types/subscriptions';
import { Button } from '../../shared/ui/Button';

/** Статусы, дающие доступ (фильтр POL-004 §4.4 — как на backend). */
const ACCESS_STATUSES: SubscriptionStatus[] = ['Trialing', 'Active', 'PastDue', 'Canceled'];

/**
 * Раздел «Подписка» кабинета (макет AccountPages 4b/4c/4d): текущая подписка
 * (UC-SUB-026) с отменой автопродления (UC-SUB-022, модалка подтверждения),
 * отменённая — с датой конца доступа, пустое состояние — приглашение в тарифы.
 */
export function SubscriptionSection() {
  const { data: subscriptions, isLoading, isError } = useMySubscriptionsQuery();
  const { data: plans } = useSubscriptionCatalogQuery();
  const [cancel, { isLoading: isCanceling }] = useCancelSubscriptionMutation();
  const [modalOpen, setModalOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="animate-pulse rounded-card bg-surface p-7 shadow-card">
        <div className="h-6 w-40 rounded-pill bg-sunken" />
        <div className="mt-5 h-4 w-full rounded-pill bg-sunken" />
        <div className="mt-2 h-4 w-2/3 rounded-pill bg-sunken" />
      </div>
    );
  }

  if (isError || !subscriptions) {
    return (
      <p className="rounded-card bg-surface p-7 text-ink-secondary shadow-card">
        Не получилось загрузить подписку. Обновите страницу или зайдите чуть позже.
      </p>
    );
  }

  const now = Date.now();
  const current =
    subscriptions.find(
      (s) => ACCESS_STATUSES.includes(s.status) && new Date(s.currentPeriodEnd).getTime() > now,
    ) ?? null;

  if (current === null) {
    return <EmptySubscription />;
  }

  const planName = plans?.find((p) => p.id === current.planId)?.publicName ?? 'Подписка';
  const isCanceled = current.status === 'Canceled' || current.cancelAtPeriodEnd;
  const accessUntil = new Date(current.currentPeriodEnd).toLocaleDateString('ru-RU');

  const rows: Array<[string, string]> = [
    ['Тариф', planName],
    ['Оплачено', formatPrice(current.snapshotAmount)],
    ['Действует до', accessUntil],
    [
      'Следующее списание',
      current.nextBillingAt !== null ? new Date(current.nextBillingAt).toLocaleDateString('ru-RU') : '—',
    ],
    ['Автопродление', current.autoRenew ? 'включено' : 'отключено'],
  ];

  async function handleCancel() {
    try {
      await cancel(current!.id).unwrap();
    } finally {
      setModalOpen(false);
    }
  }

  return (
    <>
      <div className="rounded-card border border-line bg-surface p-7 shadow-card">
        <div className="flex flex-wrap items-center gap-3">
          <span className={`rounded-pill px-3 py-1 text-sm font-medium ${planBadgeClasses(planName)}`}>
            {planName}
          </span>
          {isCanceled ? (
            <span className="rounded-pill bg-warning-bg px-3 py-1 text-sm font-medium text-warning-text">
              Отменена — доступ до {accessUntil}
            </span>
          ) : current.status === 'Trialing' ? (
            <span className="rounded-pill bg-trust-bg px-3 py-1 text-sm font-medium text-trust-text">
              Пробный период
            </span>
          ) : current.status === 'PastDue' ? (
            <span className="rounded-pill bg-warning-bg px-3 py-1 text-sm font-medium text-warning-text">
              Проблема с оплатой
            </span>
          ) : (
            <span className="rounded-pill bg-success-bg px-3 py-1 text-sm font-medium text-success-text">
              Активна
            </span>
          )}
        </div>

        <dl className="mt-5">
          {rows.map(([label, value]) => (
            <div key={label} className="flex justify-between gap-4 border-b border-line py-2.5">
              <dt className="text-ink-secondary">{label}</dt>
              <dd className="tabular font-medium">{value}</dd>
            </div>
          ))}
        </dl>

        {isCanceled ? (
          <p className="mt-5 text-sm text-ink-secondary">
            Подписка не продлится. Передумали?{' '}
            <Link to="/pricing" className="font-medium">
              Загляните в тарифы
            </Link>
            .
          </p>
        ) : (
          <div className="mt-6 flex flex-wrap items-center gap-4">
            <Button variant="danger" onClick={() => setModalOpen(true)}>
              Отменить подписку
            </Button>
            <span className="min-w-[180px] flex-1 text-sm text-ink-secondary">
              Доступ сохранится до конца оплаченного периода.
            </span>
          </div>
        )}
      </div>

      {modalOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-[rgba(42,33,26,0.4)] p-6"
          role="dialog"
          aria-modal="true"
          aria-labelledby="cancel-modal-title"
        >
          <div className="w-full max-w-[440px] rounded-card bg-surface p-8 shadow-lifted">
            <h2 id="cancel-modal-title" className="text-2xl font-[540]">
              Отменить подписку?
            </h2>
            <p className="mt-3 text-[15px] text-ink-secondary">
              Полные рецепты останутся доступны до {accessUntil}. После этого подписка не продлится,
              деньги больше не списываются.
            </p>
            <div className="mt-6 flex gap-3">
              <Button variant="secondary" className="flex-1" onClick={() => setModalOpen(false)}>
                Оставить подписку
              </Button>
              <Button variant="danger" className="flex-1" disabled={isCanceling} onClick={handleCancel}>
                {isCanceling ? 'Отменяем…' : 'Да, отменить'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

/** Пустое состояние: подписки нет — приглашение в тарифы (макет 4d, нижний блок). */
function EmptySubscription() {
  return (
    <div className="flex flex-col items-center rounded-card border border-line bg-surface px-7 py-11 text-center shadow-card">
      <svg viewBox="0 0 72 72" className="mb-5 h-[72px] w-[72px] text-line-strong" fill="none" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
        <path d="M26 30 L26 24 C26 18.5 30.5 14 36 14 C41.5 14 46 18.5 46 24 L46 30" className="stroke-action" stroke="currentColor" />
        <path d="M22 30 L50 30 L47 58 C47 60 45.5 61 44 61 L28 61 C26.5 61 25 60 25 58 Z" stroke="currentColor" />
      </svg>
      <h2 className="text-[22px] font-[540]">У вас пока нет подписки</h2>
      <p className="mt-2 max-w-[360px] text-[15px] text-ink-secondary">
        Полные рецепты и калькулятор порций — в тарифе Шафран.
      </p>
      <Link
        to="/pricing"
        className="mt-6 inline-flex h-11 items-center justify-center rounded-control bg-action px-5 font-medium text-on-action hover:bg-action-hover hover:text-on-action"
      >
        Посмотреть тарифы
      </Link>
    </div>
  );
}
