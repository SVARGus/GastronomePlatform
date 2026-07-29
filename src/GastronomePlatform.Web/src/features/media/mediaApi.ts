import { baseApi } from '../../shared/api/baseApi';

/** Результат загрузки файла (UC-MED-001): id для последующей привязки. */
export interface UploadFileResult {
  mediaId: string;
}

/** Эндпоинты модуля Media, нужные кабинету (загрузка аватара). */
export const mediaApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /**
     * Загрузка файла (UC-MED-001): JPEG/PNG до 10 МБ, multipart.
     * intendedEntityType для аватара — «UserAvatar» (MediaEntityTypes.USER_AVATAR).
     */
    uploadFile: build.mutation<UploadFileResult, { file: File; intendedEntityType: string }>({
      query: ({ file, intendedEntityType }) => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('intendedEntityType', intendedEntityType);
        return { url: 'media/upload', method: 'POST', body: formData };
      },
    }),
  }),
});

export const { useUploadFileMutation } = mediaApi;
