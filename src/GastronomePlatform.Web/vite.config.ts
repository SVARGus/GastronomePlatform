import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Прокси /api → WebAPI (http-профиль из launchSettings.json), чтобы в dev
// обходиться без CORS: браузер видит один origin — dev-сервер Vite.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5195',
        changeOrigin: true,
      },
    },
  },
})
