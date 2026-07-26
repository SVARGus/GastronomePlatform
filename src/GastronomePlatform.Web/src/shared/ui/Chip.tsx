import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

/** Чипс-ссылка (pill): популярные запросы, теги, фильтры-переходы. */
export function ChipLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      className="inline-flex h-9 items-center rounded-pill border border-line bg-surface px-4 text-[15px] font-medium text-ink shadow-chip transition-colors duration-[120ms] hover:border-line-strong hover:bg-sunken"
    >
      {children}
    </Link>
  );
}
