import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    tailwindcss(),
    react({
      babel: {
        plugins: [['babel-plugin-react-compiler']],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    // Emit the SPA straight into the API's wwwroot so one
    // `dotnet run` serves UI and API together.
    outDir: '../KeepTabs/wwwroot',
    emptyOutDir: true,
  },
  server: {
    // Split dev loop: `pnpm dev` serves the UI, API calls proxy to the API.
    proxy: {
      '/api': 'http://localhost:5104',
    },
  },
})
