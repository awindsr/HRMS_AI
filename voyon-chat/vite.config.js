import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In dev, proxy /api to the backend relay so the browser stays same-origin (no CORS, and
// EventSource streaming works through the proxy). Override the target with VITE_PROXY_TARGET
// if the API runs on a different port.
const target = globalThis.process?.env?.VITE_PROXY_TARGET || 'http://localhost:5047'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target, changeOrigin: true },
    },
  },
})
