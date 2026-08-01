import { ChevronDown, ChevronRight, Star } from 'lucide-react';
import { useState } from 'react';
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
        <CategoryList nodes={categories} selected={filters.categoryIds} onToggle={onToggle} />
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

/**
 * Дерево категорий фильтра. Роли кликов разделены: чекбокс выбирает
 * только сам чекбокс; у узлов с потомками текст и шеврон разворачивают
 * поддерево (свёрнуто по умолчанию), у листьев текст выбирает — «мёртвых»
 * зон клика нет.
 */
function CategoryList({
  nodes,
  selected,
  onToggle,
}: {
  nodes: CategoryNodeDto[];
  selected: string[];
  onToggle: (key: ArrayFilterKey, value: string) => void;
}) {
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set());

  if (nodes.length === 0) {
    return <p className="text-sm text-ink-muted">Категории появятся вместе с блюдами.</p>;
  }

  function toggleExpanded(id: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  function renderNodes(list: CategoryNodeDto[], depth: number): React.ReactNode {
    return (
      <ul className="space-y-1.5">
        {list.map((node) => {
          const hasChildren = node.children.length > 0;
          const isExpanded = expanded.has(node.id);
          return (
            <li key={node.id} style={{ paddingLeft: depth * 16 }}>
              <div className="flex items-center gap-2.5 text-[15px] text-ink">
                <input
                  type="checkbox"
                  checked={selected.includes(node.id)}
                  onChange={() => onToggle('categoryIds', node.id)}
                  aria-label={`Категория ${node.name}`}
                  className="h-4.5 w-4.5 shrink-0 cursor-pointer accent-[#C25423]"
                />
                {hasChildren ? (
                  <button
                    type="button"
                    onClick={() => toggleExpanded(node.id)}
                    aria-expanded={isExpanded}
                    className="flex min-w-0 cursor-pointer items-center gap-1 text-left hover:text-action"
                  >
                    <span className="truncate">{node.name}</span>
                    {isExpanded ? (
                      <ChevronDown className="h-4 w-4 shrink-0 text-ink-muted" strokeWidth={1.75} aria-hidden />
                    ) : (
                      <ChevronRight className="h-4 w-4 shrink-0 text-ink-muted" strokeWidth={1.75} aria-hidden />
                    )}
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={() => onToggle('categoryIds', node.id)}
                    className="min-w-0 cursor-pointer truncate text-left hover:text-action"
                  >
                    {node.name}
                  </button>
                )}
              </div>
              {hasChildren && isExpanded && renderNodes(node.children, depth + 1)}
            </li>
          );
        })}
      </ul>
    );
  }

  return renderNodes(nodes, 0);
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
