using MediatR;

namespace GastronomePlatform.Common.Domain.Events
{
    public interface IDomainEvent : INotification
    {
        DateTimeOffset OccurredOn { get; }
        Guid EventId { get; }
    }
}
