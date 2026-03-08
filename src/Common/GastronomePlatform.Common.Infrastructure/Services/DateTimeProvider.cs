using GastronomePlatform.Common.Application.Abstractions;

namespace GastronomePlatform.Common.Infrastructure.Services
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTime Today => DateTime.UtcNow.Date;
    }
}
