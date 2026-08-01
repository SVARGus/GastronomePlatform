import { Link, useLocation } from 'react-router-dom';
import { useDishRecipeQuery } from '../dishes/dishesApi';
import { RecipeView } from './RecipeView';

/**
 * Блок рецепта с Premium-гейтом (UC-DSH-052). Ответ backend определяет
 * состояние: 401 — гость (тизер «Войти»), 403 DISHES.PREMIUM_REQUIRED —
 * вошёл без подписки (тизер «Шафран»), 200 — полный рецепт.
 * Публичная часть страницы от состояния не зависит.
 */
export function RecipeGate({ dishId }: { dishId: string }) {
  const { data, error, isLoading } = useDishRecipeQuery({ dishId });
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="mt-10 animate-pulse rounded-card bg-surface p-8 shadow-card">
        <div className="h-5 w-64 rounded-pill bg-sunken" />
        <div className="mt-4 h-4 w-full rounded-pill bg-sunken" />
        <div className="mt-2 h-4 w-3/4 rounded-pill bg-sunken" />
      </div>
    );
  }

  if (data) {
    return (
      <RecipeView dishId={dishId} recipe={data.recipe} hasUnsavedChanges={data.hasUnsavedChanges} />
    );
  }

  const status = error && 'status' in error ? error.status : null;

  if (status === 401) {
    return (
      <RecipeTeaser
        title="Полный рецепт — для подписчиков"
        text="Войдите и оформите подписку, чтобы открыть ингредиенты, шаги и расчёт порций."
        action={
          <div className="flex flex-wrap items-center justify-center gap-3">
            <Link
              to="/login"
              state={{ from: `${location.pathname}${location.search}` }}
              className="inline-flex h-11 items-center justify-center rounded-control bg-action px-5 font-medium text-on-action hover:bg-action-hover hover:text-on-action"
            >
              Войти
            </Link>
            <Link to="/pricing" className="px-3 py-2 font-medium text-link hover:text-link-hover">
              Посмотреть тарифы
            </Link>
          </div>
        }
      />
    );
  }

  if (status === 403) {
    return (
      <RecipeTeaser
        badge="Шафран"
        title="Рецепт открывается подпиской Шафран"
        text="В подписке: полные рецепты с пошаговыми фото и расчёт порций."
        action={
          <Link
            to="/pricing"
            className="inline-flex h-11 items-center justify-center rounded-control bg-action px-5 font-medium text-on-action hover:bg-action-hover hover:text-on-action"
          >
            Посмотреть тарифы
          </Link>
        }
      />
    );
  }

  return (
    <p className="mt-10 rounded-card bg-surface p-8 text-center text-ink-secondary shadow-card">
      Не получилось загрузить рецепт. Обновите страницу или зайдите чуть позже.
    </p>
  );
}

interface RecipeTeaserProps {
  badge?: string;
  title: string;
  text: string;
  action: React.ReactNode;
}

/** Карточка-тизер закрытого рецепта: тарелка с «паром», текст, действие. */
function RecipeTeaser({ badge, title, text, action }: RecipeTeaserProps) {
  return (
    <div className="mt-10 flex flex-col items-center rounded-card bg-surface px-8 py-12 text-center shadow-card">
      <svg viewBox="0 0 120 90" className="h-24 w-32 text-ink-muted" aria-hidden>
        <g fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
          <path d="M42 28 C39 22, 44 18, 41 12" className="text-saffron-500" stroke="currentColor" />
          <path d="M60 26 C57 20, 62 16, 59 10" />
          <path d="M78 28 C75 22, 80 18, 77 12" />
          <ellipse cx="60" cy="62" rx="46" ry="16" />
          <ellipse cx="60" cy="60" rx="28" ry="9" />
        </g>
      </svg>
      {badge && (
        <span className="mt-5 inline-flex items-center gap-1.5 rounded-pill bg-gold-bg px-3 py-1 text-sm font-medium text-gold-text">
          <span aria-hidden className="h-1.5 w-1.5 rounded-full bg-gold" />
          {badge}
        </span>
      )}
      <h2 className="mt-4 text-2xl font-[540]">{title}</h2>
      <p className="mt-2 max-w-[460px] text-ink-secondary">{text}</p>
      <div className="mt-6">{action}</div>
    </div>
  );
}
