using FluentAssertions;
using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Common.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="Error"/>.
    /// </summary>
    public sealed class ErrorTests
    {
        #region Фабричные методы

        [Fact]
        public void NotFound_ShouldCreateErrorWithCorrectType()
        {
            // Act
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Assert
            error.Code.Should().Be("TEST.NOT_FOUND");
            error.Message.Should().Be("Не найдено.");
            error.Type.Should().Be(ErrorType.NotFound);
        }

        [Fact]
        public void Validation_ShouldCreateErrorWithCorrectType()
        {
            // Act
            Error error = Error.Validation("TEST.VALIDATION", "Ошибка валидации.");

            // Assert
            error.Code.Should().Be("TEST.VALIDATION");
            error.Message.Should().Be("Ошибка валидации.");
            error.Type.Should().Be(ErrorType.Validation);
        }

        [Fact]
        public void Conflict_ShouldCreateErrorWithCorrectType()
        {
            // Act
            Error error = Error.Conflict("TEST.CONFLICT", "Конфликт.");

            // Assert
            error.Code.Should().Be("TEST.CONFLICT");
            error.Message.Should().Be("Конфликт.");
            error.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Forbidden_ShouldCreateErrorWithCorrectType()
        {
            // Act
            Error error = Error.Forbidden("TEST.FORBIDDEN", "Запрещено.");

            // Assert
            error.Code.Should().Be("TEST.FORBIDDEN");
            error.Message.Should().Be("Запрещено.");
            error.Type.Should().Be(ErrorType.Forbidden);
        }

        [Fact]
        public void Failure_ShouldCreateErrorWithCorrectType()
        {
            // Act
            Error error = Error.Failure("TEST.FAILURE", "Ошибка.");

            // Assert
            error.Code.Should().Be("TEST.FAILURE");
            error.Message.Should().Be("Ошибка.");
            error.Type.Should().Be(ErrorType.Failure);
        }

        #endregion

        #region Error.None

        [Fact]
        public void None_ShouldHaveEmptyCodeAndMessage()
        {
            // явный Act, можно встроить в Assert для лаконичности
            Error none = Error.None;

            // Assert
            none.Code.Should().BeEmpty();
            none.Message.Should().BeEmpty();
            none.Type.Should().Be(ErrorType.Failure);
        }

        [Fact]
        public void None_ShouldEqualAnotherNone()
        {
            // явный Act, можно встроить в Assert для лаконичности
            Error none1 = Error.None;
            Error none2 = Error.None;

            // Assert
            none1.Should().Be(none2);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_WithSameCodeAndType_ShouldBeEqual()
        {
            // Arrange
            Error error1 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 1.");
            Error error2 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 2.");

            // Assert — Message не участвует в сравнении
            error1.Should().Be(error2);
        }

        [Fact]
        public void Equals_WithDifferentCode_ShouldNotBeEqual()
        {
            // Arrange
            Error error1 = Error.NotFound("TEST.NOT_FOUND", "Сообщение.");
            Error error2 = Error.NotFound("TEST.OTHER", "Сообщение.");

            // Assert
            error1.Should().NotBe(error2);
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldNotBeEqual()
        {
            // Arrange — одинаковый Code, разный ErrorType
            Error error1 = Error.NotFound("TEST.ERROR", "Сообщение.");
            Error error2 = Error.Conflict("TEST.ERROR", "Сообщение.");

            // Assert
            error1.Should().NotBe(error2);
        }

        [Fact]
        public void Equals_WithNull_ShouldNotBeEqual()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act — типизированный Equals(Error?)
            bool result = error.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_WithNull_ShouldBeFalse()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act — перегрузка Equals(object?)
            bool result = error.Equals((object?)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ObjectEquals_WithNonErrorType_ShouldBeFalse()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act — ветка `obj as Error` → null
            bool result = error.Equals("строка");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_WithEqualErrors_ShouldBeTrue()
        {
            // Arrange
            Error error1 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 1.");
            Error error2 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 2.");

            // Assert
            (error1 == error2).Should().BeTrue();
        }

        [Fact]
        public void InequalityOperator_WithDifferentErrors_ShouldBeTrue()
        {
            // Arrange
            Error error1 = Error.NotFound("TEST.NOT_FOUND", "Сообщение.");
            Error error2 = Error.Conflict("TEST.CONFLICT", "Сообщение.");

            // Assert
            (error1 != error2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_BothNull_ShouldBeTrue()
        {
            // Arrange
            Error? left = null;
            Error? right = null;

            // Assert
            (left == right).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_OneNull_ShouldBeFalse()
        {
            // Arrange
            Error? left = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");
            Error? right = null;

            // Assert — проверяем обе стороны
            (left == right).Should().BeFalse();
            (right == left).Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_WithEqualErrors_ShouldBeEqual()
        {
            // Arrange — Message не участвует в сравнении, хэш тоже должен совпасть
            Error error1 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 1.");
            Error error2 = Error.NotFound("TEST.NOT_FOUND", "Сообщение 2.");

            // Assert — контракт: Equals ⇒ одинаковые хэши
            error1.GetHashCode().Should().Be(error2.GetHashCode());
        }

        #endregion

        #region Constructor

        [Fact]
        public void Constructor_WithNullCode_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => new Error(null!, "Сообщение.");

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("code");
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => new Error("TEST.ERROR", null!);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("message");
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act
            string result = error.ToString();

            // Assert
            result.Should().Be("TEST.NOT_FOUND (NotFound): Не найдено.");
        }

        #endregion
    }
}
