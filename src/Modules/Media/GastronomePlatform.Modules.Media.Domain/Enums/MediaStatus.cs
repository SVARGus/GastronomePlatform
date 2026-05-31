namespace GastronomePlatform.Modules.Media.Domain.Enums
{
    /// <summary>
    /// Жизненный цикл медиафайла. Хранится как <c>int</c> в БД.
    /// </summary>
    /// <remarks>
    /// Граф переходов:
    /// <c>Uploaded → Processing → Ready</c>;
    /// <c>* → Failed</c> при сбое обработки;
    /// <c>Ready → Deleted</c> при soft-удалении;
    /// <c>Deleted</c> — конечное состояние.
    /// </remarks>
    public enum MediaStatus
    {
        /// <summary>Файл принят, ждёт обработки (валидация, генерация миниатюр).</summary>
        Uploaded = 0,

        /// <summary>В процессе обработки. На Этапе 2 — очень короткое промежуточное состояние.</summary>
        Processing = 1,

        /// <summary>Готов к отдаче клиенту.</summary>
        Ready = 2,

        /// <summary>Ошибка обработки. <c>StorageKey</c> может указывать на сырой файл или быть устаревшим.</summary>
        Failed = 3,

        /// <summary>Soft-delete. Физически удаляется фоновой задачей UC-MED-211 (Этап 8+).</summary>
        Deleted = 4
    }
}
