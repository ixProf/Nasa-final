import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
  plugins: [react()],
  assetsInclude: ['**/*.glb', '**/*.gltf'],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: true,
    port: 3000,
    proxy: {
      '/api': {
        target: env.VITE_BACKEND_URL || 'http://localhost:5256',
        changeOrigin: true,
        secure: true,
      },
      '/hubs': {
        target: env.VITE_BACKEND_URL || 'http://localhost:5256',
        ws: true,
        changeOrigin: true,
        secure: true,
      },
    },
  },
  }
})
