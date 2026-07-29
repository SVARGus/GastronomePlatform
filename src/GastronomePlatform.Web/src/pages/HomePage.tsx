import { Search } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import { usePopularTagsQuery, useSearchDishesQuery } from '../features/dishes/dishesApi';
import { selectIsAuthenticated } from '../shared/api/authSlice';
import { Button } from '../shared/ui/Button';
import { ChipLink } from '../shared/ui/Chip';
import { DishCard, DishCardSkeleton } from '../shared/ui/DishCard';

/** Чипсы hero, пока популярные теги не загружены или каталог пуст. */
const FALLBACK_CHIPS = ['Борщ', 'Паста', 'Завтраки', 'ПП'];

/**
 * Главная страница (бриф 1): hero с поиском, тематические подборки,
 * живые «Популярные блюда» (UC-DSH-054, ViewsDesc), «Как это работает», CTA поварам.
 */
export function HomePage() {
  return (
    <>
      <Hero />
      <CollectionBand
        title="Восточная кухня"
        bg="bg-[#F6E7D4]"
        titleColor="text-[#7A4E16]"
        items={['Плов', 'Хинкали', 'Лагман', 'Шакшука', 'Пахлава']}
        circleColors={['bg-[#E8B95C]', 'bg-[#CE6B3B]', 'bg-[#C9A55A]', 'bg-[#E9C271]', 'bg-[#B99B62]']}
      />
      <CollectionBand
        title="Освежающие блюда"
        bg="bg-[#E2F1ED]"
        titleColor="text-[#1F5D51]"
        items={['Окрошка', 'Гаспачо', 'Греческий', 'Севиче', 'Табуле']}
        circleColors={['bg-[#A8C686]', 'bg-[#CE6B3B]', 'bg-[#9CBF74]', 'bg-[#E9C271]', 'bg-[#8FB98B]']}
      />
      <PopularDishes />
      <HowItWorks />
      <ChefsCta />
    </>
  );
}

function Hero() {
  const navigate = useNavigate();
  const [text, setText] = useState('');
  const { data: tags } = usePopularTagsQuery();

  const chips =
    tags && tags.length > 0
      ? tags.slice(0, 4).map((t) => ({ label: t.name, to: `/catalog?tagIds=${t.id}` }))
      : FALLBACK_CHIPS.map((label) => ({ label, to: `/catalog?text=${encodeURIComponent(label)}` }));

  function submitSearch(e: React.FormEvent) {
    e.preventDefault();
    navigate(text.trim() ? `/catalog?text=${encodeURIComponent(text.trim())}` : '/catalog');
  }

  return (
    <section className="relative overflow-hidden">
      {/* Фото-«тарелка»: нижний слой, привязан к ЦЕНТРУ контента (не к краю окна):
          на широком экране круг виден целиком, при сужении постепенно уходит
          за правый край (overflow-hidden секции срезает без скролла).
          На мобильном — тот же приём с меньшим кругом. */}
      <div
        aria-hidden
        className="pointer-events-none absolute top-1/2 left-[calc(50%+56px)] z-0 -translate-y-1/2 sm:left-[calc(50%+80px)]"
      >
        <div className="h-[300px] w-[300px] rounded-full bg-surface p-3 shadow-lifted sm:h-[360px] sm:w-[360px] sm:p-5 md:h-[440px] md:w-[440px]">
          <div className="h-full w-full rounded-full bg-saffron-400" />
        </div>
      </div>

      <div className="relative z-10 mx-auto w-full max-w-[1200px] px-6 py-16 md:py-24">
        <div className="max-w-[560px] [background:radial-gradient(ellipse_at_left,rgba(250,246,239,.92)_55%,rgba(250,246,239,0)_80%)]">
          <h1 className="text-[36px] leading-[1.15] font-[560] tracking-[-0.005em] md:text-[44px]">
            Домашняя еда от тех,
            <br />
            кто готовит с душой
          </h1>
          <p className="mt-5 max-w-[440px] text-ink-secondary">
            Тёплые рецепты домашней кухни с профессиональными стандартами — найдите блюдо и готовьте по шагам.
          </p>

          <form onSubmit={submitSearch} className="mt-7 flex max-w-[480px] gap-3">
            <label className="relative flex-1">
              <Search
                className="pointer-events-none absolute top-1/2 left-3.5 h-5 w-5 -translate-y-1/2 text-ink-muted"
                strokeWidth={1.75}
                aria-hidden
              />
              <input
                type="search"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder="Найти блюдо или ингредиент"
                className="h-11 w-full rounded-control border border-line bg-surface pr-4 pl-11 text-ink placeholder:text-ink-muted focus:border-action"
              />
            </label>
            <Button type="submit" variant="primary">
              Найти
            </Button>
          </form>

          <div className="mt-4 flex flex-wrap gap-2">
            {chips.map((chip) => (
              <ChipLink key={chip.label} to={chip.to}>
                {chip.label}
              </ChipLink>
            ))}
          </div>
        </div>

      </div>
    </section>
  );
}

function CollectionBand({
  title,
  bg,
  titleColor,
  items,
  circleColors,
}: {
  title: string;
  bg: string;
  titleColor: string;
  items: string[];
  circleColors: string[];
}) {
  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 pb-6">
      <div className={`rounded-card ${bg} px-7 py-6`}>
        <div className="flex items-center justify-between">
          <h2 className={`text-[26px] font-[520] ${titleColor}`}>{title}</h2>
          <Link to="/catalog" className={`text-[15px] font-medium ${titleColor}`}>
            Смотреть все →
          </Link>
        </div>
        <div className="mt-5 flex gap-8 overflow-x-auto pb-1">
          {items.map((name, i) => (
            <Link key={name} to={`/catalog?text=${encodeURIComponent(name)}`} className="flex shrink-0 flex-col items-center gap-2">
              <span className="flex h-24 w-24 items-center justify-center rounded-full bg-surface p-2 shadow-chip">
                <span aria-hidden className={`h-full w-full rounded-full ${circleColors[i]}`} />
              </span>
              <span className="text-sm text-ink">{name}</span>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

function PopularDishes() {
  const { data, isLoading, isError } = useSearchDishesQuery({ sortBy: 'ViewsDesc', pageSize: 4 });

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-12">
      <div className="flex items-center justify-between">
        <h2 className="text-[26px] font-[520]">Популярные блюда</h2>
        <Link to="/catalog" className="text-[15px] font-medium">
          Весь каталог →
        </Link>
      </div>

      {isLoading && (
        <div className="mt-6 grid grid-cols-2 gap-6 lg:grid-cols-4">
          {Array.from({ length: 4 }, (_, i) => (
            <DishCardSkeleton key={i} />
          ))}
        </div>
      )}

      {!isLoading && (isError || !data || data.items.length === 0) && (
        <p className="mt-6 rounded-card bg-surface p-8 text-center text-ink-secondary shadow-card">
          {isError
            ? 'Не получилось загрузить блюда. Обновите страницу или зайдите чуть позже.'
            : 'Здесь появятся популярные блюда — каталог только наполняется.'}
        </p>
      )}

      {data && data.items.length > 0 && (
        <div className="mt-6 grid grid-cols-2 gap-6 lg:grid-cols-4">
          {data.items.map((dish) => (
            <DishCard key={dish.id} dish={dish} />
          ))}
        </div>
      )}
    </section>
  );
}

const HOW_IT_WORKS_STEPS = [
  { n: 1, title: 'Найдите блюдо', text: 'Ищите домашние рецепты в каталоге — по названию или ингредиенту.' },
  { n: 2, title: 'Оформите подписку', text: 'Откроются полные рецепты с пошаговыми фото и расчётом порций.' },
  { n: 3, title: 'Готовьте по шагам', text: 'Следуйте инструкции шаг за шагом.' },
];

function HowItWorks() {
  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 py-12">
      <h2 className="text-center text-[26px] font-[520]">Как это работает</h2>
      <div className="mt-8 grid gap-6 md:grid-cols-3">
        {HOW_IT_WORKS_STEPS.map((step) => (
          <div key={step.n} className="rounded-card bg-surface p-7 shadow-card">
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-saffron-50 font-display text-xl font-[560] text-action">
              {step.n}
            </span>
            <h3 className="mt-4 text-xl font-semibold">{step.title}</h3>
            <p className="mt-2 text-ink-secondary">{step.text}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

/**
 * CTA-блок внизу главной. Гостю предлагает регистрацию; вошедшему
 * пользователю регистрация не нужна — блок зовёт к подписке (тарифы).
 */
function ChefsCta() {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  return (
    <section className="mx-auto w-full max-w-[1200px] px-6 pt-4 pb-16">
      <div className="flex flex-col items-start gap-5 rounded-card bg-surface p-8 shadow-card md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-[26px] font-[520]">Готовите профессионально?</h2>
          <p className="mt-2 text-ink-secondary">Публикуйте свои рецепты на платформе и находите тех, кто их полюбит.</p>
        </div>
        <Link
          to={isAuthenticated ? '/pricing' : '/register'}
          className="inline-flex h-11 shrink-0 items-center rounded-control border border-action px-5 font-medium text-link hover:bg-saffron-50"
        >
          {isAuthenticated ? 'Посмотреть тарифы' : 'Присоединиться'}
        </Link>
      </div>
    </section>
  );
}
