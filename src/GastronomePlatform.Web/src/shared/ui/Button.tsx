import type { ButtonHTMLAttributes } from 'react';

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'outline' | 'danger';
type ButtonSize = 'lg' | 'md' | 'sm';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
}

/** Стили вариантов — бренд-бук §3.6. Правило: не более одной primary на экран. */
const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-action text-on-action hover:bg-action-hover active:bg-action-active disabled:bg-[#EDE4D8] disabled:text-ink-disabled',
  secondary: 'border border-line-strong bg-transparent text-ink hover:bg-sunken',
  ghost: 'bg-transparent text-ink-secondary hover:bg-sunken',
  outline: 'border border-action bg-transparent text-link hover:bg-saffron-50',
  danger: 'border border-danger bg-transparent text-danger-text hover:bg-danger-bg',
};

/** Высоты — UI Kit: lg 52 (hero CTA), md 44 (стандарт, тач-минимум), sm 36 (в карточках). */
const sizeClasses: Record<ButtonSize, string> = {
  lg: 'h-[52px] px-6 text-base',
  md: 'h-11 px-5 text-[15px]',
  sm: 'h-9 px-3.5 text-[15px]',
};

/** Кнопка дизайн-системы. Без КАПСА; текст — глагол действия (ToV §2). */
export function Button({ variant = 'primary', size = 'md', className = '', type = 'button', ...rest }: ButtonProps) {
  return (
    <button
      type={type}
      className={`inline-flex cursor-pointer items-center justify-center whitespace-nowrap rounded-control font-medium transition-colors duration-[120ms] disabled:cursor-not-allowed ${variantClasses[variant]} ${sizeClasses[size]} ${className}`}
      {...rest}
    />
  );
}
