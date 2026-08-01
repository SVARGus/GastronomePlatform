import { ArrowDown, ArrowUp, Pencil, Trash2 } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { getErrorMessage } from '../auth/apiErrors';
import {
  useAddCatalogIngredientMutation,
  useAddFreeformIngredientMutation,
  useIngredientByIdQuery,
  useLazyIngredientSearchQuery,
  useMeasureUnitsQuery,
  useRemoveRecipeIngredientMutation,
  useReorderRecipeIngredientsMutation,
  useUpdateRecipeIngredientMutation,
} from '../dishes/dishesApi';
import type { RecipeIngredientViewDto, RecipeViewDto } from '../../shared/api/types/dishes';
import { formatQuantity } from '../../shared/lib/labels';
import { Button } from '../../shared/ui/Button';

const MAX_NOTE_LENGTH = 200;
const MAX_FREEFORM_LENGTH = 200;

type SourceTab = 'catalog' | 'freeform';

/** Черновик формы добавления/редактирования позиции. */
interface IngredientDraft {
  /** id редактируемой позиции; null — режим добавления. */
  editingId: string | null;
  source: SourceTab;
  ingredientId: string | null;
  ingredientName: string;
  freeformText: string;
  quantity: string;
  measureUnitId: string;
  isOptional: boolean;
  note: string;
}

const EMPTY_DRAFT: IngredientDraft = {
  editingId: null,
  source: 'catalog',
  ingredientId: null,
  ingredientName: '',
  freeformText: '',
  quantity: '',
  measureUnitId: '',
  isOptional: false,
  note: '',
};

/**
 * Секция «Ингредиенты» редактора рецепта: список позиций с пооперационными
 * командами (UC-DSH-030…033) и форма добавления двух природ — из справочника
 * (автокомплит UC-DSH-062) или свободным текстом.
 */
export function IngredientsSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const [draft, setDraft] = useState<IngredientDraft>(EMPTY_DRAFT);
  const [formError, setFormError] = useState<string | null>(null);

  const { data: units } = useMeasureUnitsQuery();
  const unitNames = useMemo(() => new Map((units ?? []).map((u) => [u.id, u.nameRu])), [units]);

  const [addCatalog, { isLoading: isAddingCatalog }] = useAddCatalogIngredientMutation();
  const [addFreeform, { isLoading: isAddingFreeform }] = useAddFreeformIngredientMutation();
  const [update, { isLoading: isUpdating }] = useUpdateRecipeIngredientMutation();
  const [remove] = useRemoveRecipeIngredientMutation();
  const [reorder] = useReorderRecipeIngredientsMutation();

  const isSaving = isAddingCatalog || isAddingFreeform || isUpdating;
  const ingredients = useMemo(
    () => [...recipe.ingredients].sort((a, b) => a.order - b.order),
    [recipe.ingredients],
  );

  const quantityNumber = Number(draft.quantity.replace(',', '.'));
  const canSubmit =
    quantityNumber > 0 &&
    draft.measureUnitId !== '' &&
    (draft.source === 'catalog' ? draft.ingredientId !== null : draft.freeformText.trim().length > 0);

  function startEdit(ingredient: RecipeIngredientViewDto, resolvedName: string) {
    setFormError(null);
    setDraft({
      editingId: ingredient.id,
      source: ingredient.type,
      ingredientId: ingredient.type === 'catalog' ? ingredient.ingredientId : null,
      ingredientName: ingredient.type === 'catalog' ? resolvedName : '',
      freeformText: ingredient.type === 'freeform' ? ingredient.freeformText : '',
      quantity: String(ingredient.quantity),
      measureUnitId: ingredient.measureUnitId,
      isOptional: ingredient.isOptional,
      note: ingredient.preparationNote ?? '',
    });
  }

  async function handleMove(index: number, direction: -1 | 1) {
    const ids = ingredients.map((i) => i.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    try {
      await reorder({ dishId, orderedIngredientIds: ids }).unwrap();
    } catch (err) {
      setFormError(getErrorMessage(err) ?? 'Не получилось изменить порядок.');
    }
  }

  async function handleSubmit() {
    setFormError(null);
    const common = {
      quantity: quantityNumber,
      measureUnitId: draft.measureUnitId,
      isOptional: draft.isOptional,
      preparationNote: draft.note.trim() || null,
    };
    try {
      if (draft.editingId) {
        await update({
          dishId,
          recipeIngredientId: draft.editingId,
          ingredientId: draft.source === 'catalog' ? draft.ingredientId : null,
          ingredientSpecId: null,
          freeformText: draft.source === 'freeform' ? draft.freeformText.trim() : null,
          ...common,
        }).unwrap();
      } else if (draft.source === 'catalog') {
        await addCatalog({
          dishId,
          ingredientId: draft.ingredientId!,
          ingredientSpecId: null,
          ...common,
        }).unwrap();
      } else {
        await addFreeform({ dishId, freeformText: draft.freeformText.trim(), ...common }).unwrap();
      }
      setDraft(EMPTY_DRAFT);
    } catch (err) {
      setFormError(getErrorMessage(err) ?? 'Не получилось сохранить ингредиент — проверьте поля.');
    }
  }

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Ингредиенты</h2>

      <div className="mt-4 flex flex-col">
        {ingredients.length === 0 && (
          <p className="text-[15px] text-ink-muted">
            Пока пусто. Добавьте хотя бы один ингредиент — без него блюдо не опубликовать.
          </p>
        )}
        {ingredients.map((ingredient, index) => (
          <IngredientRow
            key={ingredient.id}
            ingredient={ingredient}
            unitName={unitNames.get(ingredient.measureUnitId) ?? ''}
            isFirst={index === 0}
            isLast={index === ingredients.length - 1}
            onMoveUp={() => handleMove(index, -1)}
            onMoveDown={() => handleMove(index, 1)}
            onEdit={(resolvedName) => startEdit(ingredient, resolvedName)}
            onRemove={() => remove({ dishId, recipeIngredientId: ingredient.id })}
          />
        ))}
      </div>

      {/* Форма добавления / редактирования */}
      <div className="mt-5 border-t border-line pt-5">
        <div className="mb-4 flex items-center gap-2">
          {(
            [
              { key: 'catalog', label: 'Из справочника' },
              { key: 'freeform', label: 'Свободным текстом' },
            ] as const
          ).map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() =>
                // Набранный текст переезжает между режимами: «не нашёл в
                // справочнике → добавлю свободным текстом» без повторного ввода.
                setDraft((d) => ({
                  ...d,
                  source: tab.key,
                  ingredientId: null,
                  ingredientName: tab.key === 'catalog' ? d.freeformText : '',
                  freeformText: tab.key === 'freeform' && d.ingredientName ? d.ingredientName : d.freeformText,
                }))
              }
              className={`cursor-pointer rounded-pill px-4 py-2 text-[15px] font-medium ${
                draft.source === tab.key ? 'bg-saffron-50 text-action' : 'text-ink hover:bg-sunken'
              }`}
            >
              {tab.label}
            </button>
          ))}
          {draft.editingId && (
            <span className="ml-2 text-sm text-ink-secondary">— правка позиции</span>
          )}
        </div>

        {draft.source === 'catalog' ? (
          <IngredientAutocomplete
            value={draft.ingredientName}
            selectedId={draft.ingredientId}
            onSelect={(id, name) => setDraft((d) => ({ ...d, ingredientId: id, ingredientName: name }))}
            onInput={(text) => setDraft((d) => ({ ...d, ingredientName: text, ingredientId: null }))}
          />
        ) : (
          <div className="max-w-[380px]">
            <label htmlFor="freeform-text" className="mb-1.5 block text-sm font-medium text-ink">
              Название ингредиента
            </label>
            <input
              id="freeform-text"
              type="text"
              value={draft.freeformText}
              maxLength={MAX_FREEFORM_LENGTH}
              onChange={(e) => setDraft((d) => ({ ...d, freeformText: e.target.value }))}
              placeholder="Например, ванильный сахар"
              className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
            />
            <p className="mt-1.5 text-[13px] text-ink-muted">
              Ингредиент вне справочника — аллергены по нему платформа не проверяет.
            </p>
          </div>
        )}

        <div className="mt-4 flex flex-wrap items-end gap-4">
          <div className="w-[120px]">
            <label htmlFor="ing-quantity" className="mb-1.5 block text-sm font-medium text-ink">
              Количество
            </label>
            <input
              id="ing-quantity"
              type="number"
              min="0"
              step="any"
              value={draft.quantity}
              onChange={(e) => setDraft((d) => ({ ...d, quantity: e.target.value }))}
              className="tabular h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink focus:border-action"
            />
          </div>
          <div className="w-[180px]">
            <label htmlFor="ing-unit" className="mb-1.5 block text-sm font-medium text-ink">
              Единица
            </label>
            <select
              id="ing-unit"
              value={draft.measureUnitId}
              onChange={(e) => setDraft((d) => ({ ...d, measureUnitId: e.target.value }))}
              className="h-11 w-full cursor-pointer rounded-control border border-line bg-surface px-3 text-[15px] text-ink focus:border-action"
            >
              <option value="" disabled>
                Выберите…
              </option>
              {(units ?? []).map((u) => (
                <option key={u.id} value={u.id}>
                  {u.nameRu}
                </option>
              ))}
            </select>
          </div>
          <div className="min-w-[200px] flex-1">
            <label htmlFor="ing-note" className="mb-1.5 block text-sm font-medium text-ink">
              Заметка <span className="font-normal text-ink-muted">— необязательно</span>
            </label>
            <input
              id="ing-note"
              type="text"
              value={draft.note}
              maxLength={MAX_NOTE_LENGTH}
              onChange={(e) => setDraft((d) => ({ ...d, note: e.target.value }))}
              placeholder="Например, протереть через сито"
              className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
            />
          </div>
        </div>

        <label className="mt-4 flex cursor-pointer items-center gap-2.5 text-[15px]">
          <input
            type="checkbox"
            checked={draft.isOptional}
            onChange={(e) => setDraft((d) => ({ ...d, isOptional: e.target.checked }))}
            className="h-4.5 w-4.5 cursor-pointer accent-[#C25423]"
          />
          По желанию
        </label>

        {formError && <p className="mt-3 text-sm text-danger-text">{formError}</p>}

        <div className="mt-4 flex items-center gap-3">
          <Button variant="secondary" disabled={!canSubmit || isSaving} onClick={handleSubmit}>
            {isSaving
              ? 'Сохраняем…'
              : draft.editingId
                ? 'Сохранить изменения'
                : 'Добавить ингредиент'}
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

/** Строка позиции: номер, имя (+чипсы), количество, заметка, действия. */
function IngredientRow({
  ingredient,
  unitName,
  isFirst,
  isLast,
  onMoveUp,
  onMoveDown,
  onEdit,
  onRemove,
}: {
  ingredient: RecipeIngredientViewDto;
  unitName: string;
  isFirst: boolean;
  isLast: boolean;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onEdit: (resolvedName: string) => void;
  onRemove: () => void;
}) {
  const isCatalog = ingredient.type === 'catalog';
  const { data: catalogIngredient } = useIngredientByIdQuery(
    isCatalog ? ingredient.ingredientId : '',
    { skip: !isCatalog },
  );
  const name = isCatalog ? (catalogIngredient?.name ?? '…') : ingredient.freeformText;

  return (
    <div className="flex items-center gap-3 border-b border-line py-3 last:border-b-0">
      <span className="tabular w-6 shrink-0 text-center text-sm text-ink-muted">
        {ingredient.order}
      </span>
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="font-medium">{name}</span>
          {!isCatalog && (
            <span className="rounded-pill bg-warning-bg px-2 py-0.5 text-xs font-semibold text-warning-text">
              вне справочника
            </span>
          )}
          {ingredient.isOptional && <span className="text-[13px] text-ink-muted">по желанию</span>}
        </div>
        {ingredient.preparationNote && (
          <p className="text-[13px] italic text-ink-muted">{ingredient.preparationNote}</p>
        )}
      </div>
      <span className="tabular shrink-0 text-[15px]">
        {formatQuantity(ingredient.quantity)} {unitName}
      </span>
      <div className="flex shrink-0 gap-1.5">
        <RowIconButton label="Выше" disabled={isFirst} onClick={onMoveUp}>
          <ArrowUp className="h-4 w-4" strokeWidth={1.75} aria-hidden />
        </RowIconButton>
        <RowIconButton label="Ниже" disabled={isLast} onClick={onMoveDown}>
          <ArrowDown className="h-4 w-4" strokeWidth={1.75} aria-hidden />
        </RowIconButton>
        <RowIconButton label="Изменить" onClick={() => onEdit(name)}>
          <Pencil className="h-4 w-4" strokeWidth={1.75} aria-hidden />
        </RowIconButton>
        <RowIconButton label="Удалить" danger onClick={onRemove}>
          <Trash2 className="h-4 w-4" strokeWidth={1.75} aria-hidden />
        </RowIconButton>
      </div>
    </div>
  );
}

/** Компактная иконка-кнопка действий строки/карточки. */
export function RowIconButton({
  label,
  danger,
  disabled,
  onClick,
  children,
}: {
  label: string;
  danger?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
      className={`flex h-8 w-8 items-center justify-center rounded-control border disabled:cursor-not-allowed disabled:opacity-40 ${
        danger
          ? 'border-danger text-danger-text hover:bg-danger-bg'
          : 'border-line text-ink-secondary hover:bg-sunken'
      } ${disabled ? '' : 'cursor-pointer'}`}
    >
      {children}
    </button>
  );
}

/** Автокомплит справочника ингредиентов (UC-DSH-062): debounce 250 мс, до 8 подсказок. */
function IngredientAutocomplete({
  value,
  selectedId,
  onSelect,
  onInput,
}: {
  value: string;
  selectedId: string | null;
  onSelect: (id: string, name: string) => void;
  onInput: (text: string) => void;
}) {
  const [focused, setFocused] = useState(false);
  const [search, { data: options }] = useLazyIngredientSearchQuery();
  const debounceRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (selectedId !== null || value.trim().length < 2) return;
    window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => search(value.trim()), 250);
    return () => window.clearTimeout(debounceRef.current);
  }, [value, selectedId, search]);

  const visible =
    focused && selectedId === null && value.trim().length >= 2
      ? (options ?? []).filter((o) => o.isActive).slice(0, 8)
      : [];

  return (
    <div className="relative max-w-[380px]">
      <label htmlFor="ing-search" className="mb-1.5 block text-sm font-medium text-ink">
        Ингредиент
      </label>
      <input
        id="ing-search"
        type="text"
        value={value}
        onChange={(e) => onInput(e.target.value)}
        onFocus={() => setFocused(true)}
        onBlur={() => window.setTimeout(() => setFocused(false), 150)}
        placeholder="Начните вводить название…"
        className={`h-11 w-full rounded-control border bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action ${
          selectedId ? 'border-success-text' : 'border-line'
        }`}
      />
      {visible.length > 0 && (
        <div className="absolute inset-x-0 top-[calc(100%+6px)] z-10 flex flex-col rounded-control border border-line bg-surface p-1.5 shadow-lifted">
          {visible.map((option) => (
            <button
              key={option.id}
              type="button"
              onMouseDown={(e) => {
                e.preventDefault();
                onSelect(option.id, option.name);
              }}
              className="cursor-pointer rounded-[8px] px-3 py-2 text-left text-[15px] text-ink hover:bg-sunken"
            >
              {option.name}
            </button>
          ))}
        </div>
      )}
      <p className="mt-1.5 text-[13px] text-ink-muted">
        {selectedId ? 'Ингредиент выбран из справочника.' : 'Выберите вариант из подсказок.'}
      </p>
    </div>
  );
}
