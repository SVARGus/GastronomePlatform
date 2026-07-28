import { useLayoutEffect, useRef, useState } from 'react';

interface MarqueeTextProps {
  /** Текст, который при нехватке места прокручивается бегущей строкой. */
  text: string;
  /** Дополнительные классы контейнера (ширину задаёт родитель). */
  className?: string;
}

/**
 * Однострочный текст с «бегущей строкой» при переполнении: пауза на старте,
 * прокрутка до конца, пауза в конце и перезапуск с начала (keyframes
 * `gp-marquee` в index.css). Если текст помещается целиком — стоит на месте.
 * При `prefers-reduced-motion` анимация отключается глобальным правилом.
 */
export function MarqueeText({ text, className }: MarqueeTextProps) {
  const containerRef = useRef<HTMLSpanElement>(null);
  const textRef = useRef<HTMLSpanElement>(null);
  const [shift, setShift] = useState(0);

  // Измеряем переполнение после раскладки и при изменении размеров контейнера.
  useLayoutEffect(() => {
    const container = containerRef.current;
    const inner = textRef.current;
    if (!container || !inner) return;

    const measure = () => {
      setShift(Math.max(0, inner.scrollWidth - container.clientWidth));
    };

    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(container);
    return () => observer.disconnect();
  }, [text]);

  return (
    <span ref={containerRef} className={`block overflow-hidden whitespace-nowrap ${className ?? ''}`}>
      <span
        ref={textRef}
        className={`inline-block ${shift > 0 ? 'gp-marquee' : ''}`}
        style={shift > 0 ? { '--gp-marquee-shift': `-${shift}px` } as React.CSSProperties : undefined}
      >
        {text}
      </span>
    </span>
  );
}
