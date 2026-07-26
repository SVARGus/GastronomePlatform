import { useParams } from 'react-router-dom';

/** Карточка блюда (приоритет 1, бриф 3): публичная часть + три состояния Premium-гейта рецепта. Заглушка каркаса. */
export function DishPage() {
  const { slug } = useParams<{ slug: string }>();

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-16">
      <h1 className="text-[34px] font-[540] leading-[1.2]">Карточка блюда</h1>
      <p className="mt-4 text-ink-secondary">
        Блюдо <span className="font-medium text-ink">{slug}</span> — страница в разработке.
      </p>
    </section>
  );
}
