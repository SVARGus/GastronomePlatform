import { useSearchParams } from 'react-router-dom';
import type { CostEstimate, DifficultyLevel, DishSearchSortBy } from '../../shared/api/types/dishes';

/** Состояние фильтров каталога. Живёт в URL — фильтры шарятся ссылкой и переживают перезагрузку. */
export interface CatalogFilters {
  text: string;
  categoryIds: string[];
  tagIds: string[];
  /** Значения DietLabels; в запрос уходят маской через запятую. */
  diets: string[];
  difficulties: DifficultyLevel[];
  costs: CostEstimate[];
  minRating: number | null;
  sortBy: DishSearchSortBy;
}

export type ArrayFilterKey = 'categoryIds' | 'tagIds' | 'diets' | 'difficulties' | 'costs';

const SORT_VALUES: DishSearchSortBy[] = ['Newest', 'ViewsDesc', 'RatingDesc'];

/** Чтение и изменение фильтров каталога через query-параметры URL. */
export function useCatalogFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const rawSort = searchParams.get('sortBy') as DishSearchSortBy | null;
  const rawRating = searchParams.get('minRating');

  const filters: CatalogFilters = {
    text: searchParams.get('text') ?? '',
    categoryIds: searchParams.getAll('categoryIds'),
    tagIds: searchParams.getAll('tagIds'),
    diets: searchParams.getAll('diets'),
    difficulties: searchParams.getAll('difficulties') as DifficultyLevel[],
    costs: searchParams.getAll('costs') as CostEstimate[],
    minRating: rawRating !== null && !Number.isNaN(Number(rawRating)) ? Number(rawRating) : null,
    sortBy: rawSort !== null && SORT_VALUES.includes(rawSort) ? rawSort : 'Newest',
  };

  const hasActiveFilters =
    filters.text !== '' ||
    filters.categoryIds.length > 0 ||
    filters.tagIds.length > 0 ||
    filters.diets.length > 0 ||
    filters.difficulties.length > 0 ||
    filters.costs.length > 0 ||
    filters.minRating !== null;

  function update(mutate: (next: URLSearchParams) => void) {
    const next = new URLSearchParams(searchParams);
    mutate(next);
    setSearchParams(next);
  }

  /** Добавляет или снимает значение массивного фильтра. */
  function toggle(key: ArrayFilterKey, value: string) {
    update((next) => {
      const current = next.getAll(key);
      next.delete(key);
      const updated = current.includes(value)
        ? current.filter((v) => v !== value)
        : [...current, value];
      for (const v of updated) next.append(key, v);
    });
  }

  function setText(text: string) {
    update((next) => {
      if (text.trim()) next.set('text', text.trim());
      else next.delete('text');
    });
  }

  function setSortBy(sortBy: DishSearchSortBy) {
    update((next) => {
      if (sortBy === 'Newest') next.delete('sortBy');
      else next.set('sortBy', sortBy);
    });
  }

  function toggleMinRating(value: number) {
    update((next) => {
      if (filters.minRating === value) next.delete('minRating');
      else next.set('minRating', String(value));
    });
  }

  function reset() {
    setSearchParams(new URLSearchParams());
  }

  return { filters, hasActiveFilters, toggle, setText, setSortBy, toggleMinRating, reset };
}
