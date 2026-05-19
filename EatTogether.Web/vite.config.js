import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd())

    return {
        plugins: [vue()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url)),
            },
        },
        server: {
            host: true,
            // allowedHosts:['repacking-tasting-satiable.ngrok-free.dev'], // 允許 ngrok 的域名，以便在 ngrok 提供的 URL 上測試 API 代理功能(菜單面板的 API 代理功能需要在 ngrok 提供的 URL 上測試，因為它是從 ngrok 的域名發出請求的)
            proxy: {
                '/api': {
                    target: env.VITE_API_TARGET,
                    changeOrigin: true,
                    secure: false,
                },
                '/images': {
                    target: env.VITE_ADMIN_TARGET,
                    changeOrigin: true,
                    secure: false,
                },
                '/uploads': {
                    target: env.VITE_API_TARGET,
                    changeOrigin: true,
                    secure: false,
                },
            },
        },
    }
})
