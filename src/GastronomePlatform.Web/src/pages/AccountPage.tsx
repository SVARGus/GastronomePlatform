import { CreditCard, UserRound } from 'lucide-react';
import { useEffect } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { ProfileSection } from '../features/account/ProfileSection';
import { SubscriptionSection } from '../features/account/SubscriptionSection';
import { useMyProfileQuery } from '../features/users/usersApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import { mediaThumbnailUrl } from '../shared/api/media';
import { userDisplayName } from '../shared/api/types/users';

type AccountTab = 'profile' | 'subscription';

/**
 * Личный кабинет (приоритет 1, макет AccountPages 4a–4e): сайдбар-меню
 * на десктопе / табы на мобильном, разделы «Профиль» и «Подписка».
 * Активный раздел живёт в URL (?tab=…) — переживает F5 и шарится ссылкой.
 */
export function AccountPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const [searchParams] = useSearchParams();

  const tab: AccountTab = searchParams.get('tab') === 'subscription' ? 'subscription' : 'profile';

  const { data: me } = useMyProfileQuery(undefined, { skip: !isAuthenticated });

  // Кабинет только для вошедших — гость уходит на вход с возвратом сюда.
  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: `${location.pathname}${location.search}` } });
    }
  }, [isAuthenticated, navigate, location.pathname, location.search]);

  if (!isAuthenticated) return null;

  return (
    <div className="mx-auto w-full max-w-[1200px] px-4 py-6 md:px-6 md:py-8">
      <div className="items-start gap-8 md:flex">
        {/* Меню кабинета: сайдбар ≥ md */}
        <aside className="hidden w-[240px] shrink-0 rounded-card bg-sunken p-5 md:block">
          <div className="flex items-center gap-3 border-b border-line pb-4">
            {me?.avatarMediaId ? (
              <img src={mediaThumbnailUrl(me.avatarMediaId)} alt="" className="h-11 w-11 rounded-full object-cover" />
            ) : (
              <span
                aria-hidden
                className="flex h-11 w-11 items-center justify-center rounded-full bg-saffron-100 font-medium text-action"
              >
                {me ? userDisplayName(me).charAt(0).toUpperCase() : '·'}
              </span>
            )}
            <span className="truncate text-[15px] font-semibold">{me?.userName ?? ''}</span>
          </div>
          <nav className="mt-3 flex flex-col gap-1">
            <AccountMenuLink to="/account" active={tab === 'profile'}>
              <UserRound className="h-[19px] w-[19px]" strokeWidth={1.75} aria-hidden />
              Профиль
            </AccountMenuLink>
            <AccountMenuLink to="/account?tab=subscription" active={tab === 'subscription'}>
              <CreditCard className="h-[19px] w-[19px]" strokeWidth={1.75} aria-hidden />
              Подписка
            </AccountMenuLink>
          </nav>
        </aside>

        {/* Меню кабинета: табы < md */}
        <div className="mb-5 flex gap-2 rounded-control bg-sunken p-1.5 md:hidden">
          <AccountTabLink to="/account" active={tab === 'profile'}>
            Профиль
          </AccountTabLink>
          <AccountTabLink to="/account?tab=subscription" active={tab === 'subscription'}>
            Подписка
          </AccountTabLink>
        </div>

        <div className="min-w-0 max-w-[640px] flex-1">
          <h1 className="mb-6 text-[30px] font-[560] leading-[1.2] md:text-[34px]">
            {tab === 'profile' ? 'Профиль' : 'Подписка'}
          </h1>
          {tab === 'profile' ? <ProfileSection /> : <SubscriptionSection />}
        </div>
      </div>
    </div>
  );
}

/** Пункт сайдбар-меню кабинета. */
function AccountMenuLink({ to, active, children }: { to: string; active: boolean; children: React.ReactNode }) {
  return (
    <Link
      to={to}
      className={`flex items-center gap-2.5 rounded-control px-3.5 py-3 font-medium ${
        active ? 'bg-saffron-50 text-action hover:text-action' : 'text-ink hover:bg-surface hover:text-ink'
      }`}
    >
      {children}
    </Link>
  );
}

/** Таб мобильного меню кабинета. */
function AccountTabLink({ to, active, children }: { to: string; active: boolean; children: React.ReactNode }) {
  return (
    <Link
      to={to}
      className={`flex-1 rounded-[8px] py-2.5 text-center font-medium ${
        active ? 'bg-saffron-50 text-action hover:text-action' : 'text-ink hover:text-ink'
      }`}
    >
      {children}
    </Link>
  );
}
