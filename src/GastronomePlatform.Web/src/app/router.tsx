import { createBrowserRouter } from 'react-router-dom';
import { SiteLayout } from './layout/SiteLayout';
import { HomePage } from '../pages/HomePage';
import { AuthorPage } from '../pages/AuthorPage';
import { CatalogPage } from '../pages/CatalogPage';
import { DishPage } from '../pages/DishPage';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { PricingPage } from '../pages/PricingPage';
import { SubscribePage } from '../pages/SubscribePage';
import { AccountPage } from '../pages/AccountPage';
import { NotFoundPage } from '../pages/NotFoundPage';

/** Маршруты страниц приоритета 1 (docs/public/design/pages.md §2). */
export const router = createBrowserRouter([
  {
    element: <SiteLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/catalog', element: <CatalogPage /> },
      { path: '/dishes/:slug', element: <DishPage /> },
      { path: '/authors/:userId', element: <AuthorPage /> },
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
      { path: '/pricing', element: <PricingPage /> },
      { path: '/subscribe', element: <SubscribePage /> },
      { path: '/account', element: <AccountPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
