using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Events;
using GastronomePlatform.Modules.Users.Application.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GastronomePlatform.Users.UnitTests.Application.EventHandlers
{
    /// <summary>
    /// Тесты для <see cref="SubscriptionActivatedEventHandler"/>.
    /// </summary>
    public sealed class SubscriptionActivatedEventHandlerTests
    {
        private readonly Mock<IAuthUserService> _authUserServiceMock = new();
        private readonly SubscriptionActivatedEventHandler _handler;

        private static readonly Guid _userId = Guid.NewGuid();
        private static readonly Guid _subscriptionId = Guid.NewGuid();
        private static readonly Guid _planId = Guid.NewGuid();

        public SubscriptionActivatedEventHandlerTests()
        {
            // NullLogger — ILogger не является частью контракта handler'а,
            // мокать ради Verify логов хрупко и не несёт ценности.
            _handler = new SubscriptionActivatedEventHandler(
                _authUserServiceMock.Object,
                NullLogger<SubscriptionActivatedEventHandler>.Instance);
        }

        private static SubscriptionActivatedEvent CreateEvent(PlanKind planKind = PlanKind.Base) =>
            new(_subscriptionId, _userId, _planId, planKind);

        #region Constructor

        [Fact]
        public void Constructor_WithNullAuthUserService_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscriptionActivatedEventHandler(
                null!, NullLogger<SubscriptionActivatedEventHandler>.Instance);

            action.Should().Throw<ArgumentNullException>().WithParameterName("authUserService");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            Action action = () => new SubscriptionActivatedEventHandler(
                _authUserServiceMock.Object, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion

        #region Фильтр PlanKind

        [Fact]
        public async Task Handle_WhenPlanKindIsAddOn_ShouldNotTouchRolesAsync()
        {
            // Arrange — AddOn-подписка не должна давать Premium-роль.
            SubscriptionActivatedEvent addOnEvent = CreateEvent(PlanKind.AddOn);

            // Act
            await _handler.Handle(addOnEvent, CancellationToken.None);

            // Assert — handler завершается до обращения к ролям.
            using (new AssertionScope())
            {
                _authUserServiceMock.Verify(
                    s => s.GetUserRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Never);

                _authUserServiceMock.Verify(
                    s => s.AddUserToRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion

        #region Идемпотентность

        [Fact]
        public async Task Handle_WhenUserAlreadyHasPremiumRole_ShouldSkipAddAsync()
        {
            // Arrange — защита от повторной обработки события (retry / дубль в Outbox Этапа 8+).
            _authUserServiceMock
                .Setup(s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { PlatformRoles.USER, PlatformRoles.PREMIUM });

            // Act
            await _handler.Handle(CreateEvent(), CancellationToken.None);

            // Assert — роль уже есть, повторно назначать не пытаемся.
            using (new AssertionScope())
            {
                _authUserServiceMock.Verify(
                    s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()),
                    Times.Once);

                _authUserServiceMock.Verify(
                    s => s.AddUserToRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task Handle_WhenPlanKindBaseAndRoleNotAssigned_ShouldAddPremiumRoleAsync()
        {
            // Arrange — обычный сценарий: активирована Base-подписка, роли Premium ещё нет.
            _authUserServiceMock
                .Setup(s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { PlatformRoles.USER });

            _authUserServiceMock
                .Setup(s => s.AddUserToRoleAsync(_userId, PlatformRoles.PREMIUM, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success());

            // Act
            await _handler.Handle(CreateEvent(), CancellationToken.None);

            // Assert
            using (new AssertionScope())
            {
                _authUserServiceMock.Verify(
                    s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()),
                    Times.Once);

                _authUserServiceMock.Verify(
                    s => s.AddUserToRoleAsync(_userId, PlatformRoles.PREMIUM, It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        #endregion

        #region Обработка ошибок сервиса

        [Fact]
        public async Task Handle_WhenAddRoleReturnsFailure_ShouldNotThrowAsync()
        {
            // Arrange — транзакция активации подписки уже закоммичена,
            // handler не должен пробрасывать исключение (иначе HTTP 500 при успешной подписке).
            _authUserServiceMock
                .Setup(s => s.GetUserRolesAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { PlatformRoles.USER });

            _authUserServiceMock
                .Setup(s => s.AddUserToRoleAsync(_userId, PlatformRoles.PREMIUM, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(AuthErrors.UserNotFound));

            // Act
            Func<Task> action = () => _handler.Handle(CreateEvent(), CancellationToken.None);

            // Assert — failure логируется внутри handler-а, но не бросается наружу.
            await action.Should().NotThrowAsync();

            _authUserServiceMock.Verify(
                s => s.AddUserToRoleAsync(_userId, PlatformRoles.PREMIUM, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
