using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Commands.Register;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Events;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using MediatR;
using Moq;

namespace GastronomePlatform.Auth.UnitTests.Application
{
    /// <summary>
    /// Тесты для <see cref="RegisterCommandHandler"/>.
    /// </summary>
    public sealed class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPublisher> _publisherMock = new();
        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _handler = new RegisterCommandHandler(
                _userRepositoryMock.Object,
                _publisherMock.Object);
        }

        /// <summary>
        /// Стандартная валидная команда — все параметры можно переопределить.
        /// </summary>
        private static RegisterCommand CreateValidCommand(
            string email = "test@example.com",
            string userName = "test_user",
            string password = "SecurePass123!",
            string? phone = null)
            => new(email, userName, password, phone);

        /// <summary>
        /// Настраивает моки на успешный сценарий: все проверки уникальности false,
        /// CreateAsync возвращает Success с указанным Id.
        /// </summary>
        private void SetupSuccessfulFlow(Guid createdUserId)
        {
            _userRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.CreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid>.Success(createdUserId));
        }

        #region Constructor

        [Fact]
        public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new RegisterCommandHandler(null!, _publisherMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userRepository");
        }

        [Fact]
        public void Constructor_WithNullPublisher_ShouldThrowArgumentNullException()
        {
            Action action = () => new RegisterCommandHandler(_userRepositoryMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("publisher");
        }

        #endregion

        #region Success

        [Fact]
        public async Task Handle_WithValidData_ReturnsSuccessAndPerformsSideEffectsAsync()
        {
            // Arrange
            Guid createdUserId = Guid.NewGuid();
            SetupSuccessfulFlow(createdUserId);

            RegisterCommand command = CreateValidCommand(
                email: "john@example.com",
                userName: "john",
                password: "StrongP@ss1",
                phone: "+79001234567");

            // Act
            Result result = await _handler.Handle(command, CancellationToken.None);

            // Assert — результат + создание пользователя + публикация события
            using (new AssertionScope())
            {
                result.IsSuccess.Should().BeTrue();
                result.Error.Should().Be(Error.None);

                _userRepositoryMock.Verify(
                    r => r.CreateAsync(
                        "john@example.com",
                        "john",
                        "StrongP@ss1",
                        "+79001234567",
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                _publisherMock.Verify(
                    p => p.Publish(
                        It.Is<UserRegisteredEvent>(e =>
                            e.UserId == createdUserId &&
                            e.Email == "john@example.com" &&
                            e.UserName == "john" &&
                            e.PhoneNumber == "+79001234567"),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_ShouldPassCancellationTokenToRepositoryAsync()
        {
            // Arrange — выделенный сценарий: другой CT в Act
            SetupSuccessfulFlow(Guid.NewGuid());
            using CancellationTokenSource cts = new();
            CancellationToken expectedToken = cts.Token;

            // Act
            await _handler.Handle(CreateValidCommand(), expectedToken);

            // Assert — токен пробрасывается в первую же операцию репозитория
            _userRepositoryMock.Verify(
                r => r.ExistsByEmailAsync(It.IsAny<string>(), expectedToken),
                Times.Once);
        }

        #endregion

        #region Conflicts

        [Fact]
        public async Task Handle_WhenEmailTaken_ReturnsErrorAndDoesNotProceedAsync()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Result result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            // Assert — fail-fast: последующих проверок и создания быть не должно
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.EmailAlreadyTaken);

                _userRepositoryMock.Verify(
                    r => r.ExistsByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userRepositoryMock.Verify(
                    r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userRepositoryMock.Verify(
                    r => r.CreateAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
                _publisherMock.Verify(
                    p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenUserNameTaken_ReturnsErrorAndDoesNotProceedAsync()
        {
            // Arrange — email свободен, userName занят
            _userRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Result result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.UserNameAlreadyTaken);

                _userRepositoryMock.Verify(
                    r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userRepositoryMock.Verify(
                    r => r.CreateAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
                _publisherMock.Verify(
                    p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPhoneTaken_ReturnsErrorAndDoesNotProceedAsync()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act — phone != null, иначе проверка пропускается
            Result result = await _handler.Handle(
                CreateValidCommand(phone: "+79001234567"),
                CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.PhonelAlreadyTaken);

                _userRepositoryMock.Verify(
                    r => r.CreateAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
                _publisherMock.Verify(
                    p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        [Fact]
        public async Task Handle_WhenPhoneIsNull_DoesNotCheckPhoneUniquenessAsync()
        {
            // Arrange — отдельный сценарий: проверка что phone==null обходит ExistsByPhoneAsync
            SetupSuccessfulFlow(Guid.NewGuid());

            // Act
            await _handler.Handle(CreateValidCommand(phone: null), CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(
                r => r.ExistsByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region CreateAsync failure

        [Fact]
        public async Task Handle_WhenCreateFails_ReturnsErrorAndDoesNotPublishAsync()
        {
            // Arrange — проверки уникальности проходят, CreateAsync возвращает ошибку
            _userRepositoryMock
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.ExistsByUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepositoryMock
                .Setup(r => r.CreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid>.Failure(AuthErrors.RegistrationFailed));

            // Act
            Result result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            // Assert — ошибка из Repository пробрасывается, событие не публикуется
            using (new AssertionScope())
            {
                result.IsFailure.Should().BeTrue();
                result.Error.Should().Be(AuthErrors.RegistrationFailed);

                _publisherMock.Verify(
                    p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
