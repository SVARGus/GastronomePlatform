import { baseApi } from '../../shared/api/baseApi';
import type {
  SubscribeRequest,
  SubscribeResult,
  SubscriptionCatalogPlanDto,
  SubscriptionResponse,
} from '../../shared/api/types/subscriptions';

/** Эндпоинты модуля Subscriptions: витрина тарифов и оформление подписки. */
export const subscriptionsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Витрина каталога подписок (UC-SUB-040). Анонимный; пустой каталог — []. */
    subscriptionCatalog: build.query<SubscriptionCatalogPlanDto[], void>({
      query: () => ({ url: 'subscription-plans' }),
      providesTags: ['Plans'],
    }),
    /** Оформление подписки (UC-SUB-020). 409 ALREADY_HAS_BASE / 403 FORBIDDEN_ROLE_REQUIRED. */
    subscribe: build.mutation<SubscribeResult, SubscribeRequest>({
      query: (body) => ({ url: 'user-subscriptions', method: 'POST', body }),
      invalidatesTags: ['Subscription', 'Dishes'],
    }),
    /** Карточка подписки (UC-SUB-021) — владелец или админ. */
    subscriptionById: build.query<SubscriptionResponse, string>({
      query: (id) => ({ url: `user-subscriptions/${id}` }),
      providesTags: (_result, _error, id) => [{ type: 'Subscription', id }],
    }),
    /** Все подписки текущего пользователя (UC-SUB-026) — раздел «Подписка» кабинета. */
    mySubscriptions: build.query<SubscriptionResponse[], void>({
      query: () => ({ url: 'user-subscriptions/my' }),
      providesTags: [{ type: 'Subscription', id: 'my' }],
    }),
    /** Отмена автопродления (UC-SUB-022). Доступ — до конца оплаченного периода. */
    cancelSubscription: build.mutation<void, string>({
      query: (id) => ({ url: `user-subscriptions/${id}/cancel`, method: 'POST' }),
      invalidatesTags: ['Subscription'],
    }),
  }),
});

export const {
  useSubscriptionCatalogQuery,
  useSubscribeMutation,
  useSubscriptionByIdQuery,
  useMySubscriptionsQuery,
  useCancelSubscriptionMutation,
} = subscriptionsApi;
