using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Users.Domain.Enums;

namespace GastronomePlatform.Modules.Users.Domain.Entities
{
    /// <summary>
    /// Профиль пользователя платформы.
    /// Создаётся автоматически при регистрации через обработку <c>UserRegisteredEvent</c>.
    /// Хранит публичные данные пользователя — отображаемое имя, био, аватар и контактную информацию.
    /// </summary>
    /// <remarks>
    /// Поля <see cref="Email"/>, <see cref="Phone"/> и <see cref="UserName"/> являются
    /// зеркалом данных из модуля Auth. Источник правды — модуль Auth (<c>auth.AspNetUsers</c>).
    /// Изменение этих полей инициируется через <c>IAuthUserService</c>.
    /// </remarks>
    public sealed class UserProfile : Entity<Guid>
    {
        #region Properties
        /// <summary>
        /// Идентификатор пользователя.
        /// Совпадает с <c>ApplicationUser.Id</c> в модуле Auth.
        /// Логическая связь без физического Foreign Key (изоляция модулей).
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Адрес электронной почты. Зеркало из <c>auth.AspNetUsers</c>.
        /// Используется для отображения в профиле.
        /// </summary>
        public string Email { get; private set; } = string.Empty;

        /// <summary>
        /// Номер телефона. Зеркало из <c>auth.AspNetUsers</c>.
        /// Используется для отображения в профиле. Может отсутствовать.
        /// </summary>
        public string? Phone { get; private set; }

        /// <summary>
        /// Уникальный публичный никнейм. Зеркало из <c>auth.AspNetUsers</c>.
        /// Отображается везде где нет явного <see cref="DisplayName"/>.
        /// </summary>
        public string UserName { get; private set; } = string.Empty;

        /// <summary>
        /// Имя пользователя. Опционально.
        /// </summary>
        public string? FirstName { get; private set; }

        /// <summary>
        /// Фамилия пользователя. Опционально.
        /// </summary>
        public string? LastName { get; private set; }

        /// <summary>
        /// Отчество пользователя. Опционально.
        /// Используется для русскоязычной аудитории.
        /// </summary>
        public string? MiddleName { get; private set; }

        /// <summary>
        /// Публичное отображаемое имя. Опционально.
        /// Если указано — используется вместо <see cref="UserName"/> в интерфейсе.
        /// </summary>
        public string? DisplayName { get; private set; }

        /// <summary>
        /// Краткое описание «о себе». Опционально.
        /// Отображается в публичном профиле пользователя.
        /// </summary>
        public string? Bio { get; private set; }

        /// <summary>
        /// Пол пользователя. Опционально.
        /// </summary>
        public Gender? Gender { get; private set; }

        /// <summary>
        /// Дата рождения пользователя. Опционально.
        /// Используется <see cref="DateOnly"/> — время суток не имеет смысла для даты рождения.
        /// </summary>
        public DateOnly? DateOfBirth { get; private set; }

        /// <summary>
        /// Идентификатор медиафайла аватара. Опционально.
        /// Сам файл хранится в модуле Media. Здесь хранится только ссылка.
        /// </summary>
        public Guid? AvatarMediaId { get; private set; }

        /// <summary>
        /// Страна проживания. Опционально.
        /// </summary>
        public string? Country { get; private set; }

        /// <summary>
        /// Регион или область проживания. Опционально.
        /// </summary>
        public string? Region { get; private set; }

        /// <summary>
        /// Город проживания. Опционально.
        /// </summary>
        public string? City { get; private set; }

        /// <summary>
        /// Признак публичности профиля.
        /// Если <see langword="true"/> — профиль виден всем пользователям.
        /// По умолчанию профиль публичный.
        /// </summary>
        public bool IsPublic { get; private set; } = true;

        /// <summary>
        /// Дата и время создания профиля (UTC).
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Дата и время последнего обновления профиля (UTC).
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private UserProfile() : base() { }

        /// <summary>
        /// Создаёт профиль пользователя при регистрации.
        /// Вызывается из обработчика <c>UserRegisteredEvent</c>.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя из модуля Auth.</param>
        /// <param name="email">Адрес электронной почты.</param>
        /// <param name="userName">Никнейм пользователя.</param>
        /// <param name="phone">Номер телефона (опционально).</param>
        /// <param name="createdAt">Дата и время создания профиля (UTC).</param>
        private UserProfile(
            Guid userId,
            string email,
            string userName,
            string? phone,
            DateTimeOffset createdAt)
            : base(userId)
        {
            UserId = userId;
            Email = email;
            UserName = userName;
            Phone = phone;
            CreatedAt = createdAt;
            UpdatedAt = createdAt;
            IsPublic = true;
        }
        #endregion

        #region Factory Methods
        /// <summary>
        /// Создаёт новый профиль пользователя.
        /// Вызывается из обработчика <c>UserRegisteredEvent</c>.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя из модуля Auth.</param>
        /// <param name="email">Адрес электронной почты.</param>
        /// <param name="userName">Никнейм пользователя.</param>
        /// <param name="phone">Номер телефона (опционально).</param>
        /// <param name="createdAt">Дата и время создания (UTC).</param>
        /// <returns>Новый экземпляр <see cref="UserProfile"/>.</returns>
        public static UserProfile Create(
            Guid userId,
            string email,
            string userName,
            string? phone,
            DateTimeOffset createdAt)
        {
            return new UserProfile(userId, email, userName, phone, createdAt);
        }
        #endregion

        #region Update Methods
        /// <summary>
        /// Обновляет персональные данные пользователя.
        /// </summary>
        /// <param name="firstName">Имя.</param>
        /// <param name="lastName">Фамилия.</param>
        /// <param name="middleName">Отчество.</param>
        /// <param name="displayName">Публичное отображаемое имя.</param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdatePersonalInfo(
            string? firstName,
            string? lastName,
            string? middleName,
            string? displayName,
            DateTimeOffset updatedAt)
        {
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
            DisplayName = displayName;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Обновляет описание профиля.
        /// </summary>
        /// <param name="bio">Краткое описание «о себе».</param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdateBio(string? bio, DateTimeOffset updatedAt)
        {
            Bio = bio;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Обновляет дополнительные данные профиля.
        /// </summary>
        /// <param name="gender">Пол пользователя.</param>
        /// <param name="dateOfBirth">Дата рождения.</param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdatePersonalDetails(
            Gender? gender,
            DateOnly? dateOfBirth,
            DateTimeOffset updatedAt)
        {
            Gender = gender;
            DateOfBirth = dateOfBirth;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Обновляет местоположение пользователя.
        /// </summary>
        /// <param name="country">Страна.</param>
        /// <param name="region">Регион или область.</param>
        /// <param name="city">Город.</param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdateLocation(
            string? country,
            string? region,
            string? city,
            DateTimeOffset updatedAt)
        {
            Country = country;
            Region = region;
            City = city;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Обновляет аватар пользователя.
        /// </summary>
        /// <param name="avatarMediaId">
        /// Идентификатор медиафайла из модуля Media.
        /// <see langword="null"/> — удалить аватар.
        /// </param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdateAvatar(Guid? avatarMediaId, DateTimeOffset updatedAt)
        {
            AvatarMediaId = avatarMediaId;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Устанавливает видимость профиля.
        /// </summary>
        /// <param name="isPublic">
        /// <see langword="true"/> — профиль публичный;
        /// <see langword="false"/> — профиль скрыт.
        /// </param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void SetVisibility(bool isPublic, DateTimeOffset updatedAt)
        {
            IsPublic = isPublic;
            UpdatedAt = updatedAt;
        }
        #endregion

        #region Auth Mirror Methods
        /// <summary>
        /// Обновляет зеркальные данные из модуля Auth.
        /// Вызывается только при успешном изменении данных через <c>IAuthUserService</c>.
        /// </summary>
        /// <param name="email">Новый email.</param>
        /// <param name="phone">Новый номер телефона.</param>
        /// <param name="userName">Новый никнейм.</param>
        /// <param name="updatedAt">Дата и время обновления (UTC).</param>
        public void UpdateAuthMirrorData(
            string email,
            string? phone,
            string userName,
            DateTimeOffset updatedAt)
        {
            Email = email;
            Phone = phone;
            UserName = userName;
            UpdatedAt = updatedAt;
        }
        #endregion
    }
}
