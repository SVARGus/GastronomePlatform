import { Eye, EyeOff } from 'lucide-react';
import { useId, useState } from 'react';

interface TextFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: 'text' | 'email' | 'password' | 'tel';
  placeholder?: string;
  /** Текст ошибки под полем; задаёт и красную рамку. */
  error?: string | null;
  /** Подсказка под полем (требования к вводу). Скрывается, пока показана ошибка. */
  hint?: string;
  /** Пометка справа от label вторичным цветом («— необязательно»). */
  optionalNote?: string;
  autoComplete?: string;
  required?: boolean;
}

/**
 * Поле формы дизайн-системы: label, состояние ошибки (рамка + текст),
 * для паролей — «глазок» показа значения. Управляемое (value/onChange).
 */
export function TextField({
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
  error,
  hint,
  optionalNote,
  autoComplete,
  required,
}: TextFieldProps) {
  const id = useId();
  const [passwordShown, setPasswordShown] = useState(false);

  const isPassword = type === 'password';
  const inputType = isPassword && passwordShown ? 'text' : type;

  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-ink">
        {label}
        {optionalNote && <span className="font-normal text-ink-muted"> {optionalNote}</span>}
      </label>
      <div className="relative">
        <input
          id={id}
          type={inputType}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          autoComplete={autoComplete}
          required={required}
          className={`h-11 w-full rounded-control border bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action ${
            error ? 'border-danger' : 'border-line'
          } ${isPassword ? 'pr-11' : ''}`}
        />
        {isPassword && (
          <button
            type="button"
            onClick={() => setPasswordShown((shown) => !shown)}
            aria-label={passwordShown ? 'Скрыть пароль' : 'Показать пароль'}
            className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-control p-2 text-ink-muted hover:text-ink"
          >
            {passwordShown ? (
              <Eye className="h-5 w-5" strokeWidth={1.75} aria-hidden />
            ) : (
              <EyeOff className="h-5 w-5" strokeWidth={1.75} aria-hidden />
            )}
          </button>
        )}
      </div>
      {error && <p className="mt-1.5 text-[13px] text-danger-text">{error}</p>}
      {!error && hint && <p className="mt-1.5 text-[13px] text-ink-muted">{hint}</p>}
    </div>
  );
}
