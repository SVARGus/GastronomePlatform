namespace GastronomePlatform.Common.Domain.Results
{
    /// <summary>
    /// Представляет доменную ошибку.
    /// Содержит код ошибки и описание, не привязанные к конкретному транспорту (HTTP, gRPC и т.д.).
    /// Маппинг на HTTP-коды и ProblemDetails выполняется в слое API.
    /// </summary>
    public sealed class Error : IEquatable<Error>
    {
        /// <summary>
        /// Внутренний код ошибки в формате "ДОМЕН.КАТЕГОРИЯ".
        /// Примеры: "USER.NOT_FOUND", "ORDER.INVALID_STATE", "VALIDATION.ERROR"
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Человекочитаемое описание ошибки.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Тип ошибки для классификации на уровне API.
        /// </summary>
        public ErrorType Type { get; }

        /// <summary>
        /// Создаёт экземпляр доменной ошибки.
        /// </summary>
        /// <param name="code">Код ошибки в формате "ДОМЕН.КАТЕГОРИЯ"</param>
        /// <param name="message">Описание ошибки</param>
        /// <param name="type">Тип ошибки</param>
        public Error(string code, string message, ErrorType type = ErrorType.Failure)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Type = type;
        }

        /// <summary>
        /// Представляет отсутствие ошибки (для успешных результатов).
        /// </summary>
        public static readonly Error None = new(string.Empty, string.Empty);

        #region Фабричные методы для типичных категорий ошибок

        /// <summary>
        /// Ресурс не найден.
        /// Пример: Error.NotFound("USER.NOT_FOUND", "Пользователь не найден")
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Описание ошибки</param>
        public static Error NotFound(string code, string message) =>
            new(code, message, ErrorType.NotFound);

        /// <summary>
        /// Ошибка валидации входных данных.
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Описание ошибки</param>
        public static Error Validation(string code, string message) =>
            new(code, message, ErrorType.Validation);

        /// <summary>
        /// Конфликт (ресурс уже существует, состояние не позволяет операцию).
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Описание ошибки</param>
        public static Error Conflict(string code, string message) =>
            new(code, message, ErrorType.Conflict);

        /// <summary>
        /// Недостаточно прав для выполнения операции.
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Описание ошибки</param>
        public static Error Forbidden(string code, string message) =>
            new(code, message, ErrorType.Forbidden);

        /// <summary>
        /// Общая ошибка бизнес-логики.
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Описание ошибки</param>
        public static Error Failure(string code, string message) =>
            new(code, message, ErrorType.Failure);

        #endregion

        #region Equality Members

        /// <summary>
        /// Сравнивает ошибки по коду и типу.
        /// </summary>
        /// <param name="other">Другая ошибка для сравнения</param>
        public bool Equals(Error? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Code == other.Code && Type == other.Type;
        }

        /// <summary>
        /// Переопределение Equals для object.
        /// </summary>
        /// <param name="obj">Объект для сравнения</param>
        public override bool Equals(object? obj) => Equals(obj as Error);

        /// <summary>
        /// Вычисляет хэш-код на основе кода и типа ошибки.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(Code, Type);

        public static bool operator ==(Error? left, Error? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Error? left, Error? right) => !(left == right);

        #endregion

        /// <summary>
        /// Строковое представление ошибки для логирования и отладки.
        /// Формат: "КОД (ТИП): Сообщение"
        /// </summary>
        public override string ToString() => $"{Code} ({Type}): {Message}";
    }
}
