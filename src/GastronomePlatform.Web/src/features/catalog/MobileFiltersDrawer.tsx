import { X } from 'lucide-react';
import type { ReactNode } from 'react';
import { Button } from '../../shared/ui/Button';
import { pluralize } from '../../shared/lib/labels';

interface MobileFiltersDrawerProps {
  open: boolean;
  totalCount: number | null;
  onClose: () => void;
  children: ReactNode;
}

/**
 * Полноэкранная шторка фильтров для мобильных (< md). Кнопка внизу показывает
 * живой счётчик результатов — снимает страх нулевой выдачи (бриф 2).
 */
export function MobileFiltersDrawer({ open, totalCount, onClose, children }: MobileFiltersDrawerProps) {
  if (!open) return null;

  const buttonText =
    totalCount === null
      ? 'Показать блюда'
      : totalCount === 0
        ? 'Ничего не найдено'
        : `Показать ${totalCount} ${pluralize(totalCount, ['блюдо', 'блюда', 'блюд'])}`;

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-page md:hidden">
      <header className="flex h-[64px] shrink-0 items-center justify-between border-b border-line bg-surface px-5">
        <h2 className="font-display text-xl font-[540]">Фильтры</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Закрыть фильтры"
          className="rounded-control p-2 text-ink-secondary hover:bg-sunken hover:text-ink"
        >
          <X className="h-5 w-5" strokeWidth={1.75} />
        </button>
      </header>

      <div className="flex-1 overflow-y-auto p-4">{children}</div>

      <footer className="shrink-0 border-t border-line bg-surface p-4">
        <Button variant="primary" size="lg" className="w-full tabular" onClick={onClose} disabled={totalCount === 0}>
          {buttonText}
        </Button>
      </footer>
    </div>
  );
}
