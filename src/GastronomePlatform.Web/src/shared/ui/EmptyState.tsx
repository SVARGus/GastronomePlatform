import type { ReactNode } from 'react';

/**
 * Пустое состояние: тарелка тонкой линией с «паром» (бренд-бук §8 —
 * посуда, а не еда) + текст-приглашение в ToV и опциональное действие.
 */
export function EmptyState({ title, text, action }: { title: string; text: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center rounded-card bg-surface px-8 py-14 text-center shadow-card">
      <svg viewBox="0 0 120 90" className="h-24 w-32 text-ink-muted" aria-hidden>
        <g fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
          <path d="M42 28 C39 22, 44 18, 41 12" className="text-saffron-500" stroke="currentColor" />
          <path d="M60 26 C57 20, 62 16, 59 10" />
          <path d="M78 28 C75 22, 80 18, 77 12" />
          <ellipse cx="60" cy="62" rx="46" ry="16" />
          <ellipse cx="60" cy="60" rx="28" ry="9" />
        </g>
      </svg>
      <h3 className="mt-5 text-xl font-semibold text-ink">{title}</h3>
      <p className="mt-2 max-w-[420px] text-ink-secondary">{text}</p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  );
}
