using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GastronomePlatform.Common.Application.Behaviors;
using GastronomePlatform.Common.Domain.Results;
using MediatR;
using Moq;

namespace GastronomePlatform.Common.UnitTests.Application
{
    /// <summary>
    /// Тестовая команда без значения.
    /// </summary>
    public sealed record TestCommand(string Value) : IRequest<Result>;

    /// <summary>
    /// Тестовая команда с возвращаемым значением.
    /// </summary>
    public sealed record TestCommandWithValue(string Value) : IRequest<Result<string>>;

    /// <summary>
    /// Тесты для <see cref="ValidationBehavior{TRequest, TResponse}"/>.
    /// </summary>
    public sealed class ValidationBehaviorTests
    {
        #region Без валидаторов

        [Fact]
        public async Task Handle_WhenNoValidators_ShouldCallNextAsync()
        {
            // Arrange
            ValidationBehavior<TestCommand, Result> behavior = new([]);
            TestCommand command = new("test");
            bool nextCalled = false;

            RequestHandlerDelegate<Result> next = (ct) =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success());
            };

            // Act
            Result result = await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Валидация прошла успешно

        [Fact]
        public async Task Handle_WhenValidationPasses_ShouldCallNextAsync()
        {
            // Arrange
            Mock<IValidator<TestCommand>> validatorMock = new();
            validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult()); // пустой результат = нет ошибок

            ValidationBehavior<TestCommand, Result> behavior = new([validatorMock.Object]);
            TestCommand command = new("valid value");
            bool nextCalled = false;

            RequestHandlerDelegate<Result> next = (ct) =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success());
            };

            // Act
            Result result = await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenValidationPasses_ForResultWithValue_ShouldCallNextAsync()
        {
            // Arrange
            Mock<IValidator<TestCommandWithValue>> validatorMock = new();
            validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommandWithValue>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            ValidationBehavior<TestCommandWithValue, Result<string>> behavior = new([validatorMock.Object]);
            TestCommandWithValue command = new("valid");

            RequestHandlerDelegate<Result<string>> next = (ct) =>
                Task.FromResult(Result<string>.Success("ответ"));

            // Act
            Result<string> result = await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be("ответ");
        }

        #endregion

        #region Валидация не прошла — Result

        [Fact]
        public async Task Handle_WhenValidationFails_ShouldNotCallNextAsync()
        {
            // Arrange
            ValidationFailure failure = new("Value", "Поле обязательно.");
            Mock<IValidator<TestCommand>> validatorMock = new();
            validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([failure]));

            ValidationBehavior<TestCommand, Result> behavior = new([validatorMock.Object]);
            TestCommand command = new("");
            bool nextCalled = false;

            RequestHandlerDelegate<Result> next = (ct) =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success());
            };

            // Act
            Result result = await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Be("VALIDATION.ERROR");
        }

        [Fact]
        public async Task Handle_WhenValidationFails_ShouldReturnValidationErrorMessageAsync()
        {
            // Arrange
            ValidationFailure failure = new("Value", "Поле обязательно.");
            Mock<IValidator<TestCommand>> validatorMock = new();
            validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([failure]));

            ValidationBehavior<TestCommand, Result> behavior = new([validatorMock.Object]);

            // Act
            Result result = await behavior.Handle(
                new TestCommand(""),
                (ct) => Task.FromResult(Result.Success()),
                CancellationToken.None);

            // Assert
            result.Error.Message.Should().Contain("Поле обязательно.");
        }

        #endregion

        #region Валидация не прошла — Result<T>

        [Fact]
        public async Task Handle_WhenValidationFails_ForResultWithValue_ShouldNotCallNextAsync()
        {
            // Arrange
            ValidationFailure failure = new("Value", "Значение некорректно.");
            Mock<IValidator<TestCommandWithValue>> validatorMock = new();
            validatorMock
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommandWithValue>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([failure]));

            ValidationBehavior<TestCommandWithValue, Result<string>> behavior = new([validatorMock.Object]);
            bool nextCalled = false;

            RequestHandlerDelegate<Result<string>> next = (ct) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<string>.Success("ответ"));
            };

            // Act
            Result<string> result = await behavior.Handle(
                new TestCommandWithValue(""),
                next,
                CancellationToken.None);

            // Assert
            nextCalled.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Type.Should().Be(ErrorType.Validation);
        }

        #endregion

        #region Несколько валидаторов

        [Fact]
        public async Task Handle_WithMultipleValidators_ShouldCombineErrorsAsync()
        {
            // Arrange
            Mock<IValidator<TestCommand>> validator1 = new();
            validator1
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Value", "Ошибка 1.")]));

            Mock<IValidator<TestCommand>> validator2 = new();
            validator2
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Value", "Ошибка 2.")]));

            ValidationBehavior<TestCommand, Result> behavior =
                new([validator1.Object, validator2.Object]);

            // Act
            Result result = await behavior.Handle(
                new TestCommand(""),
                (ct) => Task.FromResult(Result.Success()),
                CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Message.Should().Contain("Ошибка 1.");
            result.Error.Message.Should().Contain("Ошибка 2.");
        }

        [Fact]
        public async Task Handle_WithDuplicateErrors_ShouldDeduplicateMessagesAsync()
        {
            // Arrange — оба валидатора возвращают одинаковое сообщение
            Mock<IValidator<TestCommand>> validator1 = new();
            validator1
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Value", "Одинаковая ошибка.")]));

            Mock<IValidator<TestCommand>> validator2 = new();
            validator2
                .Setup(v => v.ValidateAsync(
                    It.IsAny<ValidationContext<TestCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Value", "Одинаковая ошибка.")]));

            ValidationBehavior<TestCommand, Result> behavior =
                new([validator1.Object, validator2.Object]);

            // Act
            Result result = await behavior.Handle(
                new TestCommand(""),
                (ct) => Task.FromResult(Result.Success()),
                CancellationToken.None);

            // Assert — сообщение встречается один раз (Distinct)
            result.Error.Message.Should().Be("Одинаковая ошибка.");
        }

        #endregion
    }
}
