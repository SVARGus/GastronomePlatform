import { X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useLazyTagSearchQuery } from '../dishes/dishesApi';

const MAX_TAGS = 20;
const MAX_TAG_LENGTH = 50;

interface TagsInputProps {
  value: string[];
  onChange: (tags: string[]) => void;
}

/**
 * Chips-ввод тегов редактора: выбранные — пилюли с крестиками, новые — по Enter
 * или из выпадающих подсказок (UC-DSH-060). Дедупликация без регистра — как
 * нормализация сервера (UC-DSH-008); лимиты: 20 тегов, имя ≤ 50 символов.
 */
export function TagsInput({ value, onChange }: TagsInputProps) {
  const [draft, setDraft] = useState('');
  const [focused, setFocused] = useState(false);
  const [searchTags, { data: suggestions }] = useLazyTagSearchQuery();
  const debounceRef = useRef<number | undefined>(undefined);

  // Подсказки с лёгким debounce, чтобы не дёргать API на каждый символ.
  useEffect(() => {
    if (draft.trim().length < 2) return;
    window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => searchTags(draft.trim()), 250);
    return () => window.clearTimeout(debounceRef.current);
  }, [draft, searchTags]);

  const limitReached = value.length >= MAX_TAGS;

  // Принимает и одиночный тег, и строку с запятыми («плов, казан») — каждый
  // фрагмент становится отдельным чипсом. Так вставка списка из буфера и
  // ввод без Enter работают одинаково.
  function addTag(raw: string) {
    const names = raw
      .split(',')
      .map((part) => part.trim().slice(0, MAX_TAG_LENGTH))
      .filter(Boolean);
    if (names.length === 0) {
      return;
    }
    const next = [...value];
    for (const name of names) {
      if (next.length >= MAX_TAGS) break;
      if (next.some((t) => t.toLowerCase() === name.toLowerCase())) continue;
      next.push(name);
    }
    if (next.length !== value.length) {
      onChange(next);
    }
    setDraft('');
  }

  const visibleSuggestions =
    focused && draft.trim().length >= 2
      ? (suggestions ?? []).filter((s) => !value.some((t) => t.toLowerCase() === s.name.toLowerCase())).slice(0, 6)
      : [];

  return (
    <div>
      {value.length > 0 && (
        <div className="mb-3 flex flex-wrap gap-2">
          {value.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center gap-1.5 rounded-pill border border-line bg-surface py-1.5 pr-2 pl-3.5 text-[15px] shadow-chip"
            >
              {tag}
              <button
                type="button"
                onClick={() => onChange(value.filter((t) => t !== tag))}
                aria-label={`Убрать тег ${tag}`}
                className="cursor-pointer rounded-full p-0.5 text-ink-muted hover:bg-sunken hover:text-ink"
              >
                <X className="h-3.5 w-3.5" strokeWidth={1.75} aria-hidden />
              </button>
            </span>
          ))}
        </div>
      )}

      <div className="relative max-w-[340px]">
        <input
          type="text"
          value={draft}
          onChange={(e) => {
            const text = e.target.value;
            // Запятая коммитит тег сразу, не дожидаясь Enter.
            if (text.includes(',')) {
              addTag(text);
            } else {
              setDraft(text);
            }
          }}
          onFocus={() => setFocused(true)}
          onBlur={() => {
            // Набранный, но не закоммиченный текст не должен теряться: иначе
            // «Сохранить» с replace-семантикой молча уносит пустой список.
            if (draft.trim()) {
              addTag(draft);
            }
            window.setTimeout(() => setFocused(false), 150);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              addTag(draft);
            }
          }}
          placeholder={limitReached ? 'Достигнут лимит тегов' : 'Добавить тег…'}
          disabled={limitReached}
          className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action disabled:bg-sunken"
        />
        {visibleSuggestions.length > 0 && (
          <div className="absolute inset-x-0 top-[calc(100%+6px)] z-10 flex flex-col rounded-control border border-line bg-surface p-1.5 shadow-lifted">
            {visibleSuggestions.map((s) => (
              <button
                key={s.id}
                type="button"
                onMouseDown={(e) => {
                  e.preventDefault();
                  addTag(s.name);
                }}
                className="cursor-pointer rounded-[8px] px-3 py-2 text-left text-[15px] text-ink hover:bg-sunken"
              >
                {s.name}
              </button>
            ))}
          </div>
        )}
      </div>

      <p className="mt-3 text-[13px] text-ink-muted">
        До {MAX_TAGS} тегов. Enter или запятая добавляет тег — можно вставить сразу несколько через
        запятую («плов, казан, баранина»). Новые теги создадутся автоматически и попадут в поиск
        после проверки модератором.
      </p>
    </div>
  );
}
