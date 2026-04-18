using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Domain.Events;
using GastronomePlatform.Modules.Users.Application.EventHandlers;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.EventHandlers
{
    /// <summary>
    /// Тесты для <see cref="UserRegisteredEventHandler"/>.
    /// </summary>
    public sealed class UserRegisteredEventHandlerTests
    {
        private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
        private readonly UserRegisteredEventHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private const string EMAIL = "user@example.com";
        private const string USERNAME = "test_user";
        private const string PHONE = "+79001234567";
        private static readonly DateTimeOffset _now =
            new(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);

        public UserRegisteredEventHandlerTests()
        {
            // NullLogger — ILogger не является частью контракта handler'а,
            // мокать ради Verify логов хрупко и не несёт ценности
            _handler = new UserRegisteredEventHandler(
                _userProfileRepositoryMock.Object,
                _dateTimeProviderMock.Object,
                NullLogger<UserRegisteredEventHandler>.Instance);
        }

        private static UserRegisteredEvent CreateEvent(string? phone = PHONE) => new()
        {
            UserId = _userId,
            Email = EMAIL,
            UserName = USERNAME,
            PhoneNumber = phone
        };

        #region Constructor

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            Action action = () => new UserRegisteredEventHandler(
                null!, _dateTimeProviderMock.Object, NullLogger<UserRegisteredEventHandler>.Instance);

            action.Should().Throw<ArgumentNullException>().WithParameterName("userProfileRepository");
        }

        [Fact]
        public void Constructor_WithNullDateTimeProvider_ShouldThrowArgumentNullException()
        {
            Action action = () => new UserRegisteredEventHandler(
                _userProfileRepositoryMock.Object, null!, NullLogger<UserRegisteredEventHandler>.Instance);

            action.Should().Throw<ArgumentNullException>().WithParameterName("dateTimeProvider");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            Action action = () => new UserRegisteredEventHandler(
                _userProfileRepositoryMock.Object, _dateTimeProviderMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion

        #region Создание профиля

        [Fact]
        public async Task Handle_WhenProfileDoesNotExist_ShouldCreateProfileWithExpectedFieldsAsync()
        {
            // Arrange
            _userProfileRepositoryMock
                .Setup(r => r.ExistsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            await _handler.Handle(CreateEvent(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                _userProfileRepositoryMock.Verify(
                    r => r.AddAsync(
                        It.Is<UserProfile>(p =>
                            p.UserId == _userId &&
                            p.Email == EMAIL &&
                            p.UserName == USERNAME &&
                            p.Phone == PHONE &&
                            p.CreatedAt == _now &&
                            p.IsPublic),
                        It.IsAny<CancellationToken>()),
                    Times.Once);

                _userProfileRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Fact]
        public async Task Handle_WhenEventHasNullPhone_ShouldCreateProfileWithNullPhoneAsync()
        {
            // Arrange — регистрация без телефона (поле опционально в RegisterCommand)
            _userProfileRepositoryMock
                .Setup(r => r.ExistsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _dateTimeProviderMock.SetupGet(d => d.UtcNow).Returns(_now);

            // Act
            await _handler.Handle(CreateEvent(phone: null), CancellationToken.None);

            // Assert
            _userProfileRepositoryMock.Verify(
                r => r.AddAsync(
                    It.Is<UserProfile>(p => p.Phone == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Идемпотентность

        [Fact]
        public async Task Handle_WhenProfileAlreadyExists_ShouldSkipCreationAsync()
        {
            // Arrange — защита от повторной обработки события (дубль из RabbitMQ в будущем,
            // или ретрай MediatR — реализация через ExistsAsync перед Create)
            _userProfileRepositoryMock
                .Setup(r => r.ExistsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _handler.Handle(CreateEvent(), CancellationToken.None);

            // Assert — повторная обработка тихо завершается без записи
            using (new AssertionScope())
            {
                _userProfileRepositoryMock.Verify(
                    r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
                    Times.Never);
                _userProfileRepositoryMock.Verify(
                    r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion
    }
}
