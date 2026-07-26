import { Heart, Star } from 'lucide-react';
import { Link } from 'react-router-dom';
import { mediaThumbnailUrl } from '../api/media';
import type { DishCardListItemDto } from '../api/types/dishes';
import { costLabels, difficultyLabels } from '../lib/labels';

/** «Новинка» — клиентская производная: опубликовано менее 14 дней назад. */
const NEW_BADGE_DAYS = 14;

function isNewDish(publishedAt: string | null): boolean {
  if (!publishedAt) return false;
  return Date.now() - new Date(publishedAt).getTime() < NEW_BADGE_DAYS * 24 * 60 * 60 * 1000;
}

/**
 * Карточка-«тарелка» каталога — строго по текущему DishCardListItemDto:
 * круглое фото, название, подводка, «сложность · стоимость», рейтинг только
 * при наличии оценок, сердечко-индикатор (не кнопка — Social Этап 5).
 * Времени и порций в контракте нет (бэклог pages.md §6 п. 2).
 */
export function DishCard({ dish }: { dish: DishCardListItemDto }) {
  return (
    <Link
      to={`/dishes/${dish.slug}`}
      className="group relative flex flex-col items-center rounded-card bg-surface p-5 text-center shadow-card transition-[transform,box-shadow] duration-[180ms] ease-[cubic-bezier(.2,0,0,1)] hover:-translate-y-1 hover:shadow-lifted"
    >
      {isNewDish(dish.publishedAt) && (
        <span className="absolute top-4 left-4 rounded-pill bg-success-bg px-2.5 py-1 text-xs font-medium text-success-text">
          Новинка
        </span>
      )}

      {dish.mainImageId ? (
        <img
          src={mediaThumbnailUrl(dish.mainImageId)}
          alt={dish.name}
          className="h-40 w-40 rounded-full object-cover"
          loading="lazy"
        />
      ) : (
        <div aria-hidden className="h-40 w-40 rounded-full bg-saffron-200" />
      )}

      <h3 className="mt-4 font-medium text-ink">{dish.name}</h3>

      {dish.shortDescription && (
        <p className="mt-1 line-clamp-2 text-sm text-ink-muted">{dish.shortDescription}</p>
      )}

      <p className="mt-2 text-sm text-ink-secondary">
        {difficultyLabels[dish.difficultyLevel]} · <span className="tabular">{costLabels[dish.costEstimate]}</span>
      </p>

      <div className="mt-3 flex w-full items-center justify-center gap-4 border-t border-line pt-3 text-sm empty:hidden">
        {dish.ratingCount > 0 && (
          <span className="tabular inline-flex items-center gap-1 font-medium text-ink">
            <Star className="h-4 w-4 fill-gold stroke-gold" aria-hidden />
            {dish.ratingAvg.toFixed(1)}
            <span className="font-normal text-ink-muted">({dish.ratingCount})</span>
          </span>
        )}
        {dish.favoritesCount > 0 && (
          <span className="tabular inline-flex items-center gap-1 text-action" title="В избранном">
            <Heart className="h-4 w-4" strokeWidth={1.75} aria-hidden />
            {dish.favoritesCount}
          </span>
        )}
      </div>
    </Link>
  );
}

/** Скелетон карточки на время загрузки (форма контента: круг + строки). */
export function DishCardSkeleton() {
  return (
    <div className="flex animate-pulse flex-col items-center rounded-card bg-surface p-5 shadow-card">
      <div className="h-40 w-40 rounded-full bg-sunken" />
      <div className="mt-4 h-4 w-32 rounded-pill bg-sunken" />
      <div className="mt-3 h-3 w-24 rounded-pill bg-sunken" />
    </div>
  );
}
