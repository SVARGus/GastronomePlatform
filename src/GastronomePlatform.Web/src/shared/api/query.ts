/**
 * Сборка query-строки с ПОВТОРЯЮЩИМИСЯ ключами для массивов
 * (`categoryIds=a&categoryIds=b`) — формат, который ждёт model binding
 * ASP.NET. Стандартная сериализация fetchBaseQuery клеит массивы через
 * запятую и для массивных параметров контракта не подходит.
 */
export function toQueryString(params: Record<string, unknown>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    if (Array.isArray(value)) {
      for (const item of value) search.append(key, String(item));
    } else {
      search.append(key, String(value));
    }
  }

  const qs = search.toString();
  return qs ? `?${qs}` : '';
}
