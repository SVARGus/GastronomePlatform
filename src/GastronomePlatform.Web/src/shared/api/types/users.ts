/**
 * Типы контракта модуля Users (ручное повторение DTO backend, как в dishes.ts).
 */

/** Профиль пользователя (GET /api/users/{id}, анонимно; GET /api/users/me). */
export interface UserProfileDto {
  userId: string;
  email: string;
  userName: string;
  isPublic: boolean;
  phone: string | null;
  firstName: string | null;
  lastName: string | null;
  middleName: string | null;
  displayName: string | null;
  bio: string | null;
  gender: string | null;
  dateOfBirth: string | null;
  avatarMediaId: string | null;
  country: string | null;
  region: string | null;
  city: string | null;
  createdAt: string;
}

/**
 * Публичный профиль (GET /api/users/{id}, анонимно) — урезанный контракт:
 * контактных и чувствительных данных нет никогда; bio и местоположение
 * заполнены только при isPublic.
 */
export interface PublicUserProfileDto {
  userId: string;
  userName: string;
  displayName: string | null;
  avatarMediaId: string | null;
  isPublic: boolean;
  bio: string | null;
  country: string | null;
  region: string | null;
  city: string | null;
  createdAt: string;
}

/** Имя для витрины: displayName, иначе никнейм (подходит обоим DTO профиля). */
export function userDisplayName(profile: { displayName: string | null; userName: string }): string {
  return profile.displayName ?? profile.userName;
}

export type Gender = 'Male' | 'Female' | 'Other' | 'PreferNotToSay';

/** Тело PUT /api/users/me/personal-info. null — очистить поле. */
export interface UpdatePersonalInfoRequest {
  firstName: string | null;
  lastName: string | null;
  middleName: string | null;
  displayName: string | null;
  bio: string | null;
  gender: Gender | null;
  dateOfBirth: string | null;
}

/** Тело PUT /api/users/me/location. */
export interface UpdateLocationRequest {
  country: string | null;
  region: string | null;
  city: string | null;
}
