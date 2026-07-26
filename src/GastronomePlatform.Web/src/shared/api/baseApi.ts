import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

/**
 * Базовый слой обращений к HTTP API (ADR-0018 §3 п. 5): единственная точка,
 * через которую интерфейс общается с backend. Конкретные эндпоинты
 * подключаются через injectEndpoints в своих модулях (features).
 *
 * Базовый адрес берётся из конфигурации (VITE_API_URL), не из кода:
 * в dev это «/api» через прокси Vite, в проде — тот же origin.
 * Заголовок Authorization появится вместе со слоем аутентификации.
 */
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_URL ?? '/api',
  }),
  tagTypes: ['Dishes', 'Categories', 'Tags', 'Profile', 'Subscription', 'Plans'],
  endpoints: () => ({}),
});
