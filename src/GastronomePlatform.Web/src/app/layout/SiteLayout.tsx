import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../hooks';
import { useLogoutMutation } from '../../features/auth/authApi';
import { useMyProfileQuery } from '../../features/users/usersApi';
import { sessionEnded, selectIsAuthenticated } from '../../shared/api/authSlice';
import { clearSession, getRefreshToken } from '../../shared/api/authStorage';
import { baseApi } from '../../shared/api/baseApi';
import { mediaThumbnailUrl } from '../../shared/api/media';
import { userDisplayName } from '../../shared/api/types/users';
import { Logo } from '../../shared/ui/Logo';

/**
 * Общий каркас страниц: шапка с навигацией, контент (Outlet), подвал.
 * На мобильном (< md) навигация и вход/регистрация сворачиваются в
 * меню-«пар» — бургер из трёх линий пара, как хохолок логотипа
 * (микросигнатура бренда, бриф v2.0 §Адаптив). При активной сессии
 * вместо «Войти/Регистрация» — аватар-кружок (в кабинет) и «Выйти».
 */
export function SiteLayout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const { data: me } = useMyProfileQuery(undefined, { skip: !isAuthenticated });
  const [logout] = useLogoutMutation();

  // Переход по любой ссылке закрывает мобильное меню.
  useEffect(() => {
    setMenuOpen(false);
  }, [location.pathname, location.search]);

  /** Выход: отзыв refresh на сервере (best effort) + локальная очистка сессии и кэша. */
  async function handleLogout() {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
      try {
        await logout(refreshToken).unwrap();
      } catch {
        // Токен мог быть уже отозван/истечь — локальный выход выполняем всё равно.
      }
    }
    clearSession();
    dispatch(sessionEnded());
    dispatch(baseApi.util.resetApiState());
    navigate('/');
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="relative border-b border-line bg-surface">
        <div className="mx-auto flex h-[64px] w-full max-w-[1200px] items-center gap-6 px-4 md:h-[72px] md:gap-8 md:px-6">
          <Link to="/" className="flex min-w-0 items-center gap-2.5 text-ink hover:text-ink md:gap-3">
            <Logo className="h-8 w-8 shrink-0 md:h-9 md:w-9" />
            <span className="truncate font-display text-lg font-[560] md:text-xl">GastronomePlatform</span>
          </Link>

          <nav className="hidden items-center gap-2 md:flex">
            <HeaderNavLink to="/catalog">Каталог</HeaderNavLink>
            <HeaderNavLink to="/pricing">Тарифы</HeaderNavLink>
          </nav>

          <div className="ml-auto hidden items-center gap-3 md:flex">
            {isAuthenticated ? (
              <>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="cursor-pointer px-3 py-2 text-ink-secondary hover:text-ink"
                >
                  Выйти
                </button>
                <Link to="/account" aria-label="Кабинет" className="shrink-0">
                  <HeaderAvatar
                    avatarMediaId={me?.avatarMediaId ?? null}
                    name={me ? userDisplayName(me) : ''}
                  />
                </Link>
              </>
            ) : (
              <>
                <Link to="/login" className="px-4 py-2 text-ink-secondary hover:text-ink">
                  Войти
                </Link>
                <Link
                  to="/register"
                  className="rounded-pill border border-action px-[18px] py-[7px] font-medium text-link hover:bg-saffron-50"
                >
                  Регистрация
                </Link>
              </>
            )}
          </div>

          <button
            type="button"
            onClick={() => setMenuOpen((open) => !open)}
            aria-label={menuOpen ? 'Закрыть меню' : 'Открыть меню'}
            aria-expanded={menuOpen}
            className="ml-auto rounded-control p-2 text-ink hover:bg-sunken md:hidden"
          >
            <SteamMenuIcon className="h-6 w-6" />
          </button>
        </div>

        {menuOpen && (
          <div className="absolute inset-x-3 top-full z-50 mt-2 rounded-card border border-line bg-surface p-3 shadow-lifted md:hidden">
            <nav className="flex flex-col">
              <MobileMenuLink to="/catalog">Каталог</MobileMenuLink>
              <MobileMenuLink to="/pricing">Тарифы</MobileMenuLink>
            </nav>
            {isAuthenticated ? (
              <div className="mt-2 border-t border-line pt-2">
                <NavLink
                  to="/account"
                  className="flex items-center gap-3 rounded-pill px-4 py-2.5 font-medium text-ink hover:bg-sunken"
                >
                  <HeaderAvatar
                    avatarMediaId={me?.avatarMediaId ?? null}
                    name={me ? userDisplayName(me) : ''}
                    small
                  />
                  Кабинет
                </NavLink>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="w-full cursor-pointer rounded-pill px-4 py-2.5 text-left font-medium text-ink-secondary hover:bg-sunken hover:text-ink"
                >
                  Выйти
                </button>
              </div>
            ) : (
              <div className="mt-2 flex items-center gap-3 border-t border-line pt-3">
                <Link to="/login" className="flex-1 rounded-control px-4 py-2.5 text-center font-medium text-ink-secondary hover:bg-sunken hover:text-ink">
                  Войти
                </Link>
                <Link
                  to="/register"
                  className="flex-1 rounded-pill border border-action px-4 py-2.5 text-center font-medium text-link hover:bg-saffron-50"
                >
                  Регистрация
                </Link>
              </div>
            )}
          </div>
        )}
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

/** Пункт навигации шапки (десктоп): активная страница — пилюля с фоном. */
function HeaderNavLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        isActive
          ? 'rounded-pill bg-saffron-50 px-4 py-2 font-medium text-link hover:text-link'
          : 'rounded-pill px-4 py-2 text-ink hover:bg-sunken hover:text-link-hover'
      }
    >
      {children}
    </NavLink>
  );
}

/** Пункт мобильного меню: активная страница — та же пилюля с фоном. */
function MobileMenuLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        isActive
          ? 'rounded-pill bg-saffron-50 px-4 py-2.5 font-medium text-link hover:text-link'
          : 'rounded-pill px-4 py-2.5 font-medium text-ink hover:bg-sunken'
      }
    >
      {children}
    </NavLink>
  );
}

/** Аватар-кружок шапки: фото профиля либо инициал на шафрановой подложке. */
function HeaderAvatar({
  avatarMediaId,
  name,
  small,
}: {
  avatarMediaId: string | null;
  name: string;
  small?: boolean;
}) {
  const sizeClasses = small ? 'h-8 w-8 text-sm' : 'h-10 w-10';
  if (avatarMediaId) {
    return (
      <img
        src={mediaThumbnailUrl(avatarMediaId)}
        alt=""
        className={`${sizeClasses} rounded-full object-cover`}
      />
    );
  }
  return (
    <span
      aria-hidden
      className={`flex ${sizeClasses} items-center justify-center rounded-full bg-saffron-100 font-medium text-action`}
    >
      {name ? name.charAt(0).toUpperCase() : '·'}
    </span>
  );
}

/** Бургер-«пар»: три волнистые линии — хохолок «Жар-птицы» (stroke 1.75). */
function SteamMenuIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" className={className} aria-hidden>
      <path d="M7 20 C5.5 16.5, 8.5 14.5, 7 11" />
      <path d="M12 21 C10.5 17, 13.5 15, 12 9.5" />
      <path d="M17 20 C15.5 16.5, 18.5 14.5, 17 11" />
    </svg>
  );
}
