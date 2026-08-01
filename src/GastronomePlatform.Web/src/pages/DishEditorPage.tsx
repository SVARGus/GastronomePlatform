import { ArrowLeft, ExternalLink, Info } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { getErrorMessage, getValidationError } from '../features/auth/apiErrors';
import { useCreateDishMutation, useDishByIdQuery } from '../features/dishes/dishesApi';
import {
  CategoriesSection,
  DietSection,
  HistorySection,
  MainSection,
  PhotoSection,
  TagsSection,
} from '../features/dishEditor/EditorSections';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import type { CostEstimate, DifficultyLevel, DishStatus } from '../shared/api/types/dishes';
import { costOptions, difficultyOptions } from '../shared/lib/labels';
import { Button } from '../shared/ui/Button';
import { TextField } from '../shared/ui/TextField';

/**
 * Редактор публичной карточки блюда (приоритет 3, бриф 9). Два режима одного
 * маршрута-семейства: /account/dishes/new — короткая форма создания черновика
 * (UC-DSH-001), /account/dishes/:id/edit — карточки-секции с независимым
 * сохранением (UC-DSH-002/007/008/009/010/011). Рецепт (шаги, тайминг,
 * ингредиенты) редактор пока не трогает — это отдельная страница будущего этапа.
 */
export function DishEditorPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  // Редактор только для вошедших — гость уходит на вход с возвратом сюда.
  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: location.pathname } });
    }
  }, [isAuthenticated, navigate, location.pathname]);

  if (!isAuthenticated) return null;

  return (
    <div className="mx-auto w-full max-w-[760px] px-4 py-6 md:px-6 md:py-8">
      <Link
        to="/account?tab=dishes"
        className="inline-flex items-center gap-1.5 text-[15px] font-medium text-ink-secondary hover:text-ink"
      >
        <ArrowLeft className="h-4 w-4" strokeWidth={1.75} aria-hidden />
        Мои блюда
      </Link>
      {id ? <EditMode dishId={id} /> : <CreateMode />}
    </div>
  );
}

/** Режим создания: минимум полей, после POST — переход в полный редактор. */
function CreateMode() {
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [difficulty, setDifficulty] = useState<DifficultyLevel>('Easy');
  const [cost, setCost] = useState<CostEstimate>('Budget');
  const [shortDescription, setShortDescription] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | null>(null);

  const [createDish, { isLoading }] = useCreateDishMutation();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    setNameError(null);
    try {
      const { id } = await createDish({
        name: name.trim(),
        difficultyLevel: difficulty,
        costEstimate: cost,
        shortDescription: shortDescription.trim() || null,
        description: null,
        dietLabelsMask: null,
        historyText: null,
      }).unwrap();
      navigate(`/account/dishes/${id}/edit`, { replace: true });
    } catch (err) {
      const fieldError = getValidationError(err, 'Name');
      if (fieldError) {
        setNameError(fieldError);
      } else {
        setFormError(getErrorMessage(err) ?? 'Не получилось создать блюдо — попробуйте ещё раз.');
      }
    }
  }

  return (
    <>
      <h1 className="mt-4 text-[30px] font-[560] leading-[1.2] md:text-[34px]">Новое блюдо</h1>
      <p className="mt-2 text-[15px] text-ink-secondary">
        Начните с малого — название и пара строк. Фото, категории и рецепт добавите позже,
        блюдо сохранится черновиком.
      </p>
      <form
        onSubmit={handleSubmit}
        className="mt-6 flex flex-col gap-4 rounded-card border border-line bg-surface p-6 shadow-card"
      >
        <TextField
          label="Название"
          value={name}
          onChange={setName}
          placeholder="Например, «Драники с грибным соусом»"
          error={nameError}
          hint="3–200 символов."
          required
        />
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="flex-1">
            <label htmlFor="create-difficulty" className="mb-1.5 block text-sm font-medium text-ink">
              Сложность
            </label>
            <select
              id="create-difficulty"
              value={difficulty}
              onChange={(e) => setDifficulty(e.target.value as DifficultyLevel)}
              className="h-11 w-full cursor-pointer rounded-control border border-line bg-surface px-3 text-[15px] text-ink focus:border-action"
            >
              {difficultyOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
          <div className="flex-1">
            <label htmlFor="create-cost" className="mb-1.5 block text-sm font-medium text-ink">
              Стоимость
            </label>
            <select
              id="create-cost"
              value={cost}
              onChange={(e) => setCost(e.target.value as CostEstimate)}
              className="h-11 w-full cursor-pointer rounded-control border border-line bg-surface px-3 text-[15px] text-ink focus:border-action"
            >
              {costOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div>
          <label htmlFor="create-short" className="mb-1.5 block text-sm font-medium text-ink">
            Короткое описание <span className="font-normal text-ink-muted">— необязательно</span>
          </label>
          <textarea
            id="create-short"
            rows={2}
            value={shortDescription}
            onChange={(e) => setShortDescription(e.target.value)}
            placeholder="Одна фраза, которая появится в каталоге"
            className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
        </div>
        {formError && (
          <p className="flex items-start gap-2.5 rounded-control bg-danger-bg p-3 text-sm text-danger-text">
            <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
            {formError}
          </p>
        )}
        <div>
          <Button type="submit" variant="primary" disabled={isLoading || name.trim().length < 3}>
            {isLoading ? 'Создаём…' : 'Создать черновик'}
          </Button>
        </div>
      </form>
    </>
  );
}

/** Режим редактирования: заголовок со статусом + шесть независимых секций. */
function EditMode({ dishId }: { dishId: string }) {
  const { data: dish, isLoading, error } = useDishByIdQuery(dishId);

  if (isLoading) {
    return (
      <div className="mt-6 flex flex-col gap-5">
        {Array.from({ length: 3 }, (_, i) => (
          <div key={i} className="h-40 animate-pulse rounded-card bg-sunken" />
        ))}
      </div>
    );
  }

  if (error || !dish) {
    return (
      <div className="mt-10 text-center">
        <h1 className="text-[26px] font-[560]">Блюдо не найдено</h1>
        <p className="mt-2 text-[15px] text-ink-secondary">
          Возможно, оно было архивировано или принадлежит другому автору.
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <h1 className="min-w-0 text-[30px] font-[560] leading-[1.2] md:text-[34px]">{dish.name}</h1>
        <EditorStatusBadge status={dish.status} />
      </div>

      {dish.status === 'Published' && (
        <p className="mt-4 flex items-start gap-2.5 rounded-control bg-warning-bg p-3.5 text-sm text-warning-text">
          <Info className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={1.75} aria-hidden />
          <span>
            Блюдо опубликовано. Вы редактируете рабочую версию — на витрине изменения появятся
            после повторной публикации из раздела{' '}
            <Link to="/account?tab=dishes" className="font-medium underline">
              «Мои блюда»
            </Link>
            .
            {'  '}
            <Link to={`/dishes/${dish.slug}`} className="inline-flex items-center gap-1 font-medium underline">
              Открыть на витрине
              <ExternalLink className="h-3.5 w-3.5" strokeWidth={1.75} aria-hidden />
            </Link>
          </span>
        </p>
      )}

      <div className="mt-6 flex flex-col gap-5">
        <MainSection dish={dish} />
        <PhotoSection dish={dish} />
        <CategoriesSection dish={dish} />
        <TagsSection dish={dish} />
        <DietSection dish={dish} />
        <HistorySection dish={dish} />
      </div>
    </>
  );
}

/** Бейдж статуса в заголовке редактора. */
function EditorStatusBadge({ status }: { status: DishStatus }) {
  if (status === 'Published') {
    return (
      <span className="rounded-pill bg-success-bg px-2.5 py-0.5 text-[13px] font-medium text-success-text">
        Опубликовано
      </span>
    );
  }
  if (status === 'Unpublished') {
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
