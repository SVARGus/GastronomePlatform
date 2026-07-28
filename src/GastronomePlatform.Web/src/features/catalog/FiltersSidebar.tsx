import { Star } from 'lucide-react';
import type { CategoryNodeDto, TagDto } from '../../shared/api/types/dishes';
import { costOptions, dietLabelOptions, difficultyOptions } from '../../shared/lib/labels';
import type { ArrayFilterKey, CatalogFilters } from './useCatalogFilters';

interface FiltersSidebarProps {
  filters: CatalogFilters;
  categories: CategoryNodeDto[];
  tags: TagDto[];
  onToggle: (key: ArrayFilterKey, value: string) => void;
  onToggleMinRating: (value: number) => void;
  onReset: () => void;
  hasActiveFilters: boolean;
}

/**
 * Сайдбар фильтров каталога — строго существующие параметры UC-DSH-054:
 * категории, диета, сложность, стоимость, рейтинг, теги. Фильтров времени
 * и аллергенов в контракте нет (pages.md §6).
 */
export function FiltersSidebar({
  filters,
  categories,
  tags,
  onToggle,
  onToggleMinRating,
  onReset,
  hasActiveFilters,
}: FiltersSidebarProps) {
  return (
    <div className="rounded-card bg-sunken p-5">
      <Section title="Категории">
        <CategoryList nodes={categories} depth={0} selected={filters.categoryIds} onToggle={onToggle} />
      </Section>

      <Section title="Диета">
        <ChipGroup
          options={dietLabelOptions}
          selected={filters.diets}
          onToggle={(v) => onToggle('diets', v)}
        />
      </Section>

      <Section title="Сложность">
        <ChipGroup
          options={difficultyOptions}
          selected={filters.difficulties}
          onToggle={(v) => onToggle('difficulties', v)}
        />
      </Section>

      <Section title="Стоимость готовки">
        <ChipGroup
          options={costOptions}
          selected={filters.costs}
          onToggle={(v) => onToggle('costs', v)}
        />
      </Section>

      <Section title="Рейтинг">
        <button
          type="button"
          onClick={() => onToggleMinRating(4)}
          className={`inline-flex h-9 items-center gap-1.5 rounded-pill border px-3.5 text-sm font-medium transition-colors duration-[120ms] ${
            filters.minRating === 4
              ? 'border-action bg-action text-on-action'
              : 'border-line bg-surface text-ink hover:border-line-strong'
          }`}
        >
          <Star className={`h-4 w-4 ${filters.minRating === 4 ? 'fill-on-action stroke-on-action' : 'fill-gold stroke-gold'}`} aria-hidden />
          от 4 и выше
        </button>
      </Section>

      {tags.length > 0 && (
        <Section title="Теги">
          <ChipGroup
            options={tags.map((t) => ({ value: t.id, label: t.name }))}
            selected={filters.tagIds}
            onToggle={(v) => onToggle('tagIds', v)}
          />
        </Section>
      )}

      {hasActiveFilters && (
        <button
          type="button"
          onClick={onReset}
          className="mt-2 text-sm text-ink-secondary underline-offset-2 hover:text-ink hover:underline"
        >
          Сбросить всё
        </button>
      )}
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mb-6 last:mb-0">
      <h3 className="mb-3 text-sm font-semibold text-ink">{title}</h3>
      {children}
    </div>
  );
}

function CategoryList({
  nodes,
  depth,
  selected,
  onToggle,
}: {
  nodes: CategoryNodeDto[];
  depth: number;
  selected: string[];
  onToggle: (key: ArrayFilterKey, value: string) => void;
}) {
  if (nodes.length === 0) {
    return <p className="text-sm text-ink-muted">Категории появятся вместе с блюдами.</p>;
  }

  return (
    <ul className="space-y-2">
      {nodes.map((node) => (
        <li key={node.id} style={{ paddingLeft: depth * 16 }}>
          <label className="flex cursor-pointer items-center gap-2.5 text-[15px] text-ink">
            <input
              type="checkbox"
              checked={selected.includes(node.id)}
              onChange={() => onToggle('categoryIds', node.id)}
              className="h-4 w-4 cursor-pointer rounded-badge border-line accent-[#C25423]"
            />
            {node.name}
          </label>
          {node.children.length > 0 && (
            <CategoryList nodes={node.children} depth={depth + 1} selected={selected} onToggle={onToggle} />
          )}
        </li>
      ))}
    </ul>
  );
}

function ChipGroup({
  options,
  selected,
  onToggle,
}: {
  options: ReadonlyArray<{ value: string; label: string }>;
  selected: string[];
  onToggle: (value: string) => void;
}) {
  return (
    <div className="flex flex-wrap gap-2">
      {options.map((option) => {
        const active = selected.includes(option.value);
        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onToggle(option.value)}
            className={`inline-flex h-9 items-center rounded-pill border px-3.5 text-sm font-medium transition-colors duration-[120ms] ${
              active
                ? 'border-action bg-action text-on-action'
                : 'border-line bg-surface text-ink hover:border-line-strong'
            }`}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
