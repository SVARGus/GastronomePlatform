import type { CostEstimate, DifficultyLevel } from '../api/types/dishes';

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
