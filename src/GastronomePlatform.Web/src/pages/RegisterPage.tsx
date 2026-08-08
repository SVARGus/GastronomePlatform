import { CircleAlert } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { getErrorCode, getErrorMessage, getValidationError } from '../features/auth/apiErrors';
import { useRegisterMutation } from '../features/auth/authApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import { Button } from '../shared/ui/Button';
import { Logo } from '../shared/ui/Logo';
import { TextField } from '../shared/ui/TextField';

/** Требования пароля — зеркало RegisterCommandValidator backend (AuthLimits). */
const PASSWORD_HINT = 'Минимум 8 символов, заглавная и строчная буквы, цифра и спецсимвол.';

/**
 * Ограничение ввода телефона: цифры и опциональный ведущий «+»,
 * не более 11 цифр («+7 …» или «8 …»). Формат бэкенд не навязывает —
 * это клиентская защита от опечаток.
 */
function sanitizePhone(raw: string): string {
  const hasPlus = raw.trimStart().startsWith('+');
  const digits = raw.replace(/\D/g, '').slice(0, 11);
  return hasPlus ? `+${digits}` : digits;
}

/**
 * Регистрация (приоритет 1, макет AuthPages 3c): email, никнейм, пароль,
 * телефон (опционально). Backend отвечает 204 без токенов — после успеха
 * redirect на /login с плашкой «Аккаунт создан». Ошибки занятости (409,
 * AUTH.*_TAKEN) и валидации (400) показываются под конкретным полем.
 */
export function RegisterPage() {
  const [email, setEmail] = useState('');
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [phone, setPhone] = useState('');

  const [register, { isLoading, error }] = useRegisterMutation();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;

  // Уже вошедшему регистрация не нужна — на главную.
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      await register({
        email: email.trim(),
        userName: userName.trim(),
        password,
        phone: phone.trim() === '' ? null : phone.trim(),
      }).unwrap();
      navigate('/login', { state: { registered: true, ...(from ? { from } : {}) } });
    } catch {
      // Ошибка отражена в error — разбирается по полям ниже.
    }
  }

  const errorCode = getErrorCode(error);
  const fieldErrors = {
    email:
      (errorCode === 'AUTH.EMAIL_TAKEN' ? 'Этот email уже используется.' : null) ??
      getValidationError(error, 'Email'),
    userName:
      (errorCode === 'AUTH.USERNAME_TAKEN' ? 'Этот никнейм уже занят — попробуйте другой.' : null) ??
      getValidationError(error, 'UserName'),
    password: getValidationError(error, 'Password'),
    phone:
      (errorCode === 'AUTH.PHONE_TAKEN' ? 'Этот телефон уже используется.' : null) ??
      getValidationError(error, 'Phone'),
  };
  const hasFieldError = Object.values(fieldErrors).some((message) => message !== null);
  const showGeneralError = error !== undefined && !hasFieldError;
  // Сообщение сервера (VALIDATION.ERROR склеивает причины через «; ») — показываем списком.
  const serverMessages = getErrorMessage(error)?.split('; ') ?? null;

  return (
    <section className="mx-auto w-full max-w-[400px] px-4 py-12 md:py-16">
      <div className="mb-5 flex justify-center">
        <Logo className="h-[52px] w-[52px]" />
      </div>

      <div className="rounded-card border border-line bg-surface p-8 shadow-card">
        <h1 className="text-center text-[30px] font-[560] leading-[1.2]">За наш стол</h1>
        <p className="mt-2 text-center text-[15px] text-ink-secondary">
          Аккаунт открывает избранное, подписку и полные рецепты.
        </p>

        {showGeneralError && (
          <p className="mt-6 flex items-start gap-2.5 rounded-control bg-danger-bg p-3.5 text-sm text-danger-text">
            <CircleAlert className="mt-0.5 h-4.5 w-4.5 shrink-0" strokeWidth={1.75} aria-hidden />
            {serverMessages ? (
              <span>
                {serverMessages.map((message) => (
                  <span key={message} className="block">
                    {message}
                  </span>
                ))}
              </span>
            ) : (
              'Не получилось создать аккаунт. Проверьте данные и попробуйте ещё раз.'
            )}
          </p>
        )}

        <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
          <TextField
            label="Email"
            type="email"
            value={email}
            onChange={setEmail}
            placeholder="you@example.com"
            error={fieldErrors.email}
            autoComplete="email"
            required
          />
          <TextField
            label="Никнейм"
            value={userName}
            onChange={setUserName}
            placeholder="Имя на платформе"
            error={fieldErrors.userName}
            autoComplete="username"
            required
          />
          <TextField
            label="Пароль"
            type="password"
            value={password}
            onChange={setPassword}
            placeholder="Придумайте пароль"
            error={fieldErrors.password}
            hint={PASSWORD_HINT}
            autoComplete="new-password"
            required
          />
          <TextField
            label="Телефон"
            type="tel"
            value={phone}
            onChange={(value) => setPhone(sanitizePhone(value))}
            placeholder="+79000000000"
            error={fieldErrors.phone}
            optionalNote="— необязательно"
            autoComplete="tel"
          />
          <Button type="submit" variant="primary" className="mt-1 w-full" disabled={isLoading}>
            {isLoading ? 'Создаём…' : 'Создать аккаунт'}
          </Button>
        </form>

        <p className="mt-4 text-center text-[13px] text-ink-muted">
          Создавая аккаунт, вы принимаете условия использования.
        </p>
        <p className="mt-4 text-center text-[15px] text-ink-secondary">
          Уже с нами?{' '}
          <Link to="/login" state={from ? { from } : undefined} className="font-medium">
            Войти
          </Link>
        </p>
      </div>
    </section>
  );
}
