/** Построение URL медиафайлов (модуль Media). База — из конфигурации, как и у API-слоя. */

const API_BASE = import.meta.env.VITE_API_URL ?? '/api';

/** URL миниатюры изображения — для карточек и превью. */
export function mediaThumbnailUrl(mediaId: string): string {
  return `${API_BASE}/media/${mediaId}/thumbnail`;
}

/** URL оригинального файла — для крупных фото. */
export function mediaFileUrl(mediaId: string): string {
  return `${API_BASE}/media/${mediaId}`;
}
