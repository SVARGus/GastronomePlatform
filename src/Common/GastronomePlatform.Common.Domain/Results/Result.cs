namespace GastronomePlatform.Common.Domain.Results
{
    /// <summary>
    /// Результат операции без возвращаемого значения.
    /// Используется для команд (CreateDish, DeleteOrder и т.д.).
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Признак успешного завершения операции.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Признак неудачного завершения операции.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        private readonly Error? _error;

        /// <summary>
        /// Возвращает ошибку, если операция завершилась неудачей.
        /// </summary>
        public Error Error => _error ?? Error.None;

        /// <summary>
        /// Защищённый конструктор для создания результата.
        /// </summary>
        /// <param name="isSuccess">Признак успешности операции</param>
        /// <param name="error">Ошибка</param>
        /// <exception cref="ArgumentException">Выбрасывается при несоответствии isSuccess и error</exception>
        protected Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None)
            {
                throw new ArgumentException("Successful result cannot have an error", nameof(error));
            }

            if (!isSuccess && error == Error.None)
            {
                throw new ArgumentException("Failed result must have a meaningful error", nameof(error));
            }

            IsSuccess = isSuccess;
            _error = error;
        }

        /// <summary>
        /// Создает успешный результат.
        /// </summary>
        public static Result Success() => new(true, Error.None);

        /// <summary>
        /// Создает результат с ошибкой.
        /// </summary>
        public static Result Failure(Error error) => new(false, error);

        /// <summary>
        /// Создает результат с ошибкой на основе кода и сообщения.
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="type">Тип ошибки (по умолчанию Failure)</param>
        /// <returns>Результат с ошибкой</returns>
        public static Result Failure(string code, string message, ErrorType type = ErrorType.Failure) =>
            Failure(new Error(code, message, type));

        /// <summary>
        /// Неявное преобразование из Error в Result (удобно для return error).
        /// </summary>
        public static implicit operator Result(Error error) => Failure(error);
    }

    /// <summary>
    /// Результат операции с возвращаемым значением.
    /// Используется для запросов (GetDish, SearchDishes и т.д.).
    /// </summary>
    /// <typeparam name="TValue">Тип возвращаемого значения</typeparam>
    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        /// <summary>
        /// Возвращает значение успешного результата.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Выбрасывается при попытке получить значение неуспешного результата
        /// </exception>
        public TValue Value
        {
            get
            {
                if (IsFailure)
                {
                    throw new InvalidOperationException($"Cannot access value of failed result: {Error}");
                }

                return _value!;
            }
        }

        /// <summary>
        /// Создаёт успешный результат со значением.
        /// </summary>
        /// <param name="value">Значение результата (не может быть null)</param>
        private Result(TValue value) : base(true, Error.None)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Создаёт неуспешный результат с ошибкой.
        /// </summary>
        /// <param name="error">Описание ошибки</param>
        private Result(Error error) : base(false, error)
        {
            _value = default;
        }

        /// <summary>
        /// Создает успешный результат со значением.
        /// </summary>
        public static Result<TValue> Success(TValue value) => new(value);

        /// <summary>
        /// Создает результат с ошибкой.
        /// </summary>
        public static new Result<TValue> Failure(Error error) => new(error);

        /// <summary>
        /// Создает результат с ошибкой на основе кода и сообщения.
        /// </summary>
        /// <param name="code">Код ошибки</param>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="type">Тип ошибки (по умолчанию Failure)</param>
        /// <returns>Результат с ошибкой</returns>
        public static new Result<TValue> Failure(string code, string message, ErrorType type = ErrorType.Failure) =>
            Failure(new Error(code, message, type));

        /// <summary>
        /// Неявное преобразование из значения в успешный результат.
        /// Позволяет писать: return dish;
        /// </summary>
        public static implicit operator Result<TValue>(TValue value) => Success(value);

        /// <summary>
        /// Неявное преобразование из Error в Result&lt;TValue&gt;.
        /// Позволяет писать: return Error.NotFound(...);
        /// </summary>
        public static implicit operator Result<TValue>(Error error) => Failure(error);
    }
}
