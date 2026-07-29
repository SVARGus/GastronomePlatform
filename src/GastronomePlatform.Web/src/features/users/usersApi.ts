import { baseApi } from '../../shared/api/baseApi';
import type {
  UpdateLocationRequest,
  UpdatePersonalInfoRequest,
  UserProfileDto,
} from '../../shared/api/types/users';

/** Эндпоинты модуля Users: профиль автора на витрине + кабинет (просмотр и правки). */
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
    /** Персональные данные (ФИО, отображаемое имя, о себе, пол, дата рождения). */
    updatePersonalInfo: build.mutation<void, UpdatePersonalInfoRequest>({
      query: (body) => ({ url: 'users/me/personal-info', method: 'PUT', body }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Местоположение (страна/регион/город). */
    updateLocation: build.mutation<void, UpdateLocationRequest>({
      query: (body) => ({ url: 'users/me/location', method: 'PUT', body }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Аватар: id медиафайла из Media; null — удалить. */
    updateAvatar: build.mutation<void, string | null>({
      query: (avatarMediaId) => ({ url: 'users/me/avatar', method: 'PUT', body: { avatarMediaId } }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Видимость профиля. */
    setVisibility: build.mutation<void, boolean>({
      query: (isPublic) => ({ url: 'users/me/visibility', method: 'PUT', body: { isPublic } }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Смена email (уникальность — 409 AUTH.EMAIL_TAKEN). */
    changeEmail: build.mutation<void, string>({
      query: (newEmail) => ({ url: 'users/me/email', method: 'PUT', body: { newEmail } }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Смена телефона (409 AUTH.PHONE_TAKEN). */
    changePhone: build.mutation<void, string>({
      query: (newPhone) => ({ url: 'users/me/phone', method: 'PUT', body: { newPhone } }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
    /** Смена никнейма (409 AUTH.USERNAME_TAKEN). */
    changeUserName: build.mutation<void, string>({
      query: (newUserName) => ({ url: 'users/me/username', method: 'PUT', body: { newUserName } }),
      invalidatesTags: [{ type: 'Profile', id: 'me' }],
    }),
  }),
});

export const {
  useUserProfileQuery,
  useMyProfileQuery,
  useUpdatePersonalInfoMutation,
  useUpdateLocationMutation,
  useUpdateAvatarMutation,
  useSetVisibilityMutation,
  useChangeEmailMutation,
  useChangePhoneMutation,
  useChangeUserNameMutation,
} = usersApi;
