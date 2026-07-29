import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query';
import { sessionEnded } from './authSlice';
import { clearSession, getAccessToken, getRefreshToken, saveSession } from './authStorage';

/**
 * Базовый слой обращений к HTTP API (ADR-0018 §3 п. 5): единственная точка,
 * через которую интерфейс общается с backend. Конкретные эндпоинты
 * подключаются через injectEndpoints в своих модулях (features).
 *
 * Базовый адрес берётся из конфигурации (VITE_API_URL), не из кода:
 * в dev это «/api» через прокси Vite, в проде — тот же origin.
 */
const rawBaseQuery = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_URL ?? '/api',
  prepareHeaders: (headers) => {
    const token = getAccessToken();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  },
});

/**
 * Общий promise текущей refresh-ротации: параллельные 401 ждут ОДНУ ротацию.
 * Без этого второй конкурентный запрос отправил бы уже отозванный refresh
 * (backend делает Token Rotation) и уронил бы сессию.
 */
let refreshPromise: Promise<boolean> | null = null;

async function refreshSession(
  api: Parameters<BaseQueryFn>[1],
  extraOptions: Parameters<BaseQueryFn>[2],
): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (refreshToken === null) {
    return false;
  }

  const result = await rawBaseQuery(
    { url: 'auth/refresh', method: 'POST', body: { refreshToken } },
    api,
    extraOptions,
  );

  const session = result.data as { accessToken?: string; refreshToken?: string } | undefined;
  if (session?.accessToken && session.refreshToken) {
    saveSession({ accessToken: session.accessToken, refreshToken: session.refreshToken });
    return true;
  }

  // Refresh недействителен или истёк — сессия закончилась.
  clearSession();
  api.dispatch(sessionEnded());
  return false;
}

/**
 * Обёртка над fetchBaseQuery с автоматическим обновлением access-токена:
 * на 401 при наличии refresh-токена выполняется ротация пары и повтор
 * исходного запроса; при провале ротации сессия очищается.
 */
const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  let result = await rawBaseQuery(args, api, extraOptions);

  if (result.error?.status === 401 && getRefreshToken() !== null) {
    refreshPromise ??= refreshSession(api, extraOptions).finally(() => {
      refreshPromise = null;
    });

    const refreshed = await refreshPromise;
    if (refreshed) {
      result = await rawBaseQuery(args, api, extraOptions);
    }
  }

  return result;
};

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Dishes', 'Categories', 'Tags', 'Profile', 'Subscription', 'Plans'],
  endpoints: () => ({}),
});
