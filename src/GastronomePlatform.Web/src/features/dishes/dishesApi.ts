import { baseApi } from '../../shared/api/baseApi';
import { toQueryString } from '../../shared/api/query';
import type {
  CategoryNodeDto,
  SearchDishesParams,
  SearchDishesResult,
  TagDto,
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
  }),
});

export const { useSearchDishesQuery, usePopularTagsQuery, useCategoryTreeQuery } = dishesApi;
