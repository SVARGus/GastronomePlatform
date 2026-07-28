import { Clock, ExternalLink, Info, Minus, Plus, Thermometer, Timer, Users, Wine } from 'lucide-react';
import { useMemo, useState } from 'react';
import { mediaThumbnailUrl } from '../../shared/api/media';
import type {
  RecipeIngredientViewDto,
  RecipeStepViewDto,
  RecipeViewDto,
} from '../../shared/api/types/dishes';
import {
  formatMinutes,
  formatQuantity,
  nutritionCalcMethodLabels,
  pluralize,
  yieldUnitLabels,
} from '../../shared/lib/labels';
import {
  useIngredientByIdQuery,
  useMeasureUnitsQuery,
  useScaledIngredientsQuery,
} from '../dishes/dishesApi';

const MIN_SERVINGS = 1;
const MAX_SERVINGS = 1000;

interface RecipeViewProps {
  dishId: string;
  recipe: RecipeViewDto;
  /** Для автора/admin: true — в рабочем слое есть неопубликованные правки. */
  hasUnsavedChanges: boolean | null;
}

/**
 * Полный рецепт (фрейм C брифа 3): сводка времени/порций/выхода, вводка,
 * ингредиенты с калькулятором порций (UC-DSH-056), шаги, КБЖУ, советы и подача.
 */
export function RecipeView({ dishId, recipe, hasUnsavedChanges }: RecipeViewProps) {
  const [servings, setServings] = useState(recipe.servingsDefault);

  const { data: units } = useMeasureUnitsQuery();
  const unitNames = useMemo(() => new Map((units ?? []).map((u) => [u.id, u.nameRu])), [units]);

  // На «родном» числе порций API не дёргаем — количества уже в рецепте.
  const isDefaultServings = servings === recipe.servingsDefault;
  const {
    data: scaled,
    error: scaledError,
    isFetching: isScaling,
  } = useScaledIngredientsQuery({ dishId, servings }, { skip: isDefaultServings });

  const scaledById = useMemo(
    () => new Map((scaled?.ingredients ?? []).map((i) => [i.id, i.scaledQuantity])),
    [scaled],
  );

  const timingParts = [
    recipe.timing.prepTimeMinutes !== null && `подготовка ${formatMinutes(recipe.timing.prepTimeMinutes)}`,
    recipe.timing.cookTimeMinutes !== null && `готовка ${formatMinutes(recipe.timing.cookTimeMinutes)}`,
    recipe.timing.restTimeMinutes !== null && `отдых ${formatMinutes(recipe.timing.restTimeMinutes)}`,
  ].filter(Boolean) as string[];

  return (
    <div className="mt-10">
      {hasUnsavedChanges === true && (
        <p className="mb-4 flex items-start gap-2 rounded-card bg-warning-bg p-4 text-sm text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          В рабочей версии есть неопубликованные правки — посетители видят опубликованную версию.
        </p>
      )}

      {/* Сводка рецепта */}
      <div className="rounded-card bg-surface p-6 shadow-card">
        <div className="flex flex-wrap items-center gap-x-8 gap-y-3">
          <span className="tabular inline-flex items-center gap-2 font-medium text-ink">
            <Clock className="h-5 w-5 text-ink-secondary" strokeWidth={1.75} aria-hidden />
            Общее время {formatMinutes(recipe.timing.totalTimeMinutes)}
          </span>
          <span className="tabular inline-flex items-center gap-2 font-medium text-ink">
            <Users className="h-5 w-5 text-ink-secondary" strokeWidth={1.75} aria-hidden />
            {recipe.yield.servingsCount} {pluralize(recipe.yield.servingsCount, ['порция', 'порции', 'порций'])}
          </span>
          <span className="tabular font-medium text-ink">
            выход {formatQuantity(recipe.yield.quantityTotal)} {yieldUnitLabels[recipe.yield.yieldUnit]}
          </span>
          {recipe.yield.gramsPerServing !== null && (
            <span className="tabular text-sm text-ink-secondary">
              порция ≈ {formatQuantity(recipe.yield.gramsPerServing)} г
            </span>
          )}
        </div>
        {timingParts.length > 0 && (
          <p className="tabular mt-2 text-sm text-ink-secondary">{timingParts.join(' · ')}</p>
        )}
        {recipe.isAlcoholic && (
          <p className="mt-3 inline-flex items-center gap-2 rounded-pill bg-warning-bg px-3 py-1.5 text-sm font-medium text-warning-text">
            <Wine className="h-4 w-4" strokeWidth={1.75} aria-hidden />
            Содержит алкоголь
          </p>
        )}
      </div>

      {recipe.introductionText && (
        <p className="mt-6 whitespace-pre-line text-ink-secondary">{recipe.introductionText}</p>
      )}

      {/* Ингредиенты + калькулятор порций */}
      <section className="mt-10">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h2 className="text-2xl font-[540]">Ингредиенты</h2>
          <div className="flex items-center gap-3">
            <div className="flex items-center rounded-control border border-line bg-surface">
              <button
                type="button"
                onClick={() => setServings((s) => Math.max(MIN_SERVINGS, s - 1))}
                disabled={servings <= MIN_SERVINGS}
                aria-label="Меньше порций"
                className="flex h-10 w-10 items-center justify-center rounded-l-control text-ink hover:bg-sunken disabled:text-ink-disabled"
              >
                <Minus className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </button>
              <span className="tabular min-w-10 text-center font-medium">{servings}</span>
              <button
                type="button"
                onClick={() => setServings((s) => Math.min(MAX_SERVINGS, s + 1))}
                disabled={servings >= MAX_SERVINGS}
                aria-label="Больше порций"
                className="flex h-10 w-10 items-center justify-center rounded-r-control text-ink hover:bg-sunken disabled:text-ink-disabled"
              >
                <Plus className="h-4 w-4" strokeWidth={1.75} aria-hidden />
              </button>
            </div>
            <span className="text-sm text-ink-secondary">
              {pluralize(servings, ['порция', 'порции', 'порций'])}
            </span>
          </div>
        </div>

        {scaledError !== undefined && !isDefaultServings && (
          <p className="mt-3 text-sm text-ink-muted">
            Пересчёт порций недоступен — показаны количества на {recipe.servingsDefault}{' '}
            {pluralize(recipe.servingsDefault, ['порцию', 'порции', 'порций'])}.
          </p>
        )}

        <ul className={`mt-5 divide-y divide-line rounded-card bg-surface px-6 py-2 shadow-card ${isScaling ? 'opacity-60' : ''}`}>
          {recipe.ingredients.map((ingredient) => (
            <IngredientRow
              key={ingredient.id}
              ingredient={ingredient}
              quantity={
                !isDefaultServings && scaledError === undefined
                  ? scaledById.get(ingredient.id) ?? ingredient.quantity
                  : ingredient.quantity
              }
              unitName={unitNames.get(ingredient.measureUnitId) ?? ''}
            />
          ))}
        </ul>
      </section>

      {/* Шаги приготовления */}
      <section className="mt-10">
        <h2 className="text-2xl font-[540]">Приготовление</h2>
        <ol className="mt-5 space-y-6">
          {recipe.steps.map((step) => (
            <StepCard key={step.id} step={step} />
          ))}
        </ol>
      </section>

      {/* Пищевая ценность */}
      {recipe.nutrition && (
        <section className="mt-10">
          <h2 className="text-2xl font-[540]">Пищевая ценность</h2>
          <div className="mt-4 rounded-card bg-surface p-6 shadow-card">
            <dl className="tabular flex flex-wrap gap-x-10 gap-y-3">
              <NutritionItem label="Калории" value={`${formatQuantity(recipe.nutrition.calories)} ккал`} />
              <NutritionItem label="Белки" value={`${formatQuantity(recipe.nutrition.proteins)} г`} />
              <NutritionItem label="Жиры" value={`${formatQuantity(recipe.nutrition.fats)} г`} />
              <NutritionItem label="Углеводы" value={`${formatQuantity(recipe.nutrition.carbs)} г`} />
            </dl>
            <p className="mt-3 text-sm text-ink-secondary">
              Указано {nutritionCalcMethodLabels[recipe.nutrition.calcMethod]}.
            </p>
          </div>
        </section>
      )}

      {/* Советы, подача, заметки */}
      {recipe.authorTips && (
        <section className="mt-10">
          <h2 className="text-2xl font-[540]">Советы автора</h2>
          <p className="mt-3 whitespace-pre-line text-ink-secondary">{recipe.authorTips}</p>
        </section>
      )}
      {recipe.servingSuggestions && (
        <section className="mt-8">
          <h2 className="text-2xl font-[540]">Подача</h2>
          <p className="mt-3 whitespace-pre-line text-ink-secondary">{recipe.servingSuggestions}</p>
        </section>
      )}
      {recipe.notes && (
        <p className="mt-8 whitespace-pre-line text-sm text-ink-muted">{recipe.notes}</p>
      )}
    </div>
  );
}

interface IngredientRowProps {
  ingredient: RecipeIngredientViewDto;
  quantity: number;
  unitName: string;
}

/** Строка ингредиента: имя (для catalog — из справочника), количество справа. */
function IngredientRow({ ingredient, quantity, unitName }: IngredientRowProps) {
  return (
    <li className="flex items-baseline justify-between gap-4 py-3">
      <span className="min-w-0">
        <span className="text-ink">
          {ingredient.type === 'catalog' ? (
            <CatalogIngredientName ingredientId={ingredient.ingredientId} />
          ) : (
            ingredient.freeformText
          )}
        </span>
        {ingredient.isOptional && <span className="ml-2 text-sm text-ink-secondary">по желанию</span>}
        {ingredient.preparationNote && (
          <span className="ml-2 text-sm italic text-ink-muted">{ingredient.preparationNote}</span>
        )}
      </span>
      <span className="tabular shrink-0 font-medium text-ink">
        {formatQuantity(quantity)} {unitName}
      </span>
    </li>
  );
}

/** Имя catalog-ингредиента через UC-DSH-063 (RTK Query кэширует по id). */
function CatalogIngredientName({ ingredientId }: { ingredientId: string }) {
  const { data, isError } = useIngredientByIdQuery(ingredientId);
  if (isError) return <>Ингредиент</>;
  if (!data) return <span className="inline-block h-4 w-24 animate-pulse rounded-pill bg-sunken align-middle" />;
  return <>{data.name}</>;
}

/** Шаг приготовления: номер-кружок, текст, опциональные фото/бейджи/видео. */
function StepCard({ step }: { step: RecipeStepViewDto }) {
  return (
    <li className="flex gap-4">
      <span
        aria-hidden
        className="tabular flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-saffron-50 font-medium text-action"
      >
        {step.order}
      </span>
      <div className="min-w-0 flex-1 pt-1">
        {step.title && <h3 className="font-semibold text-ink">{step.title}</h3>}
        <p className={`whitespace-pre-line text-ink-secondary ${step.title ? 'mt-1' : ''}`}>{step.description}</p>
        {step.imageMediaId && (
          <img
            src={mediaThumbnailUrl(step.imageMediaId)}
            alt={step.title ?? `Шаг ${step.order}`}
            loading="lazy"
            className="mt-3 h-[180px] w-[180px] rounded-full object-cover"
          />
        )}
        {(step.temperatureCelsius !== null || step.timerMinutes !== null || step.videoUrl) && (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            {step.temperatureCelsius !== null && (
              <span className="tabular inline-flex items-center gap-1.5 rounded-pill border border-line bg-surface px-3 py-1 text-sm text-ink shadow-chip">
                <Thermometer className="h-3.5 w-3.5 text-ink-secondary" strokeWidth={1.75} aria-hidden />
                {step.temperatureCelsius}°C
              </span>
            )}
            {step.timerMinutes !== null && (
              <span className="tabular inline-flex items-center gap-1.5 rounded-pill border border-line bg-surface px-3 py-1 text-sm text-ink shadow-chip">
                <Timer className="h-3.5 w-3.5 text-ink-secondary" strokeWidth={1.75} aria-hidden />
                {formatMinutes(step.timerMinutes)}
              </span>
            )}
            {step.videoUrl && (
              <a
                href={step.videoUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-sm font-medium text-link hover:text-link-hover"
              >
                Видео этапа
                <ExternalLink className="h-3.5 w-3.5" strokeWidth={1.75} aria-hidden />
              </a>
            )}
          </div>
        )}
      </div>
    </li>
  );
}

/** Ячейка КБЖУ: подпись вторичным цветом, значение — числом. */
function NutritionItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-sm text-ink-secondary">{label}</dt>
      <dd className="mt-0.5 font-medium text-ink">{value}</dd>
    </div>
  );
}
