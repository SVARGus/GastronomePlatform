import { Link, NavLink, Outlet } from 'react-router-dom';
import { Logo } from '../../shared/ui/Logo';

/**
 * Общий каркас страниц: шапка с навигацией, контент (Outlet), подвал.
 * Каркасная версия — вёрстка по макету (мобильное меню-«пар» и т. д.) впереди.
 */
export function SiteLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex h-[72px] w-full max-w-[1200px] items-center gap-8 px-6">
          <Link to="/" className="flex items-center gap-3 text-ink hover:text-ink">
            <Logo className="h-9 w-9" />
            <span className="font-display text-xl font-[560]">GastronomePlatform</span>
          </Link>
          <nav className="hidden items-center gap-6 md:flex">
            <NavLink to="/catalog" className="text-ink hover:text-link-hover">
              Каталог
            </NavLink>
            <NavLink to="/pricing" className="text-ink hover:text-link-hover">
              Тарифы
            </NavLink>
          </nav>
          <div className="ml-auto flex items-center gap-3">
            <Link to="/login" className="px-4 py-2 text-ink-secondary hover:text-ink">
              Войти
            </Link>
            <Link
              to="/register"
              className="rounded-pill border border-action px-[18px] py-[7px] font-medium text-link hover:bg-saffron-50"
            >
              Регистрация
            </Link>
          </div>
        </div>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="bg-sunken">
        <div className="mx-auto w-full max-w-[1200px] px-6 py-10 text-sm text-ink-secondary">
          © 2026 GastronomePlatform. Домашняя кухня с профессиональными стандартами.
        </div>
      </footer>
    </div>
  );
}
