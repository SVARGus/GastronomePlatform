import type {
  CostEstimate,
  DifficultyLevel,
  DishSearchSortBy,
  NutritionCalcMethod,
  YieldUnit,
} from '../api/types/dishes';

/**
 * Русские подписи enum-ов контракта. Сервер намеренно отдаёт значения enum,
 * а не готовый текст — формулировки живут на клиенте (см. брифы дизайна).
 */

export const difficultyLabels: Record<DifficultyLevel, string> = {
  Easy: 'лёгкая',
  Medium: 'средняя',
  Hard: 'сложная',
  Pro: 'профи',
};

/** Категория стоимости готовки (не цена заказа — заказов в MVP нет). */
export const costLabels: Record<CostEstimate, string> = {
  Budget: '₽',
  Moderate: '₽₽',
  Expensive: '₽₽₽',
};

export const sortLabels: Record<DishSearchSortBy, string> = {
  Newest: 'Сначала новые',
  ViewsDesc: 'Сначала популярные',
  RatingDesc: 'Сначала с высоким рейтингом',
};

/** Метки диет ([Flags] DietLabels backend): значение → подпись фильтра/бейджа. */
export const dietLabelOptions: ReadonlyArray<{ value: string; label: string }> = [
  { value: 'Vegetarian', label: 'Вегетарианское' },
  { value: 'Vegan', label: 'Веганское' },
  { value: 'GlutenFree', label: 'Без глютена' },
  { value: 'LactoseFree', label: 'Без лактозы' },
  { value: 'Halal', label: 'Халяль' },
  { value: 'Kosher', label: 'Кошерное' },
  { value: 'KetoFriendly', label: 'Кето' },
  { value: 'LowCarb', label: 'Низкоуглеводное' },
  { value: 'LowCalorie', label: 'Низкокалорийное' },
  { value: 'SugarFree', label: 'Без сахара' },
];

export const difficultyOptions: ReadonlyArray<{ value: DifficultyLevel; label: string }> = (
  ['Easy', 'Medium', 'Hard', 'Pro'] as const
).map((value) => ({ value, label: difficultyLabels[value] }));

export const costOptions: ReadonlyArray<{ value: CostEstimate; label: string }> = (
  ['Budget', 'Moderate', 'Expensive'] as const
).map((value) => ({ value, label: costLabels[value] }));

/** Русская плюрализация: pluralize(5, ['блюдо', 'блюда', 'блюд']) → «блюд». */
export function pluralize(count: number, forms: [string, string, string]): string {
  const abs = Math.abs(count) % 100;
  const last = abs % 10;
  if (abs > 10 && abs < 20) return forms[2];
  if (last > 1 && last < 5) return forms[1];
  if (last === 1) return forms[0];
  return forms[2];
}

/** Единицы выхода рецепта (YieldUnit) — короткие подписи после числа. */
export const yieldUnitLabels: Record<YieldUnit, string> = {
  Grams: 'г',
  Kilograms: 'кг',
  Milliliters: 'мл',
  Liters: 'л',
  Pieces: 'шт.',
  Servings: 'порц.',
};

/** Подпись способа расчёта КБЖУ. */
export const nutritionCalcMethodLabels: Record<NutritionCalcMethod, string> = {
  Per100g: 'на 100 г готового блюда',
  PerServing: 'на одну порцию',
};

/** Минуты → «1 ч 30 мин» / «45 мин» / «2 ч». */
export function formatMinutes(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  if (hours === 0) return `${minutes} мин`;
  if (minutes === 0) return `${hours} ч`;
  return `${hours} ч ${minutes} мин`;
}

/** Количество ингредиента: без хвостовых нулей, запятая как разделитель (ru-RU). */
export function formatQuantity(quantity: number): string {
  return quantity.toLocaleString('ru-RU', { maximumFractionDigits: 2 });
}
