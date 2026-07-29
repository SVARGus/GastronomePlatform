import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Прокси /api → WebAPI (http-профиль из launchSettings.json), чтобы в dev
// обходиться без CORS: браузер видит один origin — dev-сервер Vite.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Production-сборка кладётся прямо в wwwroot WebAPI: сайт и API — один
  // origin (ADR-0018), dotnet publish забирает содержимое wwwroot сам.
  // emptyOutDir безопасен: кроме сборки в wwwroot ничего не живёт
  // (медиа хранится в Media:Storage:LocalBasePath вне проекта).
  build: {
    outDir: '../GastronomePlatform.WebAPI/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5195',
        changeOrigin: true,
      },
    },
  },
})
