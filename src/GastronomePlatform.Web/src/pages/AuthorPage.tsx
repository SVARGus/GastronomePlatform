import { CalendarDays, ChevronRight, MapPin } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useDishesByAuthorQuery } from '../features/dishes/dishesApi';
import { useUserProfileQuery } from '../features/users/usersApi';
import { mediaThumbnailUrl } from '../shared/api/media';
import type { DishCardListItemDto } from '../shared/api/types/dishes';
import { userDisplayName } from '../shared/api/types/users';
import { pluralize } from '../shared/lib/labels';
import { Button } from '../shared/ui/Button';
import { DishCard, DishCardSkeleton } from '../shared/ui/DishCard';
import { EmptyState } from '../shared/ui/EmptyState';

const PAGE_SIZE = 12;

/** Месяцы в родительном падеже — «На платформе с мая 2026». */
const MONTHS_GENITIVE = [
  'января', 'февраля', 'марта', 'апреля', 'мая', 'июня',
  'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря',
];

function formatSince(createdAt: string): string {
  const date = new Date(createdAt);
  return `${MONTHS_GENITIVE[date.getMonth()]} ${date.getFullYear()}`;
}

/**
 * Публичная страница автора (приоритет 2, бриф 7): профиль (с гейтом
 * приватности — bio и город только у публичного) + опубликованные блюда
 * каталожными карточками (UC-DSH-055, «Показать ещё»).
 */
export function AuthorPage() {
  const { userId } = useParams<{ userId: string }>();

  const { data: author, error: authorError, isLoading: isAuthorLoading } =
    useUserProfileQuery(userId ?? '', { skip: !userId });

  const [page, setPage] = useState(1);
  const [items, setItems] = useState<DishCardListItemDto[]>([]);
  const { data: dishesPage, isFetching } = useDishesByAuthorQuery(
    { authorUserId: userId ?? '', page, pageSize: PAGE_SIZE },
    { skip: !userId },
  );

  // Накопление страниц «Показать ещё» (как в каталоге).
  useEffect(() => {
    if (!dishesPage) return;
    if (dishesPage.page === 1) {
      setItems(dishesPage.items);
    } else {
      setItems((prev) => {
        const known = new Set(prev.map((i) => i.id));
        return [...prev, ...dishesPage.items.filter((i) => !known.has(i.id))];
      });
    }
  }, [dishesPage]);

  if (isAuthorLoading) {
    return (
      <section className="mx-auto w-full max-w-[1200px] animate-pulse px-6 py-8">
        <div className="h-4 w-40 rounded-pill bg-sunken" />
        <div className="mt-7 flex items-center gap-7">
          <div className="h-28 w-28 rounded-full bg-sunken" />
          <div>
            <div className="h-8 w-56 rounded-pill bg-sunken" />
            <div className="mt-3 h-4 w-40 rounded-pill bg-sunken" />
          </div>
        </div>
      </section>
    );
  }

  if (authorError || !author) {
    return (
      <section className="mx-auto w-full max-w-[640px] px-6 py-16">
        <EmptyState
          title="Такого автора нет"
          text="Загляните в каталог — там много других поваров и блюд."
          action={
            <Link
              to="/catalog"
              className="inline-flex h-11 items-center justify-center rounded-control border border-line-strong px-5 font-medium text-ink hover:bg-sunken hover:text-ink"
            >
              В каталог
            </Link>
          }
        />
      </section>
    );
  }

  const name = userDisplayName(author);
  const totalCount = dishesPage?.totalCount ?? null;
  const hasMore = dishesPage !== undefined && page * PAGE_SIZE < dishesPage.totalCount;

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-7">
      {/* Хлебная крошка */}
      <nav aria-label="Хлебные крошки" className="flex items-center gap-1.5 pb-6 text-sm">
        <Link to="/catalog" className="text-link hover:text-link-hover">
          Каталог
        </Link>
        <ChevronRight className="h-3.5 w-3.5 text-ink-muted" strokeWidth={1.75} aria-hidden />
        <span className="truncate text-ink-secondary">{name}</span>
      </nav>

      {/* Профиль автора */}
      <div className="flex flex-col items-center gap-5 text-center md:flex-row md:items-center md:gap-7 md:text-left">
        {author.avatarMediaId ? (
          <img
            src={mediaThumbnailUrl(author.avatarMediaId)}
            alt=""
            className="h-28 w-28 shrink-0 rounded-full object-cover shadow-card ring-[6px] ring-surface"
          />
        ) : (
          <span
            aria-hidden
            className="flex h-28 w-28 shrink-0 items-center justify-center rounded-full bg-saffron-50 font-display text-[44px] font-[560] text-action shadow-card ring-[6px] ring-surface"
          >
            {name.charAt(0).toUpperCase()}
          </span>
        )}
        <div className="min-w-0">
          <h1 className="text-[32px] font-[560] leading-[1.15] md:text-[36px]">{name}</h1>
          <p className="mt-1 text-ink-secondary">@{author.userName}</p>
          <div className="mt-2.5 flex flex-wrap items-center justify-center gap-x-3.5 gap-y-1.5 text-[15px] text-ink-muted md:justify-start">
            {author.city && (
              <span className="inline-flex items-center gap-1.5">
                <MapPin className="h-4 w-4" strokeWidth={1.75} aria-hidden />
                {author.city}
              </span>
            )}
            <span className="inline-flex items-center gap-1.5">
              <CalendarDays className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              На платформе с {formatSince(author.createdAt)}
            </span>
          </div>
          {author.bio && <p className="mt-3.5 line-clamp-2 max-w-[520px] text-ink">{author.bio}</p>}
        </div>
      </div>

      {/* Блюда автора */}
      <div className="mt-12">
        <div className="flex items-baseline gap-3 border-b border-line pb-4">
          <h2 className="text-[26px] font-[540]">Блюда автора</h2>
          {totalCount !== null && (
            <span className="tabular text-[15px] text-ink-secondary">
              {totalCount} {pluralize(totalCount, ['блюдо', 'блюда', 'блюд'])}
            </span>
          )}
        </div>

        {isFetching && page === 1 && (
          <div className="mt-7 grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3">
            {Array.from({ length: 3 }, (_, i) => (
              <DishCardSkeleton key={i} />
            ))}
          </div>
        )}

        {totalCount === 0 && (
          <div className="mt-7">
            <EmptyState
              title="Автор пока не опубликовал ни одного блюда"
              text="Загляните позже — вкусное готовится."
              action={
                <Link to="/catalog" className="font-medium">
                  В каталог
                </Link>
              }
            />
          </div>
        )}

        {items.length > 0 && (
          <>
            <div className="mt-7 grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3">
              {items.map((dish) => (
                <DishCard key={dish.id} dish={dish} />
              ))}
            </div>
            {hasMore && (
              <div className="mt-8 text-center">
                <Button variant="secondary" onClick={() => setPage((p) => p + 1)} disabled={isFetching}>
                  {isFetching ? 'Загружаем…' : 'Показать ещё'}
                </Button>
              </div>
            )}
          </>
        )}
      </div>
    </section>
  );
}
