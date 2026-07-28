import { X } from 'lucide-react';
import type { CategoryNodeDto, TagDto } from '../../shared/api/types/dishes';
import { costLabels, dietLabelOptions, difficultyLabels } from '../../shared/lib/labels';
import type { ArrayFilterKey, CatalogFilters } from './useCatalogFilters';

interface AppliedFilterChipsProps {
  filters: CatalogFilters;
  categories: CategoryNodeDto[];
  tags: TagDto[];
  onToggle: (key: ArrayFilterKey, value: string) => void;
  onClearText: () => void;
  onToggleMinRating: (value: number) => void;
}

interface Chip {
  key: string;
  label: string;
  remove: () => void;
}

/** Чипсы применённых фильтров над выдачей — выдача всегда объяснима (бриф 2). */
export function AppliedFilterChips({
  filters,
  categories,
  tags,
  onToggle,
  onClearText,
  onToggleMinRating,
}: AppliedFilterChipsProps) {
  const categoryNames = flattenCategories(categories);
  const tagNames = new Map(tags.map((t) => [t.id, t.name]));
  const dietNames = new Map(dietLabelOptions.map((d) => [d.value, d.label]));

  const chips: Chip[] = [];

  if (filters.text) {
    chips.push({ key: 'text', label: `Поиск: «${filters.text}»`, remove: onClearText });
  }
  for (const id of filters.categoryIds) {
    chips.push({ key: `cat-${id}`, label: categoryNames.get(id) ?? 'Категория', remove: () => onToggle('categoryIds', id) });
  }
  for (const value of filters.diets) {
    chips.push({ key: `diet-${value}`, label: dietNames.get(value) ?? value, remove: () => onToggle('diets', value) });
  }
  for (const value of filters.difficulties) {
    chips.push({ key: `dif-${value}`, label: difficultyLabels[value], remove: () => onToggle('difficulties', value) });
  }
  for (const value of filters.costs) {
    chips.push({ key: `cost-${value}`, label: costLabels[value], remove: () => onToggle('costs', value) });
  }
  if (filters.minRating !== null) {
    chips.push({ key: 'rating', label: `от ${filters.minRating}★`, remove: () => onToggleMinRating(filters.minRating!) });
  }
  for (const id of filters.tagIds) {
    chips.push({ key: `tag-${id}`, label: tagNames.get(id) ?? 'Тег', remove: () => onToggle('tagIds', id) });
  }

  if (chips.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {chips.map((chip) => (
        <span
          key={chip.key}
          className="inline-flex h-9 items-center gap-1.5 rounded-pill border border-line bg-surface pr-2 pl-3.5 text-sm font-medium text-ink shadow-chip"
        >
          {chip.label}
          <button
            type="button"
            onClick={chip.remove}
            aria-label={`Убрать фильтр ${chip.label}`}
            className="rounded-full p-0.5 text-ink-muted transition-colors duration-[120ms] hover:bg-sunken hover:text-ink"
          >
            <X className="h-4 w-4" strokeWidth={1.75} />
          </button>
        </span>
      ))}
    </div>
  );
}

function flattenCategories(nodes: CategoryNodeDto[], into = new Map<string, string>()): Map<string, string> {
  for (const node of nodes) {
    into.set(node.id, node.name);
    flattenCategories(node.children, into);
  }
  return into;
}
