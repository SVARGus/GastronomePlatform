using FluentAssertions;
using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Common.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="AggregateRoot{TId}"/>.
    /// </summary>
    public sealed class AggregateRootTests
    {
        /// <summary>
        /// Тестовое доменное событие.
        /// </summary>
        private sealed record TestDomainEvent(string Payload) : IDomainEvent
        {
            public Guid EventId { get; } = Guid.NewGuid();
            public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Конкретная реализация AggregateRoot для тестирования.
        /// Публично пробрасывает protected RaiseDomainEvent, чтобы тесты
        /// могли проверить поведение без наследования самого теста от агрегата.
        /// </summary>
        private sealed class TestAggregate : AggregateRoot<Guid>
        {
            public TestAggregate(Guid id) : base(id) { }

            public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
        }

        /// <summary>
        /// Второй тип агрегата — чтобы проверить что Equality из Entity работает.
        /// </summary>
        private sealed class AnotherAggregate : AggregateRoot<Guid>
        {
            public AnotherAggregate(Guid id) : base(id) { }
        }

        #region DomainEvents collection

        [Fact]
        public void DomainEvents_Initially_ShouldBeEmpty()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());

            // Assert
            aggregate.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void RaiseDomainEvent_ShouldAddEventToCollection()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());
            TestDomainEvent domainEvent = new("тест");

            // Act
            aggregate.RaiseEvent(domainEvent);

            // Assert
            aggregate.DomainEvents.Should().ContainSingle()
                .Which.Should().Be(domainEvent);
        }

        [Fact]
        public void RaiseDomainEvent_MultipleTimes_ShouldPreserveOrder()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());
            TestDomainEvent event1 = new("первое");
            TestDomainEvent event2 = new("второе");
            TestDomainEvent event3 = new("третье");

            // Act
            aggregate.RaiseEvent(event1);
            aggregate.RaiseEvent(event2);
            aggregate.RaiseEvent(event3);

            // Assert — порядок добавления должен сохраняться
            aggregate.DomainEvents.Should().ContainInOrder(event1, event2, event3);
        }

        [Fact]
        public void ClearDomainEvents_ShouldEmptyCollection()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());
            aggregate.RaiseEvent(new TestDomainEvent("событие"));

            // Act
            aggregate.ClearDomainEvents();

            // Assert
            aggregate.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void ClearDomainEvents_OnEmptyCollection_ShouldNotThrow()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());

            // Act
            Action action = () => aggregate.ClearDomainEvents();

            // Assert
            action.Should().NotThrow();
            aggregate.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void DomainEvents_ShouldBeReadOnlyCollection()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());

            // Act
            IReadOnlyCollection<IDomainEvent> events = aggregate.DomainEvents;

            // Assert — DomainEvents возвращает AsReadOnly, IsReadOnly = true
            if (events is ICollection<IDomainEvent> writable)
            {
                writable.IsReadOnly.Should().BeTrue();
            }
        }

        [Fact]
        public void DomainEvents_AfterRaise_ShouldNotBeAffectedByExternalListModification()
        {
            // Arrange
            TestAggregate aggregate = new(Guid.NewGuid());
            aggregate.RaiseEvent(new TestDomainEvent("событие"));

            // Act — даже если попытаться привести к ICollection, изменение должно падать
            IReadOnlyCollection<IDomainEvent> events = aggregate.DomainEvents;
            Action tryAdd = () =>
            {
                if (events is ICollection<IDomainEvent> writable)
                {
                    writable.Add(new TestDomainEvent("посторонний"));
                }
            };

            // Assert — ReadOnlyCollection бросает NotSupportedException на Add
            tryAdd.Should().Throw<NotSupportedException>();
            aggregate.DomainEvents.Should().HaveCount(1);
        }

        #endregion

        #region Equality (наследуется от Entity)

        [Fact]
        public void Equals_WithSameId_ShouldBeEqual()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            TestAggregate aggregate1 = new(id);
            TestAggregate aggregate2 = new(id);

            // Assert — Equality-контракт унаследован от Entity<TId>
            aggregate1.Should().Be(aggregate2);
        }

        [Fact]
        public void Equals_WithDifferentAggregateType_ShouldNotBeEqual()
        {
            // Arrange — одинаковый Id, разные типы агрегатов
            Guid id = Guid.NewGuid();
            TestAggregate aggregate1 = new(id);
            AnotherAggregate aggregate2 = new(id);

            // Assert
            aggregate1.Equals(aggregate2).Should().BeFalse();
        }

        #endregion
    }
}
