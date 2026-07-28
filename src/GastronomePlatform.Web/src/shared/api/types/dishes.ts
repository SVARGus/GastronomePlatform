/**
 * Типы контракта модуля Dishes — вручную повторяют DTO backend (ADR-0018:
 * дублирование осознанное, компилятор расхождения не поймает — сверять при
 * изменении контракта).
 *
 * Сериализация: свойства camelCase; enum-ы — СТРОКАМИ (JsonStringEnumConverter
 * без naming policy → имена членов как в C#); [Flags]-маски — строкой вида
 * "Vegetarian, GlutenFree" (или "None").
 */

export type DifficultyLevel = 'Easy' | 'Medium' | 'Hard' | 'Pro';
export type CostEstimate = 'Budget' | 'Moderate' | 'Expensive';
export type DishSearchSortBy = 'Newest' | 'RatingDesc' | 'ViewsDesc';

/** Битовая маска диет в строковом виде: "None" | "Vegetarian, Vegan" | ... */
export type DietLabelsMask = string;

/** Превью-карточка блюда в списках каталога (UC-DSH-054/055). */
export interface DishCardListItemDto {
  id: string;
  authorUserId: string;
  slug: string;
  name: string;
  shortDescription: string | null;
  mainImageId: string | null;
  difficultyLevel: DifficultyLevel;
  costEstimate: CostEstimate;
  dietLabelsMask: DietLabelsMask;
  allergensMask: string;
  hasUnverifiedAllergens: boolean;
  ratingAvg: number;
  ratingCount: number;
  viewsCount: number;
  favoritesCount: number;
  publishedAt: string | null;
  createdAt: string;
}

/** Постраничный результат поиска (UC-DSH-054). */
export interface SearchDishesResult {
  items: DishCardListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Параметры GET /api/dishes/search. Все фильтры опциональны. */
export interface SearchDishesParams {
  text?: string;
  categoryIds?: string[];
  tagIds?: string[];
  dietLabelsMask?: string;
  difficulties?: DifficultyLevel[];
  costs?: CostEstimate[];
  minRating?: number;
  sortBy?: DishSearchSortBy;
  page?: number;
  pageSize?: number;
}

/** Тег для чипсов и автокомплита (UC-DSH-060/061). */
export interface TagDto {
  id: string;
  name: string;
  slug: string;
  usageCount: number;
  isVerified: boolean;
}

/** Узел дерева категорий (UC-DSH-057) — рекурсивный. */
export interface CategoryNodeDto {
  id: string;
  name: string;
  slug: string;
  order: number;
  iconMediaId: string | null;
  children: CategoryNodeDto[];
}
