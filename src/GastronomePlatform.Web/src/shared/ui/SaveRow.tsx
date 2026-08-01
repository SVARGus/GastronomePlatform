import { Button } from './Button';

interface SaveRowProps {
  isLoading: boolean;
  isSuccess: boolean;
  isError: boolean;
  onSave: () => void;
  /** Текст ошибки; по умолчанию — общий. */
  errorText?: string;
}

/**
 * Кнопка «Сохранить» с индикацией результата — общий низ карточек-секций
 * (профиль кабинета, редактор блюда).
 */
export function SaveRow({ isLoading, isSuccess, isError, onSave, errorText }: SaveRowProps) {
  return (
    <div className="mt-5 flex items-center gap-3">
      <Button variant="secondary" disabled={isLoading} onClick={onSave}>
        {isLoading ? 'Сохраняем…' : 'Сохранить'}
      </Button>
      {isSuccess && !isLoading && <span className="text-sm text-success-text">Сохранено</span>}
      {isError && !isLoading && (
        <span className="text-sm text-danger-text">
          {errorText ?? 'Не получилось сохранить — проверьте поля.'}
        </span>
      )}
    </div>
  );
}
