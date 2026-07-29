import { CircleAlert, CircleCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useLoginMutation } from '../features/auth/authApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import { baseApi } from '../shared/api/baseApi';
import { useAppDispatch, useAppSelector } from '../app/hooks';
import { Button } from '../shared/ui/Button';
import { Logo } from '../shared/ui/Logo';
import { TextField } from '../shared/ui/TextField';

interface LoginLocationState {
  /** Куда вернуть после входа (тизер рецепта передаёт исходную страницу). */
  from?: string;
  /** Показ плашки «Аккаунт создан» после регистрации. */
  registered?: boolean;
}

/**
 * Вход (приоритет 1, макет AuthPages 3a/3b): единое поле
 * «email / никнейм / телефон» + пароль с «глазком». После входа кэш API
 * сбрасывается (данные зависят от пользователя — гейт рецепта, профиль)
 * и происходит возврат на исходную страницу.
 */
export function LoginPage() {
  const [loginValue, setLoginValue] = useState('');
  const [password, setPassword] = useState('');

  const [login, { isLoading, isError }] = useLoginMutation();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();

  const state = (location.state ?? {}) as LoginLocationState;
  const from = state.from ?? '/';
  const showRegistered = state.registered === true && !isError;

  // Уже вошедшему форма входа не нужна — возврат на исходную страницу.
  // Срабатывает и после успешного submit (sessionStarted поднимает флаг).
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, from, navigate]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await login({ login: loginValue.trim(), password }).unwrap();
      dispatch(baseApi.util.resetApiState());
    } catch {
      // Ошибка отражена в isError — показывается плашкой.
    }
  }

  return (
    <section className="mx-auto w-full max-w-[400px] px-4 py-12 md:py-16">
      <div className="mb-5 flex justify-center">
        <Logo className="h-[52px] w-[52px]" />
      </div>

      <div className="rounded-card border border-line bg-surface p-8 shadow-card">
        <h1 className="text-center text-[30px] font-[560] leading-[1.2]">С возвращением</h1>
        <p className="mt-2 text-center text-[15px] text-ink-secondary">
          Войдите, чтобы готовить по полным рецептам.
        </p>

        {showRegistered && (
          <p className="mt-6 flex items-start gap-2.5 rounded-control bg-success-bg p-3.5 text-sm text-success-text">
            <CircleCheck className="mt-0.5 h-4.5 w-4.5 shrink-0" strokeWidth={1.75} aria-hidden />
            Аккаунт создан — теперь войдите.
          </p>
        )}

        {isError && (
          <p className="mt-6 flex items-start gap-2.5 rounded-control bg-danger-bg p-3.5 text-sm text-danger-text">
            <CircleAlert className="mt-0.5 h-4.5 w-4.5 shrink-0" strokeWidth={1.75} aria-hidden />
            Не удалось войти: проверьте логин и пароль.
          </p>
        )}

        <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
          <TextField
            label="Email, никнейм или телефон"
            value={loginValue}
            onChange={setLoginValue}
            placeholder="you@example.com"
            autoComplete="username"
            required
          />
          <TextField
            label="Пароль"
            type="password"
            value={password}
            onChange={setPassword}
            placeholder="Ваш пароль"
            autoComplete="current-password"
            required
          />
          <Button type="submit" variant="primary" className="mt-1 w-full" disabled={isLoading}>
            {isLoading ? 'Входим…' : 'Войти'}
          </Button>
        </form>

        <p className="mt-5 text-center text-[15px] text-ink-secondary">
          Нет аккаунта?{' '}
          <Link to="/register" state={state.from ? { from: state.from } : undefined} className="font-medium">
            Зарегистрироваться
          </Link>
        </p>
      </div>
    </section>
  );
}
