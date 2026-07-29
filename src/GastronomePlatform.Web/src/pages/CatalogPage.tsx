import { ChevronDown, Search, SlidersHorizontal } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { AppliedFilterChips } from '../features/catalog/AppliedFilterChips';
import { FiltersSidebar } from '../features/catalog/FiltersSidebar';
import { MobileFiltersDrawer } from '../features/catalog/MobileFiltersDrawer';
import { useCatalogFilters } from '../features/catalog/useCatalogFilters';
import {
  useCategoryTreeQuery,
  usePopularTagsQuery,
  useSearchDishesQuery,
} from '../features/dishes/dishesApi';
import type { DishCardListItemDto, DishSearchSortBy, SearchDishesParams } from '../shared/api/types/dishes';
import { pluralize, sortLabels } from '../shared/lib/labels';
import { Button } from '../shared/ui/Button';
import { DishCard, DishCardSkeleton } from '../shared/ui/DishCard';
import { EmptyState } from '../shared/ui/EmptyState';
import { MarqueeText } from '../shared/ui/MarqueeText';

const PAGE_SIZE = 12;

/**
 * Каталог блюд (приоритет 1, бриф 2): фильтры строго по контракту UC-DSH-054,
 * чипсы применённых фильтров, сетка карточек, «Показать ещё» (накопление
 * страниц), скелетоны и пустая выдача.
 */
export function CatalogPage() {
  const { filters, hasActiveFilters, toggle, setText, setSortBy, toggleMinRating, reset } =
    useCatalogFilters();

  const { data: categories } = useCategoryTreeQuery();
  const { data: tags } = usePopularTagsQuery();

  const [page, setPage] = useState(1);
  const [items, setItems] = useState<DishCardListItemDto[]>([]);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Строка поиска: локальный черновик, применяется по Enter/кнопке/уходу фокуса.
  // Синхронизируется с URL-фильтром (переход с главной, снятие чипса «Поиск»).
  const [searchDraft, setSearchDraft] = useState(filters.text);
  useEffect(() => {
    setSearchDraft(filters.text);
  }, [filters.text]);

  function applySearch() {
    if (searchDraft.trim() !== filters.text) {
      setText(searchDraft.trim());
    }
  }

  // Смена любого фильтра начинает выдачу с первой страницы.
  const filtersKey = useMemo(() => JSON.stringify(filters), [filters]);
  useEffect(() => {
    setPage(1);
  }, [filtersKey]);

  const params: SearchDishesParams = useMemo(
    () => ({
      text: filters.text || undefined,
      categoryIds: filters.categoryIds.length > 0 ? filters.categoryIds : undefined,
      tagIds: filters.tagIds.length > 0 ? filters.tagIds : undefined,
      dietLabelsMask: filters.diets.length > 0 ? filters.diets.join(',') : undefined,
      difficulties: filters.difficulties.length > 0 ? filters.difficulties : undefined,
      costs: filters.costs.length > 0 ? filters.costs : undefined,
      minRating: filters.minRating ?? undefined,
      sortBy: filters.sortBy,
      page,
      pageSize: PAGE_SIZE,
    }),
    [filtersKey, page],
  );

  const { data, isFetching, isError } = useSearchDishesQuery(params);

  // Накопление страниц для «Показать ещё»: первая страница заменяет выдачу.
  useEffect(() => {
    if (!data) return;
    if (data.page === 1) {
      setItems(data.items);
    } else {
      setItems((prev) => {
        const known = new Set(prev.map((i) => i.id));
        return [...prev, ...data.items.filter((i) => !known.has(i.id))];
      });
    }
  }, [data]);

  const totalCount = data?.totalCount ?? null;
  const hasMore = data !== undefined && page * PAGE_SIZE < data.totalCount;
  const isInitialLoading = isFetching && page === 1;
  const showEmpty = !isFetching && !isError && data !== undefined && data.totalCount === 0;

  const activeFilterCount =
    filters.categoryIds.length +
    filters.tagIds.length +
    filters.diets.length +
    filters.difficulties.length +
    filters.costs.length +
    (filters.minRating !== null ? 1 : 0);

  const sidebar = (
    <FiltersSidebar
      filters={filters}
      categories={categories ?? []}
      tags={tags ?? []}
      onToggle={toggle}
      onToggleMinRating={toggleMinRating}
      onReset={reset}
      hasActiveFilters={hasActiveFilters}
    />
  );

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-10">
      <div className="flex flex-wrap items-baseline justify-between gap-4">
        <h1 className="text-[34px] font-[540] leading-[1.2]">
          Каталог блюд
          {totalCount !== null && (
            <span className="tabular ml-3 align-middle text-base font-normal text-ink-secondary">
              {totalCount} {pluralize(totalCount, ['блюдо', 'блюда', 'блюд'])}
            </span>
          )}
        </h1>

        <button
          type="button"
          onClick={() => setDrawerOpen(true)}
          className="inline-flex h-10 items-center gap-2 rounded-control border border-line bg-surface px-4 text-[15px] font-medium text-ink shadow-chip md:hidden"
        >
          <SlidersHorizontal className="h-4 w-4" strokeWidth={1.75} aria-hidden />
          Фильтры
          {activeFilterCount > 0 && (
            <span className="tabular flex h-5 min-w-5 items-center justify-center rounded-pill bg-action px-1 text-xs font-medium text-on-action">
              {activeFilterCount}
            </span>
          )}
        </button>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          applySearch();
        }}
        className="mt-6 flex max-w-[560px] gap-3"
      >
        <label className="relative flex-1">
          <Search
            className="pointer-events-none absolute top-1/2 left-3.5 h-5 w-5 -translate-y-1/2 text-ink-muted"
            strokeWidth={1.75}
            aria-hidden
          />
          <input
            type="search"
            value={searchDraft}
            onChange={(e) => setSearchDraft(e.target.value)}
            onBlur={applySearch}
            placeholder="Найти блюдо или ингредиент"
            className="h-11 w-full rounded-control border border-line bg-surface pr-4 pl-11 text-ink placeholder:text-ink-muted focus:border-action"
          />
        </label>
        <Button type="submit" variant="primary">
          Найти
        </Button>
      </form>

      <div className="mt-6 items-start gap-8 md:flex">
        <aside className="hidden w-[280px] shrink-0 md:block">{sidebar}</aside>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <AppliedFilterChips
              filters={filters}
              categories={categories ?? []}
              tags={tags ?? []}
              onToggle={toggle}
              onClearText={() => setText('')}
              onToggleMinRating={toggleMinRating}
            />
            <label className="flex w-full items-center gap-2 text-sm text-ink-secondary sm:ml-auto sm:w-auto">
              <span className="shrink-0">Сортировка</span>
              {/* Текст самого селекта прозрачный: поверх лежит бегущая строка
                  (MarqueeText) — иначе длинный пункт не влезает на мобильном. */}
              <span className="relative min-w-0 flex-1 sm:flex-none">
                <select
                  value={filters.sortBy}
                  onChange={(e) => setSortBy(e.target.value as DishSearchSortBy)}
                  className="h-10 w-full cursor-pointer appearance-none rounded-control border border-line bg-surface pl-3 pr-9 text-[15px] text-transparent focus:border-action sm:w-auto"
                >
                  {Object.entries(sortLabels).map(([value, label]) => (
                    <option key={value} value={value} className="text-ink">
                      {label}
                    </option>
                  ))}
                </select>
                <span className="pointer-events-none absolute inset-y-0 left-3 right-9 flex items-center text-[15px] text-ink">
                  <MarqueeText text={sortLabels[filters.sortBy]} className="w-full" />
                </span>
                <ChevronDown
                  className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-secondary"
                  strokeWidth={1.75}
                  aria-hidden
                />
              </span>
            </label>
          </div>

          {isInitialLoading && (
            <div className="mt-6 grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3">
              {Array.from({ length: 6 }, (_, i) => (
                <DishCardSkeleton key={i} />
              ))}
            </div>
          )}

          {isError && (
            <p className="mt-6 rounded-card bg-surface p-8 text-center text-ink-secondary shadow-card">
              Не получилось загрузить каталог. Обновите страницу или зайдите чуть позже.
            </p>
          )}

          {showEmpty && (
            <div className="mt-6">
              <EmptyState
                title="По этим фильтрам ничего нет"
                text="Попробуйте убрать часть условий — вкусное точно найдётся."
                action={
                  hasActiveFilters && (
                    <Button variant="secondary" onClick={reset}>
                      Сбросить фильтры
                    </Button>
                  )
                }
              />
            </div>
          )}

          {!isInitialLoading && items.length > 0 && (
            <>
              <div className="mt-6 grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3">
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
      </div>

      <MobileFiltersDrawer open={drawerOpen} totalCount={totalCount} onClose={() => setDrawerOpen(false)}>
        {sidebar}
      </MobileFiltersDrawer>
    </section>
  );
}
