namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Статус попытки списания у платёжного шлюза.
    /// Хранится как <c>int</c> в БД. Используется в <c>SubscriptionPayment.Status</c>.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>Попытка инициирована, ответ шлюза ещё не получен.</summary>
        Pending = 0,

        /// <summary>Списание прошло успешно.</summary>
        Succeeded = 1,

        /// <summary>Списание отклонено (детали в <c>FailureReason</c>).</summary>
        Failed = 2,

        /// <summary>Платёж возвращён клиенту.</summary>
        Refunded = 3
    }
}
