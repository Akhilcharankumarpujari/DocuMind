import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const BACKEND_TARGET = process.env.VITE_BACKEND_URL || 'http://127.0.0.1:5000'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: BACKEND_TARGET,
        changeOrigin: true,
        secure: false,
        timeout: 15000,
      },
    },
  },
  preview: {
    port: 5173,
    proxy: {
      '/api': {
        target: BACKEND_TARGET,
        changeOrigin: true,
        secure: false,
        timeout: 15000,
      },
    },
  },
})
