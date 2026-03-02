namespace GastronomePlatform.Common.Domain.Primitives
{
    /// <summary>
    /// Базовый класс для всех объектов-значений (Value Objects).
    /// Объекты-значения неизменяемы и определяются исключительно набором своих атрибутов.
    /// Два ValueObject равны, если все их атомарные значения равны
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// Возвращает атомарное значение, определяющее равенство.
        /// Каждый наследник перечисляет все свои поля в том порядке,
        /// в котором они должны участвовать в сравнении.
        /// </summary>
        protected abstract IEnumerable<object> GetAtomicValues();

        #region Equality Members

        /// <summary>
        /// Сравнивает два ValueObject по их атомарным значениям.
        /// </summary>
        /// <param name="other">Другой объект-значение для сравнения</param>
        public bool Equals(ValueObject? other)
        {
            // Если другой объект null - false
            if (other is null)
                return false;

            // Если ссылки совпадают — true (оптимизация)
            if (ReferenceEquals(this, other)) 
                return true;

            // Если типы не совпадают - false
            if (GetType() != other.GetType()) 
                return false;

            // Получаем последовательности атомарных значений для обоих объектов
            IEnumerable<object> thisValues = GetAtomicValues();
            IEnumerable<object> otherValues = other.GetAtomicValues();

            // Сравниваем последовательности поэлементно
            return thisValues.SequenceEqual(otherValues);
        }

        /// <summary>
        /// Переопределение Equals для object.
        /// Делегирует к типизированному Equals(ValueObject?).
        /// </summary>
        /// <param name="obj">Объект для сравнения</param>
        public override bool Equals(object? obj)
        {
            return obj is ValueObject other && Equals(other);
        }

        /// <summary>
        /// Вычисляет хэш-код на основе всех атомарных значений.
        /// </summary>
        public override int GetHashCode()
        {
            // Используем HashCode.Combine для всех значений
            // Но поскольку количество значений неизвестно на этапе компиляции,
            // используем агрегацию через цикл

            HashCode hashCode = new();

            foreach(object value in GetAtomicValues())
            {
                // Добавляем каждое значение в комбинатор хэшей
                // Если значение null, добавляем 0 (чтобы не было исключения)
                hashCode.Add(value ?? 0);
            }

            return hashCode.ToHashCode();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Оператор равенства.
        /// </summary>
        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            // Обработка null
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            // Делегируем Equals
            return left.Equals(right);
        }

        /// <summary>
        /// Оператор неравенства.
        /// </summary>
        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }

        #endregion

        /// <summary>
        /// Возвращает строковое представление объекта-значения для отладки.
        /// Формат: "ИмяТипа [значение1, значение2, ...]"
        /// </summary>
        public override string ToString()
        {
            // Собираем значения в читаемую строку для отладки
            string values = string.Join(", ", GetAtomicValues().Select(v => v?.ToString() ?? "null"));
            return $"{GetType().Name} [{values}]";
        }
    }
}
