import { baseApi } from '../../shared/api/baseApi';
import type { SearchDishesParams, SearchDishesResult, TagDto } from '../../shared/api/types/dishes';

/**
 * Эндпоинты модуля Dishes, задействованные на главной и в каталоге.
 * Внимание: fetchBaseQuery сериализует массивы в параметрах через запятую —
 * для массивных фильтров каталога (categoryIds[], difficulties[]) понадобится
 * собственная сериализация повторяющихся ключей; на главной массивы не используются.
 */
export const dishesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Каталожный поиск опубликованных блюд (UC-DSH-054). Анонимный. */
    searchDishes: build.query<SearchDishesResult, SearchDishesParams | void>({
      query: (params) => ({ url: 'dishes/search', params: params ?? undefined }),
      providesTags: ['Dishes'],
    }),
    /** Облако популярных тегов (UC-DSH-061) — чипсы под поиском. */
    popularTags: build.query<TagDto[], void>({
      query: () => ({ url: 'tags/popular' }),
      providesTags: ['Tags'],
    }),
  }),
});

export const { useSearchDishesQuery, usePopularTagsQuery } = dishesApi;
