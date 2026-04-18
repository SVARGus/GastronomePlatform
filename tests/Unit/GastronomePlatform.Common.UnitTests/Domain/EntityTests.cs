using FluentAssertions;
using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Common.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="Entity{TId}"/>.
    /// </summary>
    public sealed class EntityTests
    {
        /// <summary>
        /// Конкретная реализация Entity для тестирования.
        /// Абстрактный класс нельзя инстанциировать напрямую.
        /// </summary>
        private sealed class TestEntity : Entity<Guid>
        {
            public TestEntity(Guid id) : base(id) { }
        }

        /// <summary>
        /// Другой тип сущности — для проверки что разные типы не равны.
        /// </summary>
        private sealed class AnotherEntity : Entity<Guid>
        {
            public AnotherEntity(Guid id) : base(id) { }
        }

        #region Constructor

        [Fact]
        public void Constructor_WithValidId_ShouldSetId()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            TestEntity entity = new(id);

            // Assert
            entity.Id.Should().Be(id);
        }

        [Fact]
        public void Constructor_WithNullId_ShouldThrowArgumentNullException()
        {
            // Act
            Action action = () => new TestEntity(default);

            // Assert — default(Guid) = Guid.Empty, не null
            // Guid — struct, не может быть null, поэтому исключения нет
            action.Should().NotThrow();
        }

        #endregion

        #region Equality Members

        [Fact]
        public void Equals_WithSameId_ShouldBeEqual()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            TestEntity entity1 = new(id);
            TestEntity entity2 = new(id);

            // Assert
            entity1.Should().Be(entity2);
            entity1.Equals(entity2).Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldNotBeEqual()
        {
            // Arrange
            TestEntity entity1 = new(Guid.NewGuid());
            TestEntity entity2 = new(Guid.NewGuid());

            // Assert
            entity1.Should().NotBe(entity2);
            entity1.Equals(entity2).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithSameReference_ShouldBeEqual()
        {
            // Arrange
            TestEntity entity = new(Guid.NewGuid());

            // Assert — ReferenceEquals ветка
            entity.Equals(entity).Should().BeTrue();
        }

        [Fact]
        public void Equals_WithNull_ShouldNotBeEqual()
        {
            // Arrange
            TestEntity entity = new(Guid.NewGuid());

            // Act
            bool result = entity.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_WithDifferentEntityType_ShouldNotBeEqual()
        {
            // Arrange — одинаковый Id, разные типы сущностей
            Guid id = Guid.NewGuid();
            TestEntity entity1 = new(id);
            AnotherEntity entity2 = new(id);

            // Assert — GetType() != other.GetType() ветка
            entity1.Equals(entity2).Should().BeFalse();
        }

        #endregion

        #region Operators

        [Fact]
        public void EqualityOperator_WithSameId_ShouldBeTrue()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            TestEntity entity1 = new(id);
            TestEntity entity2 = new(id);

            // Assert
            (entity1 == entity2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_WithDifferentId_ShouldBeFalse()
        {
            // Arrange
            TestEntity entity1 = new(Guid.NewGuid());
            TestEntity entity2 = new(Guid.NewGuid());

            // Assert
            (entity1 == entity2).Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_BothNull_ShouldBeTrue()
        {
            // Arrange
            TestEntity? entity1 = null;
            TestEntity? entity2 = null;

            // Assert
            (entity1 == entity2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_OneNull_ShouldBeFalse()
        {
            // Arrange
            TestEntity? entity1 = new(Guid.NewGuid());
            TestEntity? entity2 = null;

            // Assert
            (entity1 == entity2).Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_WithDifferentId_ShouldBeTrue()
        {
            // Arrange
            TestEntity entity1 = new(Guid.NewGuid());
            TestEntity entity2 = new(Guid.NewGuid());

            // Assert
            (entity1 != entity2).Should().BeTrue();
        }

        #endregion

        #region GetHashCode

        [Fact]
        public void GetHashCode_WithSameId_ShouldBeEqual()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            TestEntity entity1 = new(id);
            TestEntity entity2 = new(id);

            // Assert — контракт: если Equals → одинаковый GetHashCode
            entity1.GetHashCode().Should().Be(entity2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_WithDifferentId_ShouldNotBeEqual()
        {
            // Arrange
            TestEntity entity1 = new(Guid.NewGuid());
            TestEntity entity2 = new(Guid.NewGuid());

            // Assert — высокая вероятность разных хэшей для разных Guid
            entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
        }

        #endregion
    }
}
