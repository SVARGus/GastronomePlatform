/**
 * Разбор ошибок API в формах. Backend отдаёт три формата:
 * доменные ошибки Result — {code, message, type} (ApiController.MapResult),
 * ValidationProblemDetails с картой errors (ключи — PascalCase-имена полей)
 * для FluentValidation/binding, и RFC 7807 с errorCode для необработанных
 * исключений (GlobalExceptionHandlingMiddleware).
 */

interface ApiErrorBody {
  code?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}

function getBody(error: unknown): ApiErrorBody | null {
  if (typeof error === 'object' && error !== null && 'data' in error) {
    const data = (error as { data: unknown }).data;
    if (typeof data === 'object' && data !== null) {
      return data as ApiErrorBody;
    }
  }
  return null;
}

/** Код доменной ошибки («AUTH.EMAIL_TAKEN») или null. */
export function getErrorCode(error: unknown): string | null {
  const body = getBody(error);
  return body?.code ?? body?.errorCode ?? null;
}

/** Первое сообщение валидации по PascalCase-имени поля или null. */
export function getValidationError(error: unknown, field: string): string | null {
  return getBody(error)?.errors?.[field]?.[0] ?? null;
}
