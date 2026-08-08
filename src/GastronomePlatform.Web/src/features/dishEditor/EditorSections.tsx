import { Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import {
  useChangeMainImageMutation,
  useCategoryTreeQuery,
  useSetDishCategoriesMutation,
  useSetDishDietLabelsMutation,
  useSetDishHistoryMutation,
  useSetDishTagsMutation,
  useUpdateDishCardMutation,
} from '../dishes/dishesApi';
import { getErrorMessage } from '../auth/apiErrors';
import { useUploadFileMutation } from '../media/mediaApi';
import { mediaFileUrl, mediaThumbnailUrl } from '../../shared/api/media';
import type {
  CategoryNodeDto,
  CostEstimate,
  DifficultyLevel,
  DishDetailDto,
} from '../../shared/api/types/dishes';
import { costOptions, dietLabelOptions, difficultyOptions } from '../../shared/lib/labels';
import { Button } from '../../shared/ui/Button';
import { SaveRow } from '../../shared/ui/SaveRow';
import { TextField } from '../../shared/ui/TextField';
import { TagsInput } from './TagsInput';

const MAX_SHORT = 500;
const MAX_LONG = 4000;
const MAX_CATEGORIES = 3;

/** Пустая строка → null (backend трактует null как «очистить»). */
function orNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

/** Счётчик символов секции: «120/500», tabular. */
function CharCounter({ value, max }: { value: number; max: number }) {
  return (
    <p className={`tabular mt-1.5 text-[13px] ${value > max ? 'text-danger-text' : 'text-ink-muted'}`}>
      {value}/{max}
    </p>
  );
}

/** Обёртка карточки-секции редактора. */
function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">{title}</h2>
      <div className="mt-4">{children}</div>
    </section>
  );
}

/** «Основное»: название, сложность, стоимость, описания (UC-DSH-002). */
export function MainSection({ dish }: { dish: DishDetailDto }) {
  const [name, setName] = useState(dish.name);
  const [difficulty, setDifficulty] = useState<DifficultyLevel>(dish.difficultyLevel);
  const [cost, setCost] = useState<CostEstimate>(dish.costEstimate);
  const [shortDescription, setShortDescription] = useState(dish.shortDescription ?? '');
  const [description, setDescription] = useState(dish.description ?? '');

  const [save, { isLoading, isSuccess, error }] = useUpdateDishCardMutation();

  return (
    <SectionCard title="Основное">
      <div className="flex flex-col gap-4">
        <TextField label="Название" value={name} onChange={setName} hint="3–200 символов." required />
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="flex-1">
            <label htmlFor="editor-difficulty" className="mb-1.5 block text-sm font-medium text-ink">
              Сложность
            </label>
            <select
              id="editor-difficulty"
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
            <label htmlFor="editor-cost" className="mb-1.5 block text-sm font-medium text-ink">
              Стоимость
            </label>
            <select
              id="editor-cost"
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
          <label htmlFor="editor-short" className="mb-1.5 block text-sm font-medium text-ink">
            Короткое описание
          </label>
          <textarea
            id="editor-short"
            rows={2}
            value={shortDescription}
            onChange={(e) => setShortDescription(e.target.value)}
            placeholder="Одна фраза, которая появится в каталоге"
            className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
          <CharCounter value={shortDescription.length} max={MAX_SHORT} />
        </div>
        <div>
          <label htmlFor="editor-description" className="mb-1.5 block text-sm font-medium text-ink">
            Полное описание
          </label>
          <textarea
            id="editor-description"
            rows={5}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Расскажите о блюде подробнее"
            className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
          <CharCounter value={description.length} max={MAX_LONG} />
        </div>
      </div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() =>
          save({
            dishId: dish.id,
            name: name.trim(),
            difficultyLevel: difficulty,
            costEstimate: cost,
            shortDescription: orNull(shortDescription),
            description: orNull(description),
          })
        }
      />
    </SectionCard>
  );
}

/** «Главное фото»: загрузка в Media → PATCH main-image (сохранение мгновенное). */
export function PhotoSection({ dish }: { dish: DishDetailDto }) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadFile, { isLoading: isUploading }] = useUploadFileMutation();
  const [changeImage, { isLoading: isAttaching }] = useChangeMainImageMutation();
  const [error, setError] = useState<string | null>(null);

  const busy = isUploading || isAttaching;

  async function handleFile(file: File) {
    setError(null);
    try {
      const { mediaId } = await uploadFile({ file, intendedEntityType: 'Dish' }).unwrap();
      await changeImage({ dishId: dish.id, mainImageId: mediaId }).unwrap();
    } catch (err) {
      setError(
        getErrorMessage(err) ??
          'Не получилось загрузить фото. Проверьте формат (JPG/PNG) и размер (до 10 МБ).',
      );
    }
  }

  return (
    <SectionCard title="Главное фото">
      <div className="flex flex-wrap items-center gap-5">
        {dish.mainImageId ? (
          <a
            href={mediaFileUrl(dish.mainImageId)}
            target="_blank"
            rel="noreferrer"
            title="Открыть оригинал в новой вкладке"
            className="shrink-0"
          >
            <img
              src={mediaThumbnailUrl(dish.mainImageId)}
              alt=""
              className="h-[132px] w-[132px] rounded-full object-cover shadow-chip ring-[6px] ring-surface"
            />
          </a>
        ) : (
          <span aria-hidden className="h-[132px] w-[132px] shrink-0 rounded-full bg-saffron-100" />
        )}
        <div className="min-w-0">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) handleFile(file);
              e.target.value = '';
            }}
          />
          <div className="flex flex-wrap items-center gap-3">
            <Button variant="secondary" disabled={busy} onClick={() => fileInputRef.current?.click()}>
              <Upload className="mr-2 h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
              {busy ? 'Загружаем…' : 'Загрузить фото'}
            </Button>
            {dish.mainImageId && (
              <Button
                variant="ghost"
                disabled={busy}
                onClick={() => changeImage({ dishId: dish.id, mainImageId: null })}
              >
                Убрать
              </Button>
            )}
          </div>
          <p className="mt-2 text-[13px] text-ink-muted">
            JPG или PNG до 10 МБ. Фото обязательно для публикации.
          </p>
          {error && <p className="mt-1 text-[13px] text-danger-text">{error}</p>}
        </div>
      </div>
    </SectionCard>
  );
}

/** «Категории»: дерево чекбоксов, не больше трёх (UC-DSH-007, replace). */
export function CategoriesSection({ dish }: { dish: DishDetailDto }) {
  const { data: tree } = useCategoryTreeQuery();
  const [selected, setSelected] = useState<string[]>(dish.categoryIds);
  const [save, { isLoading, isSuccess, error }] = useSetDishCategoriesMutation();

  function toggle(id: string) {
    setSelected((prev) =>
      prev.includes(id)
        ? prev.filter((c) => c !== id)
        : prev.length < MAX_CATEGORIES
          ? [...prev, id]
          : prev,
    );
  }

  function renderNodes(nodes: CategoryNodeDto[], depth: number): React.ReactNode {
    return nodes.map((node) => {
      const checked = selected.includes(node.id);
      const disabled = !checked && selected.length >= MAX_CATEGORIES;
      return (
        <div key={node.id}>
          <label
            className={`flex items-center gap-2.5 py-1.5 ${disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer'}`}
            style={{ paddingLeft: depth * 24 }}
          >
            <input
              type="checkbox"
              checked={checked}
              disabled={disabled}
              onChange={() => toggle(node.id)}
              className="h-4.5 w-4.5 cursor-pointer accent-[#C25423] disabled:cursor-not-allowed"
            />
            <span className="text-[15px]">{node.name}</span>
          </label>
          {node.children.length > 0 && renderNodes(node.children, depth + 1)}
        </div>
      );
    });
  }

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <div className="flex items-baseline justify-between gap-4">
        <h2 className="font-semibold">Категории</h2>
        <span className="tabular text-sm text-ink-secondary">
          {selected.length} из {MAX_CATEGORIES}
        </span>
      </div>
      <p className="mt-1 text-[13px] text-ink-muted">До трёх категорий.</p>
      <div className="mt-3">{tree ? renderNodes(tree, 0) : <p className="text-sm text-ink-muted">Загружаем…</p>}</div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() => save({ dishId: dish.id, categoryIds: selected })}
      />
    </section>
  );
}

/** «Теги»: chips-ввод, replace именами (UC-DSH-008). */
export function TagsSection({ dish }: { dish: DishDetailDto }) {
  const [tags, setTags] = useState<string[]>(dish.tagNames);
  const [save, { isLoading, isSuccess, error }] = useSetDishTagsMutation();

  return (
    <SectionCard title="Теги">
      <TagsInput value={tags} onChange={setTags} />
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() => save({ dishId: dish.id, tagNames: tags })}
      />
    </SectionCard>
  );
}

/** «Диетические метки»: 10 переключаемых чипсов → маска строкой (UC-DSH-009). */
export function DietSection({ dish }: { dish: DishDetailDto }) {
  const initial = dish.dietLabelsMask
    .split(',')
    .map((v) => v.trim())
    .filter((v) => v && v !== 'None');
  const [active, setActive] = useState<string[]>(initial);
  const [save, { isLoading, isSuccess, error }] = useSetDishDietLabelsMutation();

  function toggle(value: string) {
    setActive((prev) => (prev.includes(value) ? prev.filter((v) => v !== value) : [...prev, value]));
  }

  return (
    <SectionCard title="Диетические метки">
      <div className="flex flex-wrap gap-2">
        {dietLabelOptions.map((option) => {
          const isOn = active.includes(option.value);
          return (
            <button
              key={option.value}
              type="button"
              aria-pressed={isOn}
              onClick={() => toggle(option.value)}
              className={`cursor-pointer rounded-pill px-3.5 py-1.5 text-sm font-medium ${
                isOn
                  ? 'bg-success-bg text-success-text'
                  : 'border border-line bg-surface text-ink-secondary hover:bg-sunken'
              }`}
            >
              {option.label}
            </button>
          );
        })}
      </div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() => save({ dishId: dish.id, dietLabelsMask: active.length > 0 ? active.join(', ') : 'None' })}
      />
    </SectionCard>
  );
}

/** «История блюда»: текст ≤4000, null — очистить (UC-DSH-010). */
export function HistorySection({ dish }: { dish: DishDetailDto }) {
  const [history, setHistory] = useState(dish.historyText ?? '');
  const [save, { isLoading, isSuccess, error }] = useSetDishHistoryMutation();

  return (
    <SectionCard title="История блюда">
      <textarea
        rows={4}
        value={history}
        onChange={(e) => setHistory(e.target.value)}
        placeholder="Откуда этот рецепт в вашей семье"
        className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
      />
      <CharCounter value={history.length} max={MAX_LONG} />
      <p className="mt-1 text-[13px] text-ink-muted">
        Необязательно. Покажем на странице блюда отдельным блоком.
      </p>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        onSave={() => save({ dishId: dish.id, historyText: orNull(history) })}
      />
    </SectionCard>
  );
}
