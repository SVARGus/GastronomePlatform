import { ArrowLeft, Check, Circle, Info } from 'lucide-react';
import { useEffect } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { useDishByIdQuery, useDishRecipeQuery } from '../features/dishes/dishesApi';
import { IngredientsSection } from '../features/recipeEditor/IngredientsSection';
import { StepsSection } from '../features/recipeEditor/StepsSection';
import {
  GeneralSection,
  NutritionSection,
  TimingSection,
  YieldSection,
} from '../features/recipeEditor/RecipeSections';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import type { DishDetailDto, RecipeViewDto } from '../shared/api/types/dishes';

/**
 * Редактор рецепта (приоритет 3, бриф 10): ингредиенты, выход/порции, шаги,
 * тайминг, общее, КБЖУ. Порядок секций — по связности данных: дозировки
 * ингредиентов ↔ порции. Публикация — из «Моих блюд»; здесь только чек-лист
 * готовности (доменные требования UC-DSH-004).
 */
export function RecipeEditorPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: location.pathname } });
    }
  }, [isAuthenticated, navigate, location.pathname]);

  const dishId = id ?? '';
  // Оба запроса — рабочая версия: редактор правит рабочий слой и должен его видеть.
  const { data: dish, isLoading: dishLoading, error: dishError } = useDishByIdQuery(
    { id: dishId, version: 'working' },
    { skip: !isAuthenticated || !dishId },
  );
  const { data: recipeData, isLoading: recipeLoading, error: recipeError } = useDishRecipeQuery(
    { dishId, version: 'working' },
    { skip: !isAuthenticated || !dishId },
  );

  if (!isAuthenticated) return null;

  const isLoading = dishLoading || recipeLoading;

  return (
    <div className="mx-auto w-full max-w-[820px] px-4 py-6 md:px-6 md:py-8">
      <nav className="flex flex-wrap items-center gap-2 text-[15px] font-medium">
        <Link to="/account?tab=dishes" className="text-ink-secondary hover:text-ink">
          Мои блюда
        </Link>
        <span className="text-ink-muted">→</span>
        <Link
          to={`/account/dishes/${dishId}/edit`}
          className="inline-flex items-center gap-1.5 text-ink-secondary hover:text-ink"
        >
          <ArrowLeft className="h-4 w-4" strokeWidth={1.75} aria-hidden />
          К карточке блюда
        </Link>
      </nav>

      {isLoading && (
        <div className="mt-6 flex flex-col gap-5">
          {Array.from({ length: 3 }, (_, i) => (
            <div key={i} className="h-44 animate-pulse rounded-card bg-sunken" />
          ))}
        </div>
      )}

      {!isLoading && (dishError || recipeError || !dish || !recipeData) && (
        <div className="mt-10 text-center">
          <h1 className="text-[26px] font-[560]">Рецепт недоступен</h1>
          <p className="mt-2 text-[15px] text-ink-secondary">
            Возможно, блюдо архивировано или принадлежит другому автору.
          </p>
        </div>
      )}

      {!isLoading && dish && recipeData && (
        <RecipeEditorBody dishId={dishId} dish={dish} recipe={recipeData.recipe} />
      )}
    </div>
  );
}

function RecipeEditorBody({
  dishId,
  dish,
  recipe,
}: {
  dishId: string;
  dish: DishDetailDto;
  recipe: RecipeViewDto;
}) {
  return (
    <>
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <h1 className="min-w-0 text-[30px] font-[560] leading-[1.2] md:text-[34px]">
          Рецепт: {dish.name}
        </h1>
        <RecipeStatusBadge dish={dish} />
      </div>

      {dish.status === 'Published' && (
        <p className="mt-4 flex items-start gap-2.5 rounded-control bg-warning-bg p-3.5 text-sm text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          Блюдо опубликовано. Вы редактируете рабочую версию — на витрине изменения появятся
          после повторной публикации из раздела «Мои блюда».
        </p>
      )}

      <ReadinessChecklist dish={dish} recipe={recipe} />

      <div className="mt-6 flex flex-col gap-5">
        <IngredientsSection dishId={dishId} recipe={recipe} />
        <YieldSection dishId={dishId} recipe={recipe} />
        <StepsSection dishId={dishId} recipe={recipe} />
        <TimingSection dishId={dishId} recipe={recipe} />
        <GeneralSection dishId={dishId} recipe={recipe} />
        <NutritionSection dishId={dishId} recipe={recipe} />
      </div>
    </>
  );
}

/** Чек-лист доменных требований публикации (UC-DSH-004): фото, состав, шаги, тайминг. */
function ReadinessChecklist({ dish, recipe }: { dish: DishDetailDto; recipe: RecipeViewDto }) {
  const items: ReadonlyArray<{ label: string; done: boolean }> = [
    { label: 'Главное фото', done: dish.mainImageId !== null },
    {
      label: recipe.ingredients.length > 0 ? `Ингредиенты — ${recipe.ingredients.length}` : 'Хотя бы один ингредиент',
      done: recipe.ingredients.length > 0,
    },
    {
      label: recipe.steps.length > 0 ? `Шаги — ${recipe.steps.length}` : 'Хотя бы один шаг',
      done: recipe.steps.length > 0,
    },
    { label: 'Общее время приготовления', done: recipe.timing.totalTimeMinutes > 0 },
  ];

  return (
    <div className="mt-5 rounded-card bg-sunken p-5">
      <h2 className="font-semibold">Готовность к публикации</h2>
      <ul className="mt-3 flex flex-col gap-2.5">
        {items.map((item) => (
          <li key={item.label} className="flex items-center gap-2.5 text-[15px]">
            {item.done ? (
              <Check className="h-[19px] w-[19px] shrink-0 text-success-text" strokeWidth={2} aria-hidden />
            ) : (
              <Circle className="h-[17px] w-[17px] shrink-0 text-ink-muted" strokeWidth={1.75} aria-hidden />
            )}
            <span className={item.done ? '' : 'text-ink-secondary'}>{item.label}</span>
          </li>
        ))}
      </ul>
      <p className="mt-3 text-[13px] text-ink-muted">
        Опубликовать блюдо можно из раздела{' '}
        <Link to="/account?tab=dishes" className="font-medium underline">
          «Мои блюда»
        </Link>
        , когда все пункты будут закрыты. Главное фото загружается в редакторе карточки.
      </p>
    </div>
  );
}

/** Бейдж статуса блюда в заголовке. */
function RecipeStatusBadge({ dish }: { dish: DishDetailDto }) {
  if (dish.status === 'Published') {
    return (
      <span className="rounded-pill bg-success-bg px-2.5 py-0.5 text-[13px] font-medium text-success-text">
        Опубликовано
      </span>
    );
  }
  if (dish.status === 'Unpublished') {
    return (
      <span className="rounded-pill bg-warning-bg px-2.5 py-0.5 text-[13px] font-medium text-warning-text">
        Снято с публикации
      </span>
    );
  }
  return (
    <span className="rounded-pill bg-sunken px-2.5 py-0.5 text-[13px] font-medium text-ink-secondary">
      Черновик
    </span>
  );
}
