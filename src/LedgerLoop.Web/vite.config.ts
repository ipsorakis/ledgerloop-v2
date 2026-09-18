import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const apiTarget = process.env.LEDGERLOOP_API_URL ?? 'http://localhost:5146';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true }
    }
  },
  preview: { port: 4173, host: true },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./vitest.setup.ts'],
    include: ['src/**/*.test.ts', 'src/**/*.test.tsx']
  }
});
