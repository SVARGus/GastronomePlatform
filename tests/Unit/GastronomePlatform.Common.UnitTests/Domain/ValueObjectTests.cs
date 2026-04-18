using FluentAssertions;
using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Common.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="ValueObject"/>.
    /// </summary>
    public sealed class ValueObjectTests
    {
        /// <summary>
        /// Тестовый ValueObject: деньги (сумма + валюта).
        /// </summary>
        private sealed class TestMoney : ValueObject
        {
            public decimal Amount { get; }
            public string Currency { get; }

            public TestMoney(decimal amount, string currency)
            {
                Amount = amount;
                Currency = currency;
            }

            protected override IEnumerable<object> GetAtomicValues()
            {
                yield return Amount;
                yield return Currency;
            }
        }

        /// <summary>
        /// Другой тип ValueObject — для проверки что разные типы не равны.
        /// </summary>
        private sealed class TestAddress : ValueObject
        {
            public string Country { get; }
            public string City { get; }

            public TestAddress(string country, string city)
            {
                Country = country;
                City = city;
            }

            protected override IEnumerable<object> GetAtomicValues()
            {
                yield return Country;
                yield return City;
            }
        }

        /// <summary>
        /// ValueObject с nullable-полем — нужен для проверки ветки `value ?? 0`
        /// в GetHashCode при наличии null среди атомарных значений.
        /// </summary>
        private sealed class NullableValueObject : ValueObject
        {
            public string? Optional { get; }

            public NullableValueObject(string? optional)
            {
                Optional = optional;
            }

            protected override IEnumerable<object> GetAtomicValues()
            {
                yield return Optional!;
            }
        }

        #region Equals — одинаковые значения

        [Fact]
        public void Equals_WithSameAtomicValues_ShouldBeEqual()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(100m, "RUB");

            // Assert
            money1.Should().Be(money2);
            money1.Equals(money2).Should().BeTrue();
        }

        [Fact]
        public void Equals_WithSameReference_ShouldBeTrue()
        {
            // Arrange
            TestMoney money = new(100m, "RUB");

            // Assert — ReferenceEquals-ветка
            money.Equals(money).Should().BeTrue();
        }

        #endregion

        #region Equals — разные значения

        [Fact]
        public void Equals_WithDifferentAmount_ShouldNotBeEqual()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(200m, "RUB");

            // Assert
            money1.Should().NotBe(money2);
        }

        [Fact]
        public void Equals_WithDifferentCurrency_ShouldNotBeEqual()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(100m, "USD");

            // Assert
            money1.Should().NotBe(money2);
        }

        [Fact]
        public void Equals_WithDifferentValueObjectType_ShouldNotBeEqual()
        {
            // Arrange — атомарные значения условно совпадают, но типы разные
            TestMoney money = new(100m, "RUB");
            TestAddress address = new("100", "RUB");

            // Assert — GetType() != other.GetType() ветка
            money.Equals(address).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithNull_ShouldBeFalse()
        {
            // Arrange
            TestMoney money = new(100m, "RUB");

            // Act — типизированный Equals(ValueObject?)
            bool result = money.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_WithNull_ShouldBeFalse()
        {
            // Arrange
            TestMoney money = new(100m, "RUB");

            // Act — перегрузка Equals(object?)
            bool result = money.Equals((object?)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_WithNonValueObjectType_ShouldBeFalse()
        {
            // Arrange
            TestMoney money = new(100m, "RUB");

            // Act — ветка `obj is ValueObject` → false
            bool result = money.Equals("не-ValueObject");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetHashCode

        [Fact]
        public void GetHashCode_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(100m, "RUB");

            // Assert — контракт: Equals ⇒ одинаковый GetHashCode
            money1.GetHashCode().Should().Be(money2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithNullAtomicValue_ShouldNotThrow()
        {
            // Arrange — в ValueObject.GetHashCode реализовано `value ?? 0`
            NullableValueObject valueObject = new(null);

            // Act
            Func<int> action = () => valueObject.GetHashCode();

            // Assert — null в атомарных значениях не должен ронять хэширование
            action.Should().NotThrow();
        }

        #endregion

        #region Operators

        [Fact]
        public void EqualityOperator_WithSameValues_ShouldBeTrue()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(100m, "RUB");

            // Assert
            (money1 == money2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_WithDifferentValues_ShouldBeFalse()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(200m, "RUB");

            // Assert
            (money1 == money2).Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_BothNull_ShouldBeTrue()
        {
            // Arrange
            TestMoney? left = null;
            TestMoney? right = null;

            // Assert
            (left == right).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_OneNull_ShouldBeFalse()
        {
            // Arrange
            TestMoney? left = new(100m, "RUB");
            TestMoney? right = null;

            // Assert
            (left == right).Should().BeFalse();
            (right == left).Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ShouldBeTrue()
        {
            // Arrange
            TestMoney money1 = new(100m, "RUB");
            TestMoney money2 = new(200m, "RUB");

            // Assert
            (money1 != money2).Should().BeTrue();
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            TestMoney money = new(100m, "RUB");

            // Act
            string result = money.ToString();

            // Assert — формат: "ТипИмя [v1, v2]"
            result.Should().Be("TestMoney [100, RUB]");
        }

        [Fact]
        public void ToString_WithNullAtomicValue_ShouldRenderNullLiteral()
        {
            // Arrange
            NullableValueObject valueObject = new(null);

            // Act
            string result = valueObject.ToString();

            // Assert — null-значения превращаются в литерал "null"
            result.Should().Be("NullableValueObject [null]");
        }

        #endregion
    }
}
