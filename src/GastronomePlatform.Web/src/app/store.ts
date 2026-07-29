import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from '../shared/api/authSlice';
import { baseApi } from '../shared/api/baseApi';

/** Redux store приложения: RTK Query + слайс аутентификации. */
export const store = configureStore({
  reducer: {
    [baseApi.reducerPath]: baseApi.reducer,
    auth: authReducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
