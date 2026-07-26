import { Link } from 'react-router-dom';

/** 404: тон по бренд-буку — что случилось + что делать, приглашение в каталог. */
export function NotFoundPage() {
  return (
    <section className="mx-auto w-full max-w-[560px] px-6 py-24 text-center">
      <h1 className="text-[34px] font-[540] leading-[1.2]">Такой страницы нет</h1>
      <p className="mt-4 text-ink-secondary">
        Возможно, ссылка устарела. Загляните в каталог — там больше тысячи домашних рецептов.
      </p>
      <Link
        to="/catalog"
        className="mt-8 inline-flex h-11 items-center rounded-control bg-action px-6 font-medium text-on-action hover:bg-action-hover"
      >
        В каталог
      </Link>
    </section>
  );
}
