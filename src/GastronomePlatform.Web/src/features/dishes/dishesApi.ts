import { baseApi } from '../../shared/api/baseApi';
import { toQueryString } from '../../shared/api/query';
import type {
  AddCatalogIngredientRequest,
  AddFreeformIngredientRequest,
  CategoryNodeDto,
  CreateDishDraftRequest,
  CreateDishDraftResult,
  CreatedIdResult,
  DishDetailDto,
  DishRecipeDto,
  DishStatus,
  GetDishesByAuthorResult,
  GetMyDraftsResult,
  GetScaledRecipeIngredientsResult,
  IngredientDto,
  MeasureUnitDto,
  RecipeStepRequest,
  SearchDishesParams,
  SearchDishesResult,
  SetNutritionRequest,
  SetTimingRequest,
  SetYieldRequest,
  TagDto,
  UpdateDishCardRequest,
  UpdateRecipeIngredientRequest,
  UpdateRecipeRequest,
} from '../../shared/api/types/dishes';

/**
 * Теги кэша, которые сбрасывает любая мутация рецепта: сам рецепт и карточка
 * блюда — сервер после правок состава пересчитывает маски аллергенов/диет
 * (ADR-0016), а чек-лист готовности к публикации зависит от обоих.
 */
function recipeTags(dishId: string): Array<{ type: 'Dishes'; id: string }> {
  return [
    { type: 'Dishes', id: `recipe-${dishId}` },
    { type: 'Dishes', id: dishId },
  ];
}

/**
 * Эндпоинты модуля Dishes, задействованные на главной и в каталоге.
 * Массивные фильтры сериализуются повторяющимися ключами через toQueryString —
 * стандартная сериализация fetchBaseQuery не подходит контракту.
 */
export const dishesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Каталожный поиск опубликованных блюд (UC-DSH-054). Анонимный. */
    searchDishes: build.query<SearchDishesResult, SearchDishesParams | void>({
      query: (params) => ({ url: `dishes/search${toQueryString({ ...(params ?? {}) })}` }),
      providesTags: ['Dishes'],
    }),
    /** Облако популярных тегов (UC-DSH-061) — чипсы поиска и фильтр тегов. */
    popularTags: build.query<TagDto[], void>({
      query: () => ({ url: 'tags/popular' }),
      providesTags: ['Tags'],
    }),
    /** Дерево активных категорий (UC-DSH-057) — фильтр каталога и подборки. */
    categoryTree: build.query<CategoryNodeDto[], void>({
      query: () => ({ url: 'categories/tree' }),
      providesTags: ['Categories'],
    }),
    /** Публичная карточка блюда по slug (UC-DSH-051). Анонимный, только snapshot. */
    dishBySlug: build.query<DishDetailDto, string>({
      query: (slug) => ({ url: `dishes/by-slug/${encodeURIComponent(slug)}` }),
      providesTags: (_result, _error, slug) => [{ type: 'Dishes', id: slug }],
    }),
    /**
     * Рецепт блюда (UC-DSH-052). Гейт: 401 — гость, 403 DISHES.PREMIUM_REQUIRED —
     * без подписки, 200 — подписчик/автор/admin. Ошибки не ретраятся — это
     * ожидаемые состояния страницы, а не сбои.
     */
    dishRecipe: build.query<DishRecipeDto, { dishId: string; version?: 'working' }>({
      query: ({ dishId, version }) => ({
        url: `dishes/${dishId}/recipe${version ? `?version=${version}` : ''}`,
      }),
      providesTags: (_result, _error, { dishId }) => [{ type: 'Dishes', id: `recipe-${dishId}` }],
    }),
    /** Пересчёт ингредиентов на N порций (UC-DSH-056). Единицы не конвертируются. */
    scaledIngredients: build.query<GetScaledRecipeIngredientsResult, { dishId: string; servings: number }>({
      query: ({ dishId, servings }) => ({ url: `dishes/${dishId}/recipe/scaled?servings=${servings}` }),
    }),
    /** Справочник единиц измерения (UC-DSH-064) — один запрос, живёт в кэше всю сессию. */
    measureUnits: build.query<MeasureUnitDto[], void>({
      query: () => ({ url: 'measure-units' }),
      keepUnusedDataFor: 3600,
    }),
    /** Карточка ингредиента (UC-DSH-063) — ради имени в списке рецепта; batch — бэклог. */
    ingredientById: build.query<IngredientDto, string>({
      query: (id) => ({ url: `ingredients/${id}` }),
      keepUnusedDataFor: 3600,
    }),
    /** Счётчик просмотров (fire-and-forget после открытия карточки). Анонимный. */
    incrementViews: build.mutation<void, string>({
      query: (dishId) => ({ url: `dishes/${dishId}/views`, method: 'POST' }),
    }),
    /** Публичные блюда автора (UC-DSH-055). Анонимный; свежие публикации сверху. */
    dishesByAuthor: build.query<GetDishesByAuthorResult, { authorUserId: string; page?: number; pageSize?: number }>({
      query: ({ authorUserId, page = 1, pageSize = 12 }) => ({
        url: `dishes/by-author/${authorUserId}${toQueryString({ page, pageSize })}`,
      }),
      providesTags: ['Dishes'],
    }),
    /** Непубличные блюда текущего пользователя по статусу (UC-DSH-053). */
    myDrafts: build.query<GetMyDraftsResult, { status: Extract<DishStatus, 'Draft' | 'Unpublished'>; page?: number; pageSize?: number }>({
      query: ({ status, page = 1, pageSize = 25 }) => ({
        url: `dishes/my-drafts${toQueryString({ status, page, pageSize })}`,
      }),
      providesTags: ['Dishes'],
    }),
    /** Публикация блюда (UC-DSH-004). 409 — доменные проверки полноты. */
    publishDish: build.mutation<void, string>({
      query: (dishId) => ({ url: `dishes/${dishId}/publish`, method: 'POST' }),
      invalidatesTags: ['Dishes'],
    }),
    /** Снятие с публикации (UC-DSH-005): блюдо уходит во вкладку «Снятые». */
    unpublishDish: build.mutation<void, string>({
      query: (dishId) => ({ url: `dishes/${dishId}/unpublish`, method: 'POST' }),
      invalidatesTags: ['Dishes'],
    }),
    /** Архивирование (UC-DSH-006): необратимо для автора. */
    archiveDish: build.mutation<void, string>({
      query: (dishId) => ({ url: `dishes/${dishId}/archive`, method: 'POST' }),
      invalidatesTags: ['Dishes'],
    }),
    /**
     * Карточка блюда по id (UC-DSH-050). version: 'working' — рабочая версия
     * даже при опубликованном блюде (только автор/admin) — источник редакторов.
     */
    dishById: build.query<DishDetailDto, { id: string; version?: 'working' }>({
      query: ({ id, version }) => ({ url: `dishes/${id}${version ? `?version=${version}` : ''}` }),
      providesTags: (_result, _error, { id }) => [{ type: 'Dishes', id }],
    }),
    /** Автокомплит тегов (UC-DSH-060) — подсказки в chips-вводе редактора. */
    tagSearch: build.query<TagDto[], string>({
      query: (query) => ({ url: `tags/search${toQueryString({ query })}` }),
    }),
    /** Создание черновика (UC-DSH-001) → id для редактора. */
    createDish: build.mutation<CreateDishDraftResult, CreateDishDraftRequest>({
      query: (body) => ({ url: 'dishes', method: 'POST', body }),
      invalidatesTags: ['Dishes'],
    }),
    /** Публичная карточка (UC-DSH-002): name/difficulty/cost/описания. */
    updateDishCard: build.mutation<void, { dishId: string } & UpdateDishCardRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}`, method: 'PATCH', body }),
      invalidatesTags: (_result, _error, { dishId }) => ['Dishes', { type: 'Dishes', id: dishId }],
    }),
    /** Категории блюда (UC-DSH-007): replace-семантика, ≤3. */
    setDishCategories: build.mutation<void, { dishId: string; categoryIds: string[] }>({
      query: ({ dishId, categoryIds }) => ({
        url: `dishes/${dishId}/categories`,
        method: 'PUT',
        body: { categoryIds },
      }),
      invalidatesTags: (_result, _error, { dishId }) => [{ type: 'Dishes', id: dishId }],
    }),
    /** Теги блюда (UC-DSH-008): ИМЕНА, replace, сервер делает find-or-create, ≤20. */
    setDishTags: build.mutation<void, { dishId: string; tagNames: string[] }>({
      query: ({ dishId, tagNames }) => ({
        url: `dishes/${dishId}/tags`,
        method: 'PUT',
        body: { tagNames },
      }),
      invalidatesTags: (_result, _error, { dishId }) => [{ type: 'Dishes', id: dishId }],
    }),
    /** Диетические метки (UC-DSH-009): маска строкой; «None» — снять все. */
    setDishDietLabels: build.mutation<void, { dishId: string; dietLabelsMask: string }>({
      query: ({ dishId, dietLabelsMask }) => ({
        url: `dishes/${dishId}/diet-labels`,
        method: 'PATCH',
        body: { dietLabelsMask },
      }),
      invalidatesTags: (_result, _error, { dishId }) => [{ type: 'Dishes', id: dishId }],
    }),
    /** История блюда (UC-DSH-010): null — очистить. */
    setDishHistory: build.mutation<void, { dishId: string; historyText: string | null }>({
      query: ({ dishId, historyText }) => ({
        url: `dishes/${dishId}/history`,
        method: 'PATCH',
        body: { historyText },
      }),
      invalidatesTags: (_result, _error, { dishId }) => [{ type: 'Dishes', id: dishId }],
    }),
    /** Главное фото (UC-DSH-011): id медиафайла; null — убрать. */
    changeMainImage: build.mutation<void, { dishId: string; mainImageId: string | null }>({
      query: ({ dishId, mainImageId }) => ({
        url: `dishes/${dishId}/main-image`,
        method: 'PATCH',
        body: { mainImageId },
      }),
      invalidatesTags: (_result, _error, { dishId }) => ['Dishes', { type: 'Dishes', id: dishId }],
    }),
    /** Автокомплит справочника ингредиентов (UC-DSH-062) — ILIKE-префикс, ≤50. */
    ingredientSearch: build.query<IngredientDto[], string>({
      query: (query) => ({ url: `ingredients/search${toQueryString({ query })}` }),
    }),
    /** Общие поля рецепта (UC-DSH-011): полная замена. */
    updateRecipe: build.mutation<void, { dishId: string } & UpdateRecipeRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}/recipe`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Тайминг рецепта (UC-DSH-040). */
    setRecipeTiming: build.mutation<void, { dishId: string } & SetTimingRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}/recipe/timing`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Выход и порции (UC-DSH-041). */
    setRecipeYield: build.mutation<void, { dishId: string } & SetYieldRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}/recipe/yield`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** КБЖУ (UC-DSH-042): создаёт или перезаписывает. */
    setRecipeNutrition: build.mutation<void, { dishId: string } & SetNutritionRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}/recipe/nutrition`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Добавление шага (UC-DSH-020): порядок назначает сервер. */
    addRecipeStep: build.mutation<CreatedIdResult, { dishId: string } & RecipeStepRequest>({
      query: ({ dishId, ...body }) => ({ url: `dishes/${dishId}/recipe/steps`, method: 'POST', body }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Обновление шага (UC-DSH-021): null в опциональных полях — очистить. */
    updateRecipeStep: build.mutation<void, { dishId: string; stepId: string } & RecipeStepRequest>({
      query: ({ dishId, stepId, ...body }) => ({
        url: `dishes/${dishId}/recipe/steps/${stepId}`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Удаление шага (UC-DSH-022): сервер перенумеровывает оставшиеся. */
    removeRecipeStep: build.mutation<void, { dishId: string; stepId: string }>({
      query: ({ dishId, stepId }) => ({
        url: `dishes/${dishId}/recipe/steps/${stepId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Переупорядочивание шагов (UC-DSH-023): полный список id без дублей. */
    reorderRecipeSteps: build.mutation<void, { dishId: string; orderedStepIds: string[] }>({
      query: ({ dishId, orderedStepIds }) => ({
        url: `dishes/${dishId}/recipe/steps/order`,
        method: 'PUT',
        body: { orderedStepIds },
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Позиция из справочника (UC-DSH-030, catalog). */
    addCatalogIngredient: build.mutation<CreatedIdResult, { dishId: string } & AddCatalogIngredientRequest>({
      query: ({ dishId, ...body }) => ({
        url: `dishes/${dishId}/recipe/ingredients/catalog`,
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Позиция свободным текстом (UC-DSH-030, freeform): блюдо получит hasUnverifiedAllergens. */
    addFreeformIngredient: build.mutation<CreatedIdResult, { dishId: string } & AddFreeformIngredientRequest>({
      query: ({ dishId, ...body }) => ({
        url: `dishes/${dishId}/recipe/ingredients/freeform`,
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Обновление позиции (UC-DSH-031): допускает смену природы catalog↔freeform. */
    updateRecipeIngredient: build.mutation<
      void,
      { dishId: string; recipeIngredientId: string } & UpdateRecipeIngredientRequest
    >({
      query: ({ dishId, recipeIngredientId, ...body }) => ({
        url: `dishes/${dishId}/recipe/ingredients/${recipeIngredientId}`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Удаление позиции (UC-DSH-032). */
    removeRecipeIngredient: build.mutation<void, { dishId: string; recipeIngredientId: string }>({
      query: ({ dishId, recipeIngredientId }) => ({
        url: `dishes/${dishId}/recipe/ingredients/${recipeIngredientId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
    /** Переупорядочивание позиций (UC-DSH-033): полный список id без дублей. */
    reorderRecipeIngredients: build.mutation<void, { dishId: string; orderedIngredientIds: string[] }>({
      query: ({ dishId, orderedIngredientIds }) => ({
        url: `dishes/${dishId}/recipe/ingredients/order`,
        method: 'PUT',
        body: { orderedIngredientIds },
      }),
      invalidatesTags: (_result, _error, { dishId }) => recipeTags(dishId),
    }),
  }),
});

export const {
  useSearchDishesQuery,
  usePopularTagsQuery,
  useCategoryTreeQuery,
  useDishBySlugQuery,
  useDishRecipeQuery,
  useScaledIngredientsQuery,
  useMeasureUnitsQuery,
  useIngredientByIdQuery,
  useIncrementViewsMutation,
  useDishesByAuthorQuery,
  useMyDraftsQuery,
  usePublishDishMutation,
  useUnpublishDishMutation,
  useArchiveDishMutation,
  useDishByIdQuery,
  useLazyTagSearchQuery,
  useCreateDishMutation,
  useUpdateDishCardMutation,
  useSetDishCategoriesMutation,
  useSetDishTagsMutation,
  useSetDishDietLabelsMutation,
  useSetDishHistoryMutation,
  useChangeMainImageMutation,
  useLazyIngredientSearchQuery,
  useUpdateRecipeMutation,
  useSetRecipeTimingMutation,
  useSetRecipeYieldMutation,
  useSetRecipeNutritionMutation,
  useAddRecipeStepMutation,
  useUpdateRecipeStepMutation,
  useRemoveRecipeStepMutation,
  useReorderRecipeStepsMutation,
  useAddCatalogIngredientMutation,
  useAddFreeformIngredientMutation,
  useUpdateRecipeIngredientMutation,
  useRemoveRecipeIngredientMutation,
  useReorderRecipeIngredientsMutation,
} = dishesApi;
