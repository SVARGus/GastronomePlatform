namespace GastronomePlatform.Common.Domain.Results
{
    /// <summary>
    /// Классификация доменных ошибок.
    /// Используется API-слоем для маппинга на HTTP-коды.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Общая ошибка бизнес-логики (маппится в HTTP 500 или 400).
        /// </summary>
        Failure = 0,

        /// <summary>
        /// Ошибка валидации входных данных (HTTP 400).
        /// </summary>
        Validation = 1,

        /// <summary>
        /// Ресурс не найден (HTTP 404).
        /// </summary>
        NotFound = 2,

        /// <summary>
        /// Конфликт состояния (HTTP 409).
        /// </summary>
        Conflict = 3,

        /// <summary>
        /// Недостаточно прав (HTTP 403).
        /// </summary>
        Forbidden = 4
    }
}
