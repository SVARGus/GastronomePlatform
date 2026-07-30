import { ChevronRight, Eye, Heart, Info, Star } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { Link, useParams } from 'react-router-dom';
import { RecipeGate } from '../features/dish/RecipeGate';
import { useDishBySlugQuery, useIncrementViewsMutation } from '../features/dishes/dishesApi';
import { useUserProfileQuery } from '../features/users/usersApi';
import { mediaFileUrl, mediaThumbnailUrl } from '../shared/api/media';
import type { DishDetailDto } from '../shared/api/types/dishes';
import { userDisplayName } from '../shared/api/types/users';
import { costLabels, dietLabelOptions, difficultyLabels } from '../shared/lib/labels';
import { EmptyState } from '../shared/ui/EmptyState';

/**
 * Карточка блюда (приоритет 1, бриф 3): публичная часть по UC-DSH-051
 * (шапка блюда, описание, история) одинакова для всех; блок рецепта —
 * три состояния Premium-гейта (RecipeGate). Времени/порций/калорий
 * в шапке нет — по контракту они живут только внутри рецепта.
 */
export function DishPage() {
  const { slug } = useParams<{ slug: string }>();
  const { data: dish, error, isLoading } = useDishBySlugQuery(slug ?? '', { skip: !slug });

  // Счётчик просмотров — fire-and-forget один раз на блюдо (guard от StrictMode).
  const [incrementViews] = useIncrementViewsMutation();
  const countedDishId = useRef<string | null>(null);
  useEffect(() => {
    if (dish && countedDishId.current !== dish.id) {
      countedDishId.current = dish.id;
      incrementViews(dish.id);
    }
  }, [dish, incrementViews]);

  if (isLoading) {
    return <DishPageSkeleton />;
  }

  if (error || !dish) {
    return (
      <section className="mx-auto w-full max-w-[720px] px-6 py-16">
        <EmptyState
          title="Такого блюда нет или оно снято с публикации"
          text="Загляните в каталог — там много другого вкусного."
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

  const dietLabels = parseDietLabels(dish.dietLabelsMask);
  const descriptionParagraphs = splitParagraphs(dish.description);

  return (
    <article className="mx-auto w-full max-w-[1200px] px-6 py-8">
      {/* Хлебная крошка */}
      <nav aria-label="Хлебные крошки" className="flex items-center gap-1.5 text-sm">
        <Link to="/catalog" className="text-link hover:text-link-hover">
          Каталог
        </Link>
        <ChevronRight className="h-3.5 w-3.5 text-ink-muted" strokeWidth={1.75} aria-hidden />
        <span className="truncate text-ink-secondary">{dish.name}</span>
      </nav>

      {/* Шапка блюда: фото-«тарелка» + данные */}
      <div className="mt-8 flex flex-col items-center gap-8 md:flex-row md:items-start md:gap-12">
        <div className="shrink-0">
          {dish.mainImageId ? (
            <img
              src={mediaFileUrl(dish.mainImageId)}
              alt={dish.name}
              className="h-[280px] w-[280px] rounded-full object-cover shadow-card md:h-[420px] md:w-[420px]"
            />
          ) : (
            <div aria-hidden className="h-[280px] w-[280px] rounded-full bg-saffron-200 md:h-[420px] md:w-[420px]" />
          )}
        </div>

        <div className="min-w-0 flex-1 text-center md:text-left">
          <h1 className="text-[34px] font-[540] leading-[1.2] md:text-[40px]">{dish.name}</h1>

          <AuthorLine authorUserId={dish.authorUserId} />

          {dish.shortDescription && <p className="mt-3 text-lg text-ink-secondary">{dish.shortDescription}</p>}

          {/* Метаданные: сложность · стоимость · рейтинг · просмотры (без времени и порций) */}
          <div className="mt-5 flex flex-wrap items-center justify-center gap-x-6 gap-y-2 text-[15px] md:justify-start">
            <span className="text-ink">
              сложность <span className="font-medium">{difficultyLabels[dish.difficultyLevel]}</span>
            </span>
            <span className="tabular font-medium text-ink">{costLabels[dish.costEstimate]}</span>
            {dish.ratingCount > 0 && (
              <span className="tabular inline-flex items-center gap-1 font-medium text-ink">
                <Star className="h-4 w-4 fill-gold stroke-gold" aria-hidden />
                {dish.ratingAvg.toFixed(1)}
                <span className="font-normal text-ink-muted">({dish.ratingCount})</span>
              </span>
            )}
            <span className="tabular inline-flex items-center gap-1.5 text-ink-secondary">
              <Eye className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              {dish.viewsCount.toLocaleString('ru-RU')}
            </span>
            {dish.favoritesCount > 0 && (
              <span className="tabular inline-flex items-center gap-1.5 text-action" title="В избранном">
                <Heart className="h-4 w-4" strokeWidth={1.75} aria-hidden />
                {dish.favoritesCount}
              </span>
            )}
          </div>

          {dietLabels.length > 0 && (
            <div className="mt-4 flex flex-wrap justify-center gap-2 md:justify-start">
              {dietLabels.map((label) => (
                <span key={label} className="rounded-pill bg-success-bg px-3 py-1 text-sm font-medium text-success-text">
                  {label}
                </span>
              ))}
            </div>
          )}

          {dish.hasUnverifiedAllergens && (
            <p className="mt-5 flex items-start gap-2 rounded-card bg-warning-bg p-4 text-sm text-warning-text">
              <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
              Состав аллергенов может быть неполным — в рецепте есть ингредиенты вне справочника.
            </p>
          )}
        </div>
      </div>

      {/* О блюде */}
      {descriptionParagraphs.length > 0 && (
        <section className="mt-12 max-w-[760px]">
          <h2 className="text-2xl font-[540]">О блюде</h2>
          {descriptionParagraphs.map((paragraph, index) => (
            <p key={index} className="mt-3 text-ink-secondary">
              {paragraph}
            </p>
          ))}
        </section>
      )}

      {/* История блюда */}
      {dish.historyText && (
        <section className="mt-10 max-w-[760px] rounded-card bg-sunken p-6">
          <h2 className="text-2xl font-[540]">История блюда</h2>
          <p className="mt-3 whitespace-pre-line text-ink-secondary">{dish.historyText}</p>
        </section>
      )}

      {/* Рецепт за Premium-гейтом */}
      <div className="max-w-[760px]">
        <RecipeGate dishId={dish.id} />
      </div>
    </article>
  );
}

/** Строка автора: аватар + «Рецепт от N» — ссылка на публичную страницу автора. */
function AuthorLine({ authorUserId }: { authorUserId: string }) {
  const { data: author } = useUserProfileQuery(authorUserId);
  if (!author) return null;

  return (
    <p className="mt-3 flex items-center justify-center md:justify-start">
      <Link
        to={`/authors/${authorUserId}`}
        className="group flex items-center gap-2.5 text-ink-secondary hover:text-ink-secondary"
      >
        {author.avatarMediaId ? (
          <img
            src={mediaThumbnailUrl(author.avatarMediaId)}
            alt=""
            className="h-11 w-11 rounded-full object-cover"
          />
        ) : (
          <span
            aria-hidden
            className="flex h-11 w-11 items-center justify-center rounded-full bg-saffron-100 font-medium text-action"
          >
            {userDisplayName(author).charAt(0).toUpperCase()}
          </span>
        )}
        <span>
          Рецепт от{' '}
          <span className="font-medium text-link group-hover:text-link-hover">
            {userDisplayName(author)}
          </span>
        </span>
      </Link>
    </p>
  );
}

/** Скелетон страницы: круг фото + строки (форма контента, бриф 3). */
function DishPageSkeleton() {
  return (
    <section className="mx-auto w-full max-w-[1200px] animate-pulse px-6 py-8">
      <div className="h-4 w-40 rounded-pill bg-sunken" />
      <div className="mt-8 flex flex-col items-center gap-8 md:flex-row md:items-start md:gap-12">
        <div className="h-[280px] w-[280px] rounded-full bg-sunken md:h-[420px] md:w-[420px]" />
        <div className="w-full max-w-[480px]">
          <div className="h-9 w-3/4 rounded-pill bg-sunken" />
          <div className="mt-4 h-5 w-1/2 rounded-pill bg-sunken" />
          <div className="mt-6 h-4 w-full rounded-pill bg-sunken" />
          <div className="mt-2 h-4 w-2/3 rounded-pill bg-sunken" />
        </div>
      </div>
    </section>
  );
}

/** "Vegetarian, Halal" → русские подписи чипсов диет. */
function parseDietLabels(mask: DishDetailDto['dietLabelsMask']): string[] {
  return mask
    .split(',')
    .map((value) => value.trim())
    .filter((value) => value && value !== 'None')
    .map((value) => dietLabelOptions.find((option) => option.value === value)?.label ?? value);
}

/** Текст описания → абзацы по пустым строкам (markdown-рендер — бэклог). */
function splitParagraphs(text: string | null): string[] {
  if (!text) return [];
  return text
    .split(/\n{2,}|\r\n\r\n/)
    .map((paragraph) => paragraph.trim())
    .filter(Boolean);
}
