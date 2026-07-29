import { baseApi } from '../../shared/api/baseApi';
import type { UserProfileDto } from '../../shared/api/types/users';

/** Эндпоинты модуля Users, нужные витрине (профиль автора блюда). */
export const usersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Публичный профиль пользователя (анонимный) — имя и аватар автора. */
    userProfile: build.query<UserProfileDto, string>({
      query: (id) => ({ url: `users/${id}` }),
      providesTags: (_result, _error, id) => [{ type: 'Profile', id }],
      keepUnusedDataFor: 600,
    }),
    /** Профиль текущего пользователя — имя и аватар в шапке. Требует токен. */
    myProfile: build.query<UserProfileDto, void>({
      query: () => ({ url: 'users/me' }),
      providesTags: [{ type: 'Profile', id: 'me' }],
    }),
  }),
});

export const { useUserProfileQuery, useMyProfileQuery } = usersApi;
