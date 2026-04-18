using FluentAssertions;
using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Common.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="Result"/> и <see cref="Result{TValue}"/>.
    /// </summary>
    public sealed class ResultTests
    {
        /// <summary>
        /// Тестовый наследник Result с публичным конструктором —
        /// позволяет достучаться до защищённой валидации Result(bool, Error).
        /// </summary>
        private sealed class TestableResult : Result
        {
            public TestableResult(bool isSuccess, Error error) : base(isSuccess, error) { }
        }

        #region Result (без значения)

        [Fact]
        public void Success_ShouldCreateSuccessfulResult()
        {
            // Act
            Result result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().Be(Error.None);
        }

        [Fact]
        public void Failure_WithError_ShouldCreateFailedResult()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Ресурс не найден.");

            // Act
            Result result = Result.Failure(error);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void Failure_WithCodeAndMessage_ShouldCreateFailedResult()
        {
            // Act
            Result result = Result.Failure("TEST.ERROR", "Ошибка.", ErrorType.Validation);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("TEST.ERROR");
            result.Error.Message.Should().Be("Ошибка.");
            result.Error.Type.Should().Be(ErrorType.Validation);
        }

        [Fact]
        public void Failure_WithErrorNone_ShouldThrowArgumentException()
        {
            // Act
            Action action = () => Result.Failure(Error.None);

            // Assert
            action.Should().Throw<ArgumentException>().WithMessage("*meaningful error*");
        }

        [Fact]
        public void Constructor_SuccessWithNonNoneError_ShouldThrowArgumentException()
        {
            // Arrange — инвариант: успешный результат не может нести ошибку
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act
            Action action = () => new TestableResult(true, error);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("*cannot have an error*");
        }

        [Fact]
        public void ImplicitConversion_FromError_ShouldCreateFailedResult()
        {
            // Arrange
            Error error = Error.Conflict("TEST.CONFLICT", "Конфликт.");

            // Act — implicit operator: Error → Result
            Result result = error;

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
        }

        #endregion

        #region Result<TValue>

        [Fact]
        public void Success_WithValue_ShouldCreateSuccessfulResultWithValue()
        {
            // Arrange
            const string VALUE = "тестовое значение";

            // Act
            Result<string> result = Result<string>.Success(VALUE);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(VALUE);
        }

        [Fact]
        public void Failure_WithError_ShouldCreateFailedResultWithoutValue()
        {
            // Arrange
            Error error = Error.NotFound("TEST.NOT_FOUND", "Не найдено.");

            // Act
            Result<string> result = Result<string>.Failure(error);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void Failure_WithCodeAndMessage_ShouldCreateFailedResultWithoutValue()
        {
            // Act
            Result<string> result = Result<string>.Failure("TEST.ERROR", "Ошибка.", ErrorType.Validation);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("TEST.ERROR");
            result.Error.Message.Should().Be("Ошибка.");
            result.Error.Type.Should().Be(ErrorType.Validation);
        }

        [Fact]
        public void Value_WhenResultIsFailure_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Result<string> result = Result<string>.Failure(Error.NotFound("TEST.NOT_FOUND", "Не найдено."));

            // Act
            Action action = () => _ = result.Value;

            // Assert
            action.Should().Throw<InvalidOperationException>().WithMessage("*failed result*");
        }

        [Fact]
        public void ImplicitConversion_FromValue_ShouldCreateSuccessfulResult()
        {
            // Arrange
            const int VALUE = 42;

            // Act — implicit operator: TValue → Result<TValue>
            Result<int> result = VALUE;

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(VALUE);
        }

        [Fact]
        public void ImplicitConversion_FromError_ShouldCreateFailedResultWithoutValue()
        {
            // Arrange
            Error error = Error.Forbidden("TEST.FORBIDDEN", "Доступ запрещён.");

            // Act — implicit operator: Error → Result<TValue>
            Result<string> result = error;

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void Success_WithNullValue_ForResultWithValue_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => Result<string>.Success(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Failure_WithErrorNone_ForResultWithValue_ShouldThrowArgumentException()
        {
            // Act — Result<T>.Failure(Error.None) должен провалиться на проверке
            // в protected base-конструкторе (!isSuccess && error == Error.None)
            Action action = () => Result<string>.Failure(Error.None);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("*meaningful error*");
        }

        #endregion
    }
}
