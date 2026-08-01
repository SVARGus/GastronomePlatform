import { ArrowDown, ArrowUp, Pencil, Thermometer, Timer, Trash2, Upload } from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { getErrorMessage } from '../auth/apiErrors';
import {
  useAddRecipeStepMutation,
  useRemoveRecipeStepMutation,
  useReorderRecipeStepsMutation,
  useUpdateRecipeStepMutation,
} from '../dishes/dishesApi';
import { useUploadFileMutation } from '../media/mediaApi';
import { mediaThumbnailUrl } from '../../shared/api/media';
import type { RecipeStepViewDto, RecipeViewDto } from '../../shared/api/types/dishes';
import { Button } from '../../shared/ui/Button';
import { RowIconButton } from './IngredientsSection';

const MIN_DESCRIPTION = 10;
const MAX_DESCRIPTION = 4000;
const MAX_TITLE = 200;
const MAX_VIDEO_URL = 500;

/** Черновик формы шага; числовые поля — строками, парсинг при отправке. */
interface StepDraft {
  editingId: string | null;
  title: string;
  description: string;
  temperature: string;
  timer: string;
  videoUrl: string;
  imageMediaId: string | null;
}

const EMPTY_DRAFT: StepDraft = {
  editingId: null,
  title: '',
  description: '',
  temperature: '',
  timer: '',
  videoUrl: '',
  imageMediaId: null,
};

/** Пустая строка → null, иначе целое число. */
function intOrNull(value: string): number | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : Number(trimmed);
}

/**
 * Секция «Шаги приготовления» редактора рецепта: карточки шагов с
 * пооперационными командами (UC-DSH-020…023) и форма добавления/правки
 * с фото шага (Media, intendedEntityType RecipeStep).
 */
export function StepsSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const [draft, setDraft] = useState<StepDraft>(EMPTY_DRAFT);
  const [formError, setFormError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [addStep, { isLoading: isAdding }] = useAddRecipeStepMutation();
  const [updateStep, { isLoading: isUpdating }] = useUpdateRecipeStepMutation();
  const [removeStep] = useRemoveRecipeStepMutation();
  const [reorderSteps] = useReorderRecipeStepsMutation();
  const [uploadFile, { isLoading: isUploading }] = useUploadFileMutation();

  const isSaving = isAdding || isUpdating;
  const steps = useMemo(() => [...recipe.steps].sort((a, b) => a.order - b.order), [recipe.steps]);
  const canSubmit = draft.description.trim().length >= MIN_DESCRIPTION;

  function startEdit(step: RecipeStepViewDto) {
    setFormError(null);
    setDraft({
      editingId: step.id,
      title: step.title ?? '',
      description: step.description,
      temperature: step.temperatureCelsius !== null ? String(step.temperatureCelsius) : '',
      timer: step.timerMinutes !== null ? String(step.timerMinutes) : '',
      videoUrl: step.videoUrl ?? '',
      imageMediaId: step.imageMediaId,
    });
  }

  async function handleMove(index: number, direction: -1 | 1) {
    const ids = steps.map((s) => s.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    try {
      await reorderSteps({ dishId, orderedStepIds: ids }).unwrap();
    } catch (err) {
      setFormError(getErrorMessage(err) ?? 'Не получилось изменить порядок шагов.');
    }
  }

  async function handlePhoto(file: File) {
    setFormError(null);
    try {
      const { mediaId } = await uploadFile({ file, intendedEntityType: 'RecipeStep' }).unwrap();
      setDraft((d) => ({ ...d, imageMediaId: mediaId }));
    } catch {
      setFormError('Не получилось загрузить фото шага. Проверьте формат (JPG/PNG) и размер (до 10 МБ).');
    }
  }

  async function handleSubmit() {
    setFormError(null);
    const body = {
      description: draft.description.trim(),
      title: draft.title.trim() || null,
      imageMediaId: draft.imageMediaId,
      videoUrl: draft.videoUrl.trim() || null,
      temperatureCelsius: intOrNull(draft.temperature),
      timerMinutes: intOrNull(draft.timer),
    };
    try {
      if (draft.editingId) {
        await updateStep({ dishId, stepId: draft.editingId, ...body }).unwrap();
      } else {
        await addStep({ dishId, ...body }).unwrap();
      }
      setDraft(EMPTY_DRAFT);
    } catch (err) {
      setFormError(getErrorMessage(err) ?? 'Не получилось сохранить шаг — проверьте поля.');
    }
  }

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Шаги приготовления</h2>

      <div className="mt-4 flex flex-col gap-3.5">
        {steps.length === 0 && (
          <p className="text-[15px] text-ink-muted">
            Пока пусто. Опишите хотя бы один шаг — без него блюдо не опубликовать.
          </p>
        )}
        {steps.map((step, index) => (
          <div key={step.id} className="flex gap-4 rounded-[14px] bg-sunken p-4.5">
            <span className="tabular min-w-[28px] shrink-0 font-display text-[28px] font-[560] leading-none text-action">
              {step.order}
            </span>
            <div className="min-w-0 flex-1">
              {step.title && <h3 className="mb-1 font-semibold">{step.title}</h3>}
              <p className="text-[15px] text-ink">{step.description}</p>
              <div className="mt-2.5 flex flex-wrap items-center gap-2.5 empty:hidden">
                {step.imageMediaId && (
                  <img
                    src={mediaThumbnailUrl(step.imageMediaId)}
                    alt=""
                    className="h-[72px] w-[72px] rounded-full object-cover shadow-chip ring-4 ring-surface"
                  />
                )}
                {step.temperatureCelsius !== null && (
                  <StepMetaChip>
                    <Thermometer className="h-[15px] w-[15px] text-action" strokeWidth={1.75} aria-hidden />
                    {step.temperatureCelsius} °C
                  </StepMetaChip>
                )}
                {step.timerMinutes !== null && (
                  <StepMetaChip>
                    <Timer className="h-[15px] w-[15px] text-action" strokeWidth={1.75} aria-hidden />
                    {step.timerMinutes} мин
                  </StepMetaChip>
                )}
              </div>
            </div>
            <div className="flex shrink-0 flex-col gap-1.5">
              <RowIconButton label="Выше" disabled={index === 0} onClick={() => handleMove(index, -1)}>
                <ArrowUp className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </RowIconButton>
              <RowIconButton
                label="Ниже"
                disabled={index === steps.length - 1}
                onClick={() => handleMove(index, 1)}
              >
                <ArrowDown className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </RowIconButton>
              <RowIconButton label="Изменить" onClick={() => startEdit(step)}>
                <Pencil className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </RowIconButton>
              <RowIconButton label="Удалить" danger onClick={() => removeStep({ dishId, stepId: step.id })}>
                <Trash2 className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </RowIconButton>
            </div>
          </div>
        ))}
      </div>

      {/* Форма добавления / редактирования шага */}
      <div className="mt-5 flex flex-col gap-4 border-t border-line pt-5">
        {draft.editingId && (
          <p className="text-sm text-ink-secondary">Правка шага — сохраните или отмените изменения.</p>
        )}
        <div>
          <label htmlFor="step-title" className="mb-1.5 block text-sm font-medium text-ink">
            Заголовок <span className="font-normal text-ink-muted">— необязательно</span>
          </label>
          <input
            id="step-title"
            type="text"
            value={draft.title}
            maxLength={MAX_TITLE}
            onChange={(e) => setDraft((d) => ({ ...d, title: e.target.value }))}
            placeholder="Например, обжарьте сырники"
            className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
        </div>
        <div>
          <label htmlFor="step-description" className="mb-1.5 block text-sm font-medium text-ink">
            Описание
          </label>
          <textarea
            id="step-description"
            rows={3}
            value={draft.description}
            onChange={(e) => setDraft((d) => ({ ...d, description: e.target.value }))}
            placeholder="Что делаем на этом шаге"
            className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
          <div className="mt-1.5 flex justify-between gap-3 text-[13px] text-ink-muted">
            {/* Пока описание короче минимума — подсказка янтарная: именно она
                держит кнопку «Добавить шаг» неактивной. */}
            <span
              className={
                draft.description.length > 0 && draft.description.trim().length < MIN_DESCRIPTION
                  ? 'font-medium text-warning-text'
                  : ''
              }
            >
              Минимум {MIN_DESCRIPTION} символов.
            </span>
            <span className={`tabular ${draft.description.length > MAX_DESCRIPTION ? 'text-danger-text' : ''}`}>
              {draft.description.length}/{MAX_DESCRIPTION}
            </span>
          </div>
        </div>
        <div className="flex flex-wrap gap-4">
          <div className="w-[140px]">
            <label htmlFor="step-temp" className="mb-1.5 block text-sm font-medium text-ink">
              Температура, °C
            </label>
            <input
              id="step-temp"
              type="number"
              value={draft.temperature}
              onChange={(e) => setDraft((d) => ({ ...d, temperature: e.target.value }))}
              placeholder="180"
              className="tabular h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
            />
          </div>
          <div className="w-[140px]">
            <label htmlFor="step-timer" className="mb-1.5 block text-sm font-medium text-ink">
              Таймер, мин
            </label>
            <input
              id="step-timer"
              type="number"
              value={draft.timer}
              onChange={(e) => setDraft((d) => ({ ...d, timer: e.target.value }))}
              placeholder="40"
              className="tabular h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
            />
          </div>
          <div className="min-w-[200px] flex-1">
            <label htmlFor="step-video" className="mb-1.5 block text-sm font-medium text-ink">
              Ссылка на видео <span className="font-normal text-ink-muted">— необязательно</span>
            </label>
            <input
              id="step-video"
              type="url"
              value={draft.videoUrl}
              maxLength={MAX_VIDEO_URL}
              onChange={(e) => setDraft((d) => ({ ...d, videoUrl: e.target.value }))}
              placeholder="https://"
              className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
            />
          </div>
        </div>
        <div>
          <span className="mb-1.5 block text-sm font-medium text-ink">Фото шага</span>
          <div className="flex flex-wrap items-center gap-4">
            {draft.imageMediaId ? (
              <img
                src={mediaThumbnailUrl(draft.imageMediaId)}
                alt=""
                className="h-[72px] w-[72px] shrink-0 rounded-full object-cover shadow-chip ring-4 ring-surface"
              />
            ) : (
              <span aria-hidden className="h-[72px] w-[72px] shrink-0 rounded-full bg-sunken" />
            )}
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png"
              className="hidden"
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) handlePhoto(file);
                e.target.value = '';
              }}
            />
            <Button variant="secondary" disabled={isUploading} onClick={() => fileInputRef.current?.click()}>
              <Upload className="mr-2 h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
              {isUploading ? 'Загружаем…' : 'Загрузить'}
            </Button>
            {draft.imageMediaId && (
              <Button variant="ghost" onClick={() => setDraft((d) => ({ ...d, imageMediaId: null }))}>
                Убрать
              </Button>
            )}
          </div>
        </div>

        {formError && <p className="text-sm text-danger-text">{formError}</p>}

        <div className="flex items-center gap-3">
          <Button variant="secondary" disabled={!canSubmit || isSaving} onClick={handleSubmit}>
            {isSaving ? 'Сохраняем…' : draft.editingId ? 'Сохранить шаг' : 'Добавить шаг'}
          </Button>
          {draft.editingId && (
            <Button variant="ghost" onClick={() => setDraft(EMPTY_DRAFT)}>
              Отмена
            </Button>
          )}
        </div>
      </div>
    </section>
  );
}

/** Пилюля-метаданные шага (температура / таймер). */
function StepMetaChip({ children }: { children: React.ReactNode }) {
  return (
    <span className="tabular inline-flex items-center gap-1.5 rounded-pill border border-line bg-surface px-3 py-1 text-sm">
      {children}
    </span>
  );
}
