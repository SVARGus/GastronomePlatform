import { Minus, Plus } from 'lucide-react';
import { useState } from 'react';
import {
  useSetRecipeNutritionMutation,
  useSetRecipeTimingMutation,
  useSetRecipeYieldMutation,
  useUpdateRecipeMutation,
} from '../dishes/dishesApi';
import type {
  NutritionCalcMethod,
  RecipeViewDto,
  YieldUnit,
} from '../../shared/api/types/dishes';
import { formatMinutes } from '../../shared/lib/labels';
import { SaveRow } from '../../shared/ui/SaveRow';

const MAX_TEXT = 4000;
const MIN_SERVINGS = 1;
const MAX_SERVINGS = 1000;

/** Полные подписи единиц выхода для select (короткие «г/кг/…» — в labels.ts). */
const YIELD_UNIT_OPTIONS: ReadonlyArray<{ value: YieldUnit; label: string }> = [
  { value: 'Grams', label: 'граммы' },
  { value: 'Kilograms', label: 'килограммы' },
  { value: 'Milliliters', label: 'миллилитры' },
  { value: 'Liters', label: 'литры' },
  { value: 'Pieces', label: 'штуки' },
  { value: 'Servings', label: 'порции' },
];

/** Пустая строка → null, иначе число (запятая допускается). */
function numOrNull(value: string): number | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : Number(trimmed.replace(',', '.'));
}

/** Число или 0 для обязательных числовых полей. */
function numOrZero(value: string): number {
  return numOrNull(value) ?? 0;
}

/** Обёртка карточки-секции. */
function SectionCard({ title, children }: { title: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">{title}</h2>
      <div className="mt-4">{children}</div>
    </section>
  );
}

/** Подписанное number-поле фиксированной ширины. */
function NumberField({
  id,
  label,
  value,
  onChange,
  placeholder,
  width = 'w-[140px]',
  disabled,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  width?: string;
  disabled?: boolean;
}) {
  return (
    <div className={width}>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-ink">
        {label}
      </label>
      <input
        id={id}
        type="number"
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="tabular h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action disabled:bg-sunken disabled:text-ink-muted"
      />
    </div>
  );
}

/**
 * «Выход и порции» (UC-DSH-041). Степпер «Порций» — единственный источник
 * числа порций на странице: сохранение шлёт PUT yield и вслед PUT recipe
 * с тем же servingsDefault (поле из «Общего о рецепте» убрано), чтобы
 * калькулятор витрины всегда совпадал с выходом.
 */
export function YieldSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const [quantityTotal, setQuantityTotal] = useState(String(recipe.yield.quantityTotal));
  const [yieldUnit, setYieldUnit] = useState<YieldUnit>(recipe.yield.yieldUnit);
  const [servings, setServings] = useState(recipe.yield.servingsCount);
  const [gramsPerServing, setGramsPerServing] = useState(
    recipe.yield.gramsPerServing !== null ? String(recipe.yield.gramsPerServing) : '',
  );

  const [saveYield, { isLoading: isSavingYield, isSuccess, error }] = useSetRecipeYieldMutation();
  const [saveRecipe, { isLoading: isSavingRecipe }] = useUpdateRecipeMutation();

  async function handleSave() {
    try {
      await saveYield({
        dishId,
        quantityTotal: numOrZero(quantityTotal),
        yieldUnit,
        servingsCount: servings,
        gramsPerServing: numOrNull(gramsPerServing),
      }).unwrap();
      // Синхронизация servingsDefault: остальные поля — из серверного состояния.
      await saveRecipe({
        dishId,
        introductionText: recipe.introductionText,
        servingsDefault: servings,
        isAlcoholic: recipe.isAlcoholic,
        authorTips: recipe.authorTips,
        servingSuggestions: recipe.servingSuggestions,
        notes: recipe.notes,
      }).unwrap();
    } catch {
      // Ошибка отображается через SaveRow (error из saveYield) либо общий текст.
    }
  }

  return (
    <SectionCard title="Выход и порции">
      <div className="flex flex-wrap items-end gap-4">
        <div>
          <span className="mb-1.5 block text-sm font-medium text-ink">На сколько порций рецепт</span>
          <div className="flex h-11 items-center gap-0.5 rounded-control border border-line bg-surface p-1">
            <button
              type="button"
              aria-label="Меньше порций"
              disabled={servings <= MIN_SERVINGS}
              onClick={() => setServings((s) => Math.max(MIN_SERVINGS, s - 1))}
              className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-[8px] text-action hover:bg-saffron-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              <Minus className="h-4.5 w-4.5" strokeWidth={2} aria-hidden />
            </button>
            <span className="tabular min-w-[30px] text-center font-semibold">{servings}</span>
            <button
              type="button"
              aria-label="Больше порций"
              disabled={servings >= MAX_SERVINGS}
              onClick={() => setServings((s) => Math.min(MAX_SERVINGS, s + 1))}
              className="flex h-9 w-9 cursor-pointer items-center justify-center rounded-[8px] text-action hover:bg-saffron-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              <Plus className="h-4.5 w-4.5" strokeWidth={2} aria-hidden />
            </button>
          </div>
        </div>
        <NumberField
          id="yield-total"
          label="Выход готового блюда"
          value={quantityTotal}
          onChange={setQuantityTotal}
          placeholder="2400"
          width="w-[170px]"
        />
        <div className="w-[170px]">
          <label htmlFor="yield-unit" className="mb-1.5 block text-sm font-medium text-ink">
            Единица выхода
          </label>
          <select
            id="yield-unit"
            value={yieldUnit}
            onChange={(e) => setYieldUnit(e.target.value as YieldUnit)}
            className="h-11 w-full cursor-pointer rounded-control border border-line bg-surface px-3 text-[15px] text-ink focus:border-action"
          >
            {YIELD_UNIT_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </div>
        <NumberField
          id="yield-per-serving"
          label="Вес порции, г"
          value={gramsPerServing}
          onChange={setGramsPerServing}
          placeholder="400"
          width="w-[150px]"
        />
      </div>
      <p className="mt-2 text-[13px] text-ink-muted">
        Порции — главное число: его использует калькулятор на странице блюда. Выход и вес порции —
        независимые подсказки читателю («получится 2,4 кг ≈ 6 порций по 400 г»), вес порции можно
        не указывать.
      </p>
      <SaveRow
        isLoading={isSavingYield || isSavingRecipe}
        isSuccess={isSuccess && !isSavingRecipe}
        isError={error !== undefined}
        onSave={handleSave}
      />
    </SectionCard>
  );
}

/** «Время приготовления» (UC-DSH-040): автосумма Prep+Cook+Rest либо ручное общее. */
export function TimingSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const t = recipe.timing;
  const [prep, setPrep] = useState(t.prepTimeMinutes !== null ? String(t.prepTimeMinutes) : '');
  const [cook, setCook] = useState(t.cookTimeMinutes !== null ? String(t.cookTimeMinutes) : '');
  const [rest, setRest] = useState(t.restTimeMinutes !== null ? String(t.restTimeMinutes) : '');
  const [active, setActive] = useState(t.activeTimeMinutes !== null ? String(t.activeTimeMinutes) : '');
  // У свежего рецепта дефолт isTotalManual=true при total=0 — это «не задано»,
  // а не осознанный ручной режим; стартуем с автосуммы, чтобы пустое ручное
  // поле не сохранило нулевое общее время.
  const [isManual, setIsManual] = useState(t.isTotalManual && t.totalTimeMinutes > 0);
  const [manualTotal, setManualTotal] = useState(t.totalTimeMinutes > 0 ? String(t.totalTimeMinutes) : '');

  const [save, { isLoading, isSuccess, error }] = useSetRecipeTimingMutation();

  const autoTotal = (numOrNull(prep) ?? 0) + (numOrNull(cook) ?? 0) + (numOrNull(rest) ?? 0);
  const effectiveTotal = isManual ? (numOrNull(manualTotal) ?? 0) : autoTotal;

  return (
    <SectionCard title="Время приготовления">
      <div className="flex flex-wrap gap-4">
        <NumberField id="timing-prep" label="Подготовка, мин" value={prep} onChange={setPrep} width="w-[150px]" />
        <NumberField id="timing-cook" label="Готовка, мин" value={cook} onChange={setCook} width="w-[150px]" />
        <NumberField id="timing-rest" label="Отдых, мин" value={rest} onChange={setRest} width="w-[150px]" />
        <NumberField id="timing-active" label="Активное, мин" value={active} onChange={setActive} width="w-[150px]" />
      </div>
      <p className="tabular mt-4 text-[15px]">
        Общее время: <span className="font-semibold">{effectiveTotal > 0 ? formatMinutes(effectiveTotal) : '—'}</span>
        {!isManual && <span className="text-ink-muted"> — считается автоматически</span>}
      </p>
      <div className="mt-3 flex flex-wrap items-end gap-4">
        <label className="flex cursor-pointer items-center gap-2.5 pb-2.5 text-[15px]">
          <input
            type="checkbox"
            checked={isManual}
            onChange={(e) => setIsManual(e.target.checked)}
            className="h-4.5 w-4.5 cursor-pointer accent-[#C25423]"
          />
          Указать общее время вручную
        </label>
        <NumberField
          id="timing-total"
          label="Общее, мин"
          value={isManual ? manualTotal : String(autoTotal || '')}
          onChange={setManualTotal}
          placeholder="90"
          disabled={!isManual}
        />
      </div>
      <p className="mt-2 text-[13px] text-ink-muted">
        Активное время — сколько повар занят руками; в общее время оно не суммируется.
      </p>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() =>
          save({
            dishId,
            prepTimeMinutes: numOrNull(prep),
            cookTimeMinutes: numOrNull(cook),
            restTimeMinutes: numOrNull(rest),
            activeTimeMinutes: numOrNull(active),
            totalTimeMinutes: effectiveTotal,
            isTotalManual: isManual,
          })
        }
      />
    </SectionCard>
  );
}

/**
 * «Общее о рецепте» (UC-DSH-011). Поля «порций по умолчанию» здесь нет —
 * единый контрол порций живёт в «Выходе и порциях»; при сохранении секция
 * шлёт servingsDefault из текущего серверного состояния рецепта.
 */
export function GeneralSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const [introduction, setIntroduction] = useState(recipe.introductionText ?? '');
  const [isAlcoholic, setIsAlcoholic] = useState(recipe.isAlcoholic);
  const [tips, setTips] = useState(recipe.authorTips ?? '');
  const [suggestions, setSuggestions] = useState(recipe.servingSuggestions ?? '');
  const [notes, setNotes] = useState(recipe.notes ?? '');

  const [save, { isLoading, isSuccess, error }] = useUpdateRecipeMutation();

  return (
    <SectionCard title="Общее о рецепте">
      <div className="flex flex-col gap-4">
        <GeneralTextArea
          id="recipe-intro"
          label="Вступление"
          rows={3}
          value={introduction}
          onChange={setIntroduction}
          placeholder="Пара тёплых фраз перед списком ингредиентов"
        />
        <label className="flex cursor-pointer items-center gap-2.5 text-[15px]">
          <input
            type="checkbox"
            checked={isAlcoholic}
            onChange={(e) => setIsAlcoholic(e.target.checked)}
            className="h-4.5 w-4.5 cursor-pointer accent-[#C25423]"
          />
          Содержит алкоголь
        </label>
        <GeneralTextArea
          id="recipe-tips"
          label="Советы автора"
          rows={2}
          value={tips}
          onChange={setTips}
          placeholder="Что важно не упустить"
        />
        <GeneralTextArea
          id="recipe-suggestions"
          label="Подача"
          rows={2}
          value={suggestions}
          onChange={setSuggestions}
          placeholder="С чем и как подавать"
        />
        <GeneralTextArea
          id="recipe-notes"
          label="Заметки"
          rows={2}
          value={notes}
          onChange={setNotes}
          placeholder="Личные заметки к рецепту"
        />
      </div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() =>
          save({
            dishId,
            introductionText: introduction.trim() || null,
            servingsDefault: recipe.servingsDefault,
            isAlcoholic,
            authorTips: tips.trim() || null,
            servingSuggestions: suggestions.trim() || null,
            notes: notes.trim() || null,
          })
        }
      />
    </SectionCard>
  );
}

/** Textarea со счётчиком /4000 для «Общего о рецепте». */
function GeneralTextArea({
  id,
  label,
  rows,
  value,
  onChange,
  placeholder,
}: {
  id: string;
  label: string;
  rows: number;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
}) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-ink">
        {label}
      </label>
      <textarea
        id={id}
        rows={rows}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
      />
      <p className={`tabular mt-1.5 text-[13px] ${value.length > MAX_TEXT ? 'text-danger-text' : 'text-ink-muted'}`}>
        {value.length}/{MAX_TEXT}
      </p>
    </div>
  );
}

/** «Пищевая ценность» (UC-DSH-042), опциональная: 8 значений + метод расчёта. */
export function NutritionSection({ dishId, recipe }: { dishId: string; recipe: RecipeViewDto }) {
  const n = recipe.nutrition;
  const [calcMethod, setCalcMethod] = useState<NutritionCalcMethod>(n?.calcMethod ?? 'Per100g');
  const [values, setValues] = useState<Record<string, string>>({
    calories: n ? String(n.calories) : '',
    proteins: n ? String(n.proteins) : '',
    fats: n ? String(n.fats) : '',
    saturatedFats: n?.saturatedFats !== null && n !== null ? String(n.saturatedFats) : '',
    carbs: n ? String(n.carbs) : '',
    sugar: n?.sugar !== null && n !== null ? String(n.sugar) : '',
    fiber: n?.fiber !== null && n !== null ? String(n.fiber) : '',
    salt: n?.salt !== null && n !== null ? String(n.salt) : '',
  });

  const [save, { isLoading, isSuccess, error }] = useSetRecipeNutritionMutation();

  const fields: ReadonlyArray<{ key: string; label: string }> = [
    { key: 'calories', label: 'Калории, ккал' },
    { key: 'proteins', label: 'Белки, г' },
    { key: 'fats', label: 'Жиры, г' },
    { key: 'saturatedFats', label: 'Насыщенные жиры, г' },
    { key: 'carbs', label: 'Углеводы, г' },
    { key: 'sugar', label: 'Сахар, г' },
    { key: 'fiber', label: 'Клетчатка, г' },
    { key: 'salt', label: 'Соль, г' },
  ];

  return (
    <SectionCard
      title={
        <>
          Пищевая ценность <span className="text-[15px] font-normal text-ink-muted">— необязательно</span>
        </>
      }
    >
      <div className="mb-4 flex w-fit gap-1 rounded-pill bg-sunken p-1">
        {(
          [
            { value: 'Per100g', label: 'на 100 г' },
            { value: 'PerServing', label: 'на порцию' },
          ] as const
        ).map((tab) => (
          <button
            key={tab.value}
            type="button"
            onClick={() => setCalcMethod(tab.value)}
            className={`cursor-pointer rounded-pill px-4 py-1.5 text-sm font-medium ${
              calcMethod === tab.value ? 'bg-surface text-ink shadow-chip' : 'text-ink-secondary'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {fields.map((field) => (
          <div key={field.key}>
            <label htmlFor={`nutri-${field.key}`} className="mb-1.5 block text-sm font-medium text-ink">
              {field.label}
            </label>
            <input
              id={`nutri-${field.key}`}
              type="number"
              min="0"
              step="any"
              value={values[field.key]}
              onChange={(e) => setValues((v) => ({ ...v, [field.key]: e.target.value }))}
              className="tabular h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink focus:border-action"
            />
          </div>
        ))}
      </div>
      <p className="mt-3 text-[13px] text-ink-muted">
        Насыщенные — часть жиров, сахар — часть углеводов.
      </p>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() =>
          save({
            dishId,
            calcMethod,
            calories: numOrZero(values.calories),
            proteins: numOrZero(values.proteins),
            fats: numOrZero(values.fats),
            saturatedFats: numOrNull(values.saturatedFats),
            carbs: numOrZero(values.carbs),
            sugar: numOrNull(values.sugar),
            fiber: numOrNull(values.fiber),
            salt: numOrNull(values.salt),
          })
        }
      />
    </SectionCard>
  );
}
