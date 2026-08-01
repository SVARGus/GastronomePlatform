import { Archive, ExternalLink, Info, Pencil, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { getErrorMessage } from '../auth/apiErrors';
import {
  useArchiveDishMutation,
  useDishesByAuthorQuery,
  useMyDraftsQuery,
  usePublishDishMutation,
  useUnpublishDishMutation,
} from '../dishes/dishesApi';
import { useMyProfileQuery } from '../users/usersApi';
import { mediaThumbnailUrl } from '../../shared/api/media';
import type { DishCardListItemDto, DishDraftListItemDto } from '../../shared/api/types/dishes';
import { costLabels, difficultyLabels, pluralize } from '../../shared/lib/labels';
import { Button } from '../../shared/ui/Button';

type DishesTab = 'drafts' | 'published' | 'unpublished';

const TAB_LABELS: Record<DishesTab, string> = {
  drafts: 'Черновики',
  published: 'Опубликованные',
  unpublished: 'Снятые',
};

/** Подтверждение опасного действия: архив (необратим для автора) или снятие. */
interface PendingAction {
  kind: 'archive' | 'unpublish';
  dishId: string;
  dishName: string;
}

/**
 * Раздел «Мои блюда» кабинета (приоритет 2, бриф 8): вкладки по статусам
 * (черновики UC-DSH-053, опубликованные UC-DSH-055, снятые UC-DSH-053
 * `status=Unpublished`), действия публикации/снятия/архива с подтверждениями.
 */
export function MyDishesSection() {
  const [tab, setTab] = useState<DishesTab>('drafts');
  const [pending, setPending] = useState<PendingAction | null>(null);
  const [publishErrors, setPublishErrors] = useState<Record<string, string>>({});

  const { data: me } = useMyProfileQuery();

  const { data: drafts, isLoading: draftsLoading } = useMyDraftsQuery({ status: 'Draft' });
  const { data: removed, isLoading: removedLoading } = useMyDraftsQuery({ status: 'Unpublished' });
  const { data: published, isLoading: publishedLoading } = useDishesByAuthorQuery(
    { authorUserId: me?.userId ?? '', pageSize: 25 },
    { skip: !me },
  );

  const [publish] = usePublishDishMutation();
  const [unpublish, { isLoading: isUnpublishing }] = useUnpublishDishMutation();
  const [archive, { isLoading: isArchiving }] = useArchiveDishMutation();

  const counts: Record<DishesTab, number | null> = {
    drafts: drafts?.totalCount ?? null,
    published: published?.totalCount ?? null,
    unpublished: removed?.totalCount ?? null,
  };
  const isLoading =
    (tab === 'drafts' && draftsLoading) ||
    (tab === 'published' && publishedLoading) ||
    (tab === 'unpublished' && removedLoading);

  async function handlePublish(dishId: string) {
    setPublishErrors((prev) => ({ ...prev, [dishId]: '' }));
    try {
      await publish(dishId).unwrap();
    } catch (err) {
      setPublishErrors((prev) => ({
        ...prev,
        [dishId]:
          getErrorMessage(err) ?? 'Не получилось опубликовать — проверьте заполненность блюда.',
      }));
    }
  }

  async function handleConfirmPending() {
    if (!pending) return;
    try {
      if (pending.kind === 'archive') {
        await archive(pending.dishId).unwrap();
      } else {
        await unpublish(pending.dishId).unwrap();
      }
    } finally {
      setPending(null);
    }
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-4">
        <Link
          to="/account/dishes/new"
          className="inline-flex h-11 items-center rounded-control bg-action px-5 text-[15px] font-medium text-on-action transition-colors duration-[120ms] hover:bg-action-hover hover:text-on-action"
        >
          <Plus className="mr-2 h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
          Создать блюдо
        </Link>
        {/* Вкладки переносятся на новую строку, а не прокручиваются: скрытую
            за краем экрана вкладку пользователь может просто не найти. */}
        <div className="flex flex-wrap gap-2">
          {(Object.keys(TAB_LABELS) as DishesTab[]).map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => setTab(key)}
              className={`tabular cursor-pointer rounded-pill px-4 py-2 text-[15px] font-medium whitespace-nowrap ${
                tab === key ? 'bg-saffron-50 text-action' : 'text-ink hover:bg-sunken'
              }`}
            >
              {TAB_LABELS[key]}
              {counts[key] !== null && ` (${counts[key]})`}
            </button>
          ))}
        </div>
      </div>

      <div className="mt-6 flex flex-col gap-4">
        {isLoading &&
          Array.from({ length: 3 }, (_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-card bg-sunken" />
          ))}

        {!isLoading && tab === 'drafts' && (
          <>
            {drafts?.items.map((dish) => (
              <DraftRow
                key={dish.id}
                dish={dish}
                badge={<StatusBadge kind="draft" />}
                dateLabel={`обновлено ${new Date(dish.updatedAt).toLocaleDateString('ru-RU')}`}
                error={publishErrors[dish.id] || null}
                actions={
                  <>
                    <EditLink dishId={dish.id} />
                    <Button variant="secondary" size="sm" onClick={() => handlePublish(dish.id)}>
                      Опубликовать
                    </Button>
                    <ArchiveButton onClick={() => setPending({ kind: 'archive', dishId: dish.id, dishName: dish.name })} />
                  </>
                }
              />
            ))}
            {drafts?.totalCount === 0 && (
              <DishesEmptyState
                title="У вас пока нет черновиков"
                text="Начните с малого — название и короткое описание."
              />
            )}
          </>
        )}

        {!isLoading && tab === 'published' && (
          <>
            {published?.items.map((dish) => (
              <PublishedRow
                key={dish.id}
                dish={dish}
                error={publishErrors[dish.id] || null}
                onPublishChanges={() => handlePublish(dish.id)}
                onUnpublish={() => setPending({ kind: 'unpublish', dishId: dish.id, dishName: dish.name })}
                onArchive={() => setPending({ kind: 'archive', dishId: dish.id, dishName: dish.name })}
              />
            ))}
            {published?.totalCount === 0 && (
              <DishesEmptyState
                title="Опубликованных блюд пока нет"
                text="Опубликуйте черновик — и блюдо появится в каталоге."
              />
            )}
          </>
        )}

        {!isLoading && tab === 'unpublished' && (
          <>
            {removed?.items.map((dish) => (
              <DraftRow
                key={dish.id}
                dish={dish}
                badge={<StatusBadge kind="unpublished" />}
                dateLabel={`снято ${new Date(dish.updatedAt).toLocaleDateString('ru-RU')}`}
                error={publishErrors[dish.id] || null}
                actions={
                  <>
                    <EditLink dishId={dish.id} />
                    <Button variant="secondary" size="sm" onClick={() => handlePublish(dish.id)}>
                      Опубликовать снова
                    </Button>
                    <ArchiveButton onClick={() => setPending({ kind: 'archive', dishId: dish.id, dishName: dish.name })} />
                  </>
                }
              />
            ))}
            {removed?.totalCount === 0 && (
              <DishesEmptyState
                title="Снятых с публикации блюд нет"
                text="Сюда попадают блюда, которые вы убрали с витрины."
              />
            )}
          </>
        )}
      </div>

      {pending && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-[rgba(42,33,26,0.4)] p-6"
          role="dialog"
          aria-modal="true"
        >
          <div className="w-full max-w-[440px] rounded-card bg-surface p-8 shadow-lifted">
            {pending.kind === 'archive' ? (
              <>
                <h2 className="text-2xl font-[540]">Архивировать блюдо?</h2>
                <p className="mt-3 text-[15px] text-ink-secondary">
                  «{pending.dishName}» исчезнет с витрины и из ваших списков. Вернуть его сможет
                  только администратор.
                </p>
              </>
            ) : (
              <>
                <h2 className="text-2xl font-[540]">Снять с публикации?</h2>
                <p className="mt-3 text-[15px] text-ink-secondary">
                  «{pending.dishName}» пропадёт из каталога и перейдёт во вкладку «Снятые» — оттуда
                  его можно опубликовать снова.
                </p>
              </>
            )}
            <div className="mt-6 flex gap-3">
              <Button variant="secondary" className="flex-1" onClick={() => setPending(null)}>
                Оставить
              </Button>
              <Button
                variant={pending.kind === 'archive' ? 'danger' : 'secondary'}
                className="flex-1"
                disabled={isArchiving || isUnpublishing}
                onClick={handleConfirmPending}
              >
                {pending.kind === 'archive' ? 'Да, архивировать' : 'Да, снять'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

/** Строка непубличного блюда (черновик или снятое). */
function DraftRow({
  dish,
  badge,
  dateLabel,
  error,
  actions,
}: {
  dish: DishDraftListItemDto;
  badge: React.ReactNode;
  dateLabel: string;
  error: string | null;
  actions: React.ReactNode;
}) {
  return (
    <div>
      <div className="rounded-card border border-line bg-surface p-4 shadow-card">
        <div className="flex items-center gap-4">
          <RowThumbnail mainImageId={dish.mainImageId} />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2.5">
              <span className="font-medium">{dish.name}</span>
              {badge}
            </div>
            {dish.shortDescription && (
              <p className="mt-0.5 truncate text-sm text-ink-muted">{dish.shortDescription}</p>
            )}
            <p className="tabular mt-0.5 text-[13px] text-ink-secondary">
              {dateLabel} · {difficultyLabels[dish.difficultyLevel]} · {costLabels[dish.costEstimate]}
            </p>
          </div>
        </div>
        {/* Действия всегда отдельной строкой под описанием — единообразно
            на любой ширине, текст не сжимается кнопками. */}
        <div className="mt-3 flex flex-wrap items-center gap-2">{actions}</div>
      </div>
      {error && (
        <p className="mt-2 flex items-start gap-2.5 rounded-control bg-warning-bg p-3 text-sm text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          {error}
        </p>
      )}
    </div>
  );
}

/** Строка опубликованного блюда — со статистикой и переходом на витрину. */
function PublishedRow({
  dish,
  error,
  onPublishChanges,
  onUnpublish,
  onArchive,
}: {
  dish: DishCardListItemDto;
  error: string | null;
  onPublishChanges: () => void;
  onUnpublish: () => void;
  onArchive: () => void;
}) {
  return (
    <div>
      <div className="rounded-card border border-line bg-surface p-4 shadow-card">
        <div className="flex items-center gap-4">
          <RowThumbnail mainImageId={dish.mainImageId} />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2.5">
              <span className="font-medium">{dish.name}</span>
              <StatusBadge kind="published" />
              {dish.hasUnsavedChanges === true && (
                <span
                  className="whitespace-nowrap rounded-pill bg-warning-bg px-2.5 py-0.5 text-[13px] font-medium text-warning-text"
                  title="Правки сохранены в рабочей версии — на витрину попадут после повторной публикации"
                >
                  есть неопубликованные правки
                </span>
              )}
            </div>
            {dish.shortDescription && (
              <p className="mt-0.5 truncate text-sm text-ink-muted">{dish.shortDescription}</p>
            )}
            <p className="tabular mt-0.5 text-[13px] text-ink-secondary">
              {dish.ratingCount > 0 && `★ ${dish.ratingAvg.toFixed(1)} (${dish.ratingCount}) · `}
              {dish.viewsCount.toLocaleString('ru-RU')}{' '}
              {pluralize(dish.viewsCount, ['просмотр', 'просмотра', 'просмотров'])}
              {dish.publishedAt && ` · опубликовано ${new Date(dish.publishedAt).toLocaleDateString('ru-RU')}`}
            </p>
          </div>
        </div>
        {/* Действия всегда отдельной строкой под описанием — единообразно
            на любой ширине, текст не сжимается кнопками. */}
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <Link
            to={`/dishes/${dish.slug}`}
            className="inline-flex h-9 items-center gap-1.5 whitespace-nowrap px-2.5 text-[15px] font-medium"
          >
            Открыть
            <ExternalLink className="h-3.5 w-3.5" strokeWidth={1.75} aria-hidden />
          </Link>
          <EditLink dishId={dish.id} />
          {dish.hasUnsavedChanges === true && (
            <Button variant="secondary" size="sm" onClick={onPublishChanges}>
              Опубликовать правки
            </Button>
          )}
          <Button variant="secondary" size="sm" onClick={onUnpublish}>
            Снять с публикации
          </Button>
          <ArchiveButton onClick={onArchive} />
        </div>
      </div>
      {error && (
        <p className="mt-2 flex items-start gap-2.5 rounded-control bg-warning-bg p-3 text-sm text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          {error}
        </p>
      )}
    </div>
  );
}

/** Мини-фото строки: круг 64px или пастельная заглушка. */
function RowThumbnail({ mainImageId }: { mainImageId: string | null }) {
  if (mainImageId) {
    return (
      <img
        src={mediaThumbnailUrl(mainImageId)}
        alt=""
        className="h-16 w-16 shrink-0 rounded-full object-cover ring-4 ring-surface shadow-chip"
      />
    );
  }
  return <span aria-hidden className="h-16 w-16 shrink-0 rounded-full bg-saffron-100" />;
}

/** Бейдж статуса блюда: черновик — нейтраль, опубликовано — success, снято — warning. */
function StatusBadge({ kind }: { kind: 'draft' | 'published' | 'unpublished' }) {
  if (kind === 'published') {
    return (
      <span className="rounded-pill bg-success-bg px-2.5 py-0.5 text-[13px] font-medium text-success-text">
        Опубликовано
      </span>
    );
  }
  if (kind === 'unpublished') {
    return (
      <span className="rounded-pill bg-warning-bg px-2.5 py-0.5 text-[13px] font-medium text-warning-text">
        Снято с публикации
      </span>
    );
  }
  return (
    <span className="rounded-pill bg-sunken px-2.5 py-0.5 text-[13px] font-medium text-ink-secondary">
      Черновик
    </span>
  );
}

/** Иконка-ссылка в редактор карточки блюда. */
function EditLink({ dishId }: { dishId: string }) {
  return (
    <Link
      to={`/account/dishes/${dishId}/edit`}
      title="Редактировать"
      aria-label="Редактировать"
      className="flex h-9 w-9 items-center justify-center rounded-control border border-line-strong text-ink hover:bg-sunken hover:text-ink"
    >
      <Pencil className="h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
    </Link>
  );
}

/** Иконка-кнопка архивирования (danger, с подсказкой). */
function ArchiveButton({ onClick }: { onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      title="Архивировать"
      aria-label="Архивировать"
      className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-control border border-danger text-danger-text hover:bg-danger-bg"
    >
      <Archive className="h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
    </button>
  );
}

/** Пустое состояние вкладки. */
function DishesEmptyState({ title, text }: { title: string; text: string }) {
  return (
    <div className="flex flex-col items-center rounded-card border border-line bg-surface px-7 py-11 text-center shadow-card">
      <svg viewBox="0 0 120 90" className="mb-4 h-20 w-28 text-ink-muted" aria-hidden>
        <g fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
          <path d="M42 28 C39 22, 44 18, 41 12" className="text-saffron-500" stroke="currentColor" />
          <path d="M60 26 C57 20, 62 16, 59 10" />
          <path d="M78 28 C75 22, 80 18, 77 12" />
          <ellipse cx="60" cy="62" rx="46" ry="16" />
          <ellipse cx="60" cy="60" rx="28" ry="9" />
        </g>
      </svg>
      <h3 className="text-[22px] font-[540]">{title}</h3>
      <p className="mt-2 max-w-[340px] text-[15px] text-ink-secondary">{text}</p>
    </div>
  );
}
