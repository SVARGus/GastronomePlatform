import { baseApi } from '../../shared/api/baseApi';
import { toQueryString } from '../../shared/api/query';
import type {
  CategoryNodeDto,
  CreateDishDraftRequest,
  CreateDishDraftResult,
  DishDetailDto,
  DishRecipeDto,
  DishStatus,
  GetDishesByAuthorResult,
  GetMyDraftsResult,
  GetScaledRecipeIngredientsResult,
  IngredientDto,
  MeasureUnitDto,
  SearchDishesParams,
  SearchDishesResult,
  TagDto,
  UpdateDishCardRequest,
} from '../../shared/api/types/dishes';

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
    dishRecipe: build.query<DishRecipeDto, string>({
      query: (dishId) => ({ url: `dishes/${dishId}/recipe` }),
      providesTags: (_result, _error, dishId) => [{ type: 'Dishes', id: `recipe-${dishId}` }],
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
    /** Карточка блюда по id (UC-DSH-050): автору отдаётся рабочая версия — источник редактора. */
    dishById: build.query<DishDetailDto, string>({
      query: (id) => ({ url: `dishes/${id}` }),
      providesTags: (_result, _error, id) => [{ type: 'Dishes', id }],
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
} = dishesApi;
