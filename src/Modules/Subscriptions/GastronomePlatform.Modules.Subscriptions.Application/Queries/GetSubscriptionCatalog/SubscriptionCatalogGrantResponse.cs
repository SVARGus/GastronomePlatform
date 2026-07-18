using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog
{
    /// <summary>
    /// Позиция состава услуг плана на витрине (UC-SUB-040 response).
    /// </summary>
    /// <remarks>
    /// Отдаётся значение перечисления, а не готовый человекочитаемый текст:
    /// формулировка и перевод — задача клиента, который знает язык и контекст
    /// показа. Сервер отвечает за то, какие услуги входят в план, а не за то,
    /// как они называются в интерфейсе.
    /// </remarks>
    /// <param name="Grant">Услуга, входящая в план.</param>
    /// <param name="Quantity">
    /// Квота для квотовых услуг (например, число продвижений).
    /// <see langword="null"/> — безлимит либо квота к этой услуге неприменима.
    /// </param>
    public sealed record SubscriptionCatalogGrantResponse(
        FeatureGrant Grant,
        int? Quantity);
}
