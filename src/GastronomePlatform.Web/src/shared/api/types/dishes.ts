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
  /** Владельцу/admin: в рабочем слое есть правки, не попавшие на витрину. Остальным null. */
  hasUnsavedChanges: boolean | null;
}

/** Постраничный результат поиска (UC-DSH-054). */
export interface SearchDishesResult {
  items: DishCardListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Постраничный список публичных блюд автора (UC-DSH-055) — та же карточка. */
export interface GetDishesByAuthorResult {
  items: DishCardListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Превью непубличного блюда в «Моих блюдах» (UC-DSH-053) — без рейтинга/просмотров. */
export interface DishDraftListItemDto {
  id: string;
  slug: string;
  name: string;
  shortDescription: string | null;
  mainImageId: string | null;
  difficultyLevel: DifficultyLevel;
  costEstimate: CostEstimate;
  dietLabelsMask: DietLabelsMask;
  allergensMask: string;
  hasUnverifiedAllergens: boolean;
  createdAt: string;
  updatedAt: string;
}

/** Постраничный результат UC-DSH-053 (черновики либо снятые — по параметру status). */
export interface GetMyDraftsResult {
  items: DishDraftListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Тело POST /api/dishes (UC-DSH-001) — создание черновика. */
export interface CreateDishDraftRequest {
  name: string;
  difficultyLevel: DifficultyLevel;
  costEstimate: CostEstimate;
  shortDescription: string | null;
  description: string | null;
  dietLabelsMask: string | null;
  historyText: string | null;
}

/** Результат создания черновика: id для редактора, slug для будущей витрины. */
export interface CreateDishDraftResult {
  id: string;
  slug: string;
}

/** Тело PATCH /api/dishes/{id} (UC-DSH-002) — публичная карточка без диет/истории/фото. */
export interface UpdateDishCardRequest {
  name: string;
  difficultyLevel: DifficultyLevel;
  costEstimate: CostEstimate;
  shortDescription: string | null;
  description: string | null;
}

/** Тело PUT /api/dishes/{id}/recipe (UC-DSH-011) — общие поля рецепта, полная замена. */
export interface UpdateRecipeRequest {
  introductionText: string | null;
  servingsDefault: number;
  isAlcoholic: boolean;
  authorTips: string | null;
  servingSuggestions: string | null;
  notes: string | null;
}

/**
 * Тело PUT /api/dishes/{id}/recipe/timing (UC-DSH-040). При isTotalManual=false
 * сервер сам считает Total = Prep + Cook + Rest, присланное totalTimeMinutes игнорирует.
 */
export interface SetTimingRequest {
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  restTimeMinutes: number | null;
  activeTimeMinutes: number | null;
  totalTimeMinutes: number;
  isTotalManual: boolean;
}

/** Тело PUT /api/dishes/{id}/recipe/yield (UC-DSH-041). */
export interface SetYieldRequest {
  quantityTotal: number;
  yieldUnit: YieldUnit;
  servingsCount: number;
  gramsPerServing: number | null;
}

/** Тело PUT /api/dishes/{id}/recipe/nutrition (UC-DSH-042): SaturatedFats ≤ Fats, Sugar ≤ Carbs. */
export interface SetNutritionRequest {
  calcMethod: NutritionCalcMethod;
  calories: number;
  proteins: number;
  fats: number;
  saturatedFats: number | null;
  carbs: number;
  sugar: number | null;
  fiber: number | null;
  salt: number | null;
}

/** Тело POST/PUT шага рецепта (UC-DSH-020/021): описание 10–4000, °C −30…300, таймер 1–1440. */
export interface RecipeStepRequest {
  description: string;
  title: string | null;
  imageMediaId: string | null;
  videoUrl: string | null;
  temperatureCelsius: number | null;
  timerMinutes: number | null;
}

/** Тело POST …/recipe/ingredients/catalog (UC-DSH-030): позиция из справочника. */
export interface AddCatalogIngredientRequest {
  ingredientId: string;
  ingredientSpecId: string | null;
  quantity: number;
  measureUnitId: string;
  isOptional: boolean;
  preparationNote: string | null;
}

/** Тело POST …/recipe/ingredients/freeform (UC-DSH-030): свободный текст 1–200. */
export interface AddFreeformIngredientRequest {
  freeformText: string;
  quantity: number;
  measureUnitId: string;
  isOptional: boolean;
  preparationNote: string | null;
}

/** Тело PUT …/recipe/ingredients/{id} (UC-DSH-031): ровно один из ingredientId/freeformText. */
export interface UpdateRecipeIngredientRequest {
  ingredientId: string | null;
  ingredientSpecId: string | null;
  freeformText: string | null;
  quantity: number;
  measureUnitId: string;
  isOptional: boolean;
  preparationNote: string | null;
}

/** Результат 201 Created для шага/позиции рецепта. */
export interface CreatedIdResult {
  id: string;
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

export type DishStatus = 'Draft' | 'Published' | 'Unpublished' | 'Archived';
export type OwnerType = 'User' | 'Chef' | 'Restaurant' | 'Brand';
export type YieldUnit = 'Grams' | 'Kilograms' | 'Milliliters' | 'Liters' | 'Pieces' | 'Servings';
export type NutritionCalcMethod = 'Per100g' | 'PerServing';
export type MeasureUnitType = 'Mass' | 'Volume' | 'Count' | 'Pinch';

/** Публичная карточка блюда (UC-DSH-050/051). Рецепта внутри нет — он за гейтом UC-DSH-052. */
export interface DishDetailDto {
  id: string;
  authorUserId: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  description: string | null;
  historyText: string | null;
  mainImageId: string | null;
  status: DishStatus;
  difficultyLevel: DifficultyLevel;
  costEstimate: CostEstimate;
  ownerType: OwnerType;
  dietLabelsMask: DietLabelsMask;
  allergensMask: string;
  hasUnverifiedAllergens: boolean;
  categoryIds: string[];
  tagNames: string[];
  ratingAvg: number;
  ratingCount: number;
  viewsCount: number;
  favoritesCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
  isPublishedVersion: boolean;
  hasUnsavedChanges: boolean | null;
}

/** Времена этапов приготовления (UC-DSH-052). Обязателен только totalTimeMinutes. */
export interface TimingViewDto {
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  restTimeMinutes: number | null;
  activeTimeMinutes: number | null;
  totalTimeMinutes: number;
  isTotalManual: boolean;
}

/** Выход готового продукта и размер порции (UC-DSH-052). */
export interface YieldViewDto {
  quantityTotal: number;
  yieldUnit: YieldUnit;
  servingsCount: number;
  gramsPerServing: number | null;
}

/** Пищевая ценность (UC-DSH-052). calcMethod определяет подпись «на 100 г» / «на порцию». */
export interface NutritionViewDto {
  calcMethod: NutritionCalcMethod;
  calories: number;
  proteins: number;
  fats: number;
  saturatedFats: number | null;
  carbs: number;
  sugar: number | null;
  fiber: number | null;
  salt: number | null;
}

/** Шаг рецепта (UC-DSH-052), упорядочен по order. */
export interface RecipeStepViewDto {
  id: string;
  order: number;
  title: string | null;
  description: string;
  imageMediaId: string | null;
  videoUrl: string | null;
  temperatureCelsius: number | null;
  timerMinutes: number | null;
}

/** Общие поля позиции рецепта; природа — по дискриминатору type. */
interface RecipeIngredientViewBase {
  id: string;
  order: number;
  quantity: number;
  measureUnitId: string;
  isOptional: boolean;
  preparationNote: string | null;
}

/** Позиция рецепта из справочника ингредиентов (type: "catalog"). */
export interface CatalogRecipeIngredientViewDto extends RecipeIngredientViewBase {
  type: 'catalog';
  ingredientId: string;
  ingredientSpecId: string | null;
}

/** Позиция рецепта свободным текстом (type: "freeform"). */
export interface FreeformRecipeIngredientViewDto extends RecipeIngredientViewBase {
  type: 'freeform';
  freeformText: string;
}

/** Discriminated union позиций рецепта (ADR-0012/0014, поле type). */
export type RecipeIngredientViewDto = CatalogRecipeIngredientViewDto | FreeformRecipeIngredientViewDto;

/** Рецепт со всеми вложенными сущностями (UC-DSH-052). */
export interface RecipeViewDto {
  introductionText: string | null;
  servingsDefault: number;
  isAlcoholic: boolean;
  authorTips: string | null;
  servingSuggestions: string | null;
  notes: string | null;
  timing: TimingViewDto;
  yield: YieldViewDto;
  nutrition: NutritionViewDto | null;
  steps: RecipeStepViewDto[];
  ingredients: RecipeIngredientViewDto[];
}

/** Обёртка рецепта с метаданными слоя-источника (UC-DSH-052). */
export interface DishRecipeDto {
  dishId: string;
  isPublishedVersion: boolean;
  hasUnsavedChanges: boolean | null;
  recipe: RecipeViewDto;
}

/** Позиция с пересчитанным количеством (UC-DSH-056) — плоский DTO, type: "catalog"|"freeform". */
export interface ScaledIngredientDto {
  id: string;
  order: number;
  type: 'catalog' | 'freeform';
  ingredientId: string | null;
  ingredientSpecId: string | null;
  freeformText: string | null;
  originalQuantity: number;
  scaledQuantity: number;
  measureUnitId: string;
  isOptional: boolean;
  preparationNote: string | null;
}

/** Результат пересчёта ингредиентов на N порций (UC-DSH-056). */
export interface GetScaledRecipeIngredientsResult {
  servingsDefault: number;
  servingsRequested: number;
  multiplier: number;
  ingredients: ScaledIngredientDto[];
}

/** Единица измерения (UC-DSH-064) — справочник, кэшируется целиком. */
export interface MeasureUnitDto {
  id: string;
  code: string;
  nameRu: string;
  type: MeasureUnitType;
  conversionToBase: number;
  isBase: boolean;
}

/** Ингредиент справочника (UC-DSH-063) — на карточке блюда нужен ради name. */
export interface IngredientDto {
  id: string;
  name: string;
  pluralName: string | null;
  description: string | null;
  imageMediaId: string | null;
  isLiquid: boolean;
  densityApprox: number | null;
  isAllergen: boolean;
  allergenType: string | null;
  dietConflictsMask: string;
  baseMeasureUnitId: string;
  defaultNutritionId: string | null;
  isActive: boolean;
}
