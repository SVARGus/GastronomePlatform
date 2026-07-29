import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from './store';

/** Типизированный useDispatch (знает про thunk-и RTK Query). */
export const useAppDispatch = useDispatch.withTypes<AppDispatch>();

/** Типизированный useSelector под RootState приложения. */
export const useAppSelector = useSelector.withTypes<RootState>();
