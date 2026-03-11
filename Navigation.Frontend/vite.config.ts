import { defineConfig } from 'vite'
import type { UserConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'
import { VitePWA } from 'vite-plugin-pwa'
import pkg from './package.json'

// 为 vite-ssg 扩展配置类型
interface ViteSSGConfig extends UserConfig {
  ssgOptions?: {
    script?: 'sync' | 'async' | 'defer' | 'module'
    formatting?: 'minify' | 'prettify' | 'none'
    includedRoutes?: (paths: string[]) => string[] | Promise<string[]>
  }
}

export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
  },
  base: '/',
  preview: {
    allowedHosts: true,
  },
  build: {
    outDir: 'docs',
    sourcemap: false,
    cssCodeSplit: true,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules/vue')) return 'vendor-vue'
          if (id.includes('node_modules/vue-router')) return 'vendor-vue'
          if (id.includes('node_modules/pinia')) return 'vendor-vue'
          if (id.includes('node_modules/reka-ui')) return 'vendor-ui'
          if (id.includes('node_modules/@vueuse/core')) return 'vendor-ui'
          if (id.includes('node_modules/crypto-js')) return 'vendor-crypto'
          if (id.includes('node_modules/lucide-vue-next')) return 'vendor-icons'
        }
      }
    },
  },
  plugins: [
    vue(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['apple-touch-icon.png', 'logo.svg'],
      manifest: {
        name: 'Navigation - 极简导航',
        short_name: 'Navigation',
        description: '一个功能丰富的网页版书签导航',
        theme_color: '#6366f1',
        background_color: '#ffffff',
        display: 'standalone',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable'
          }
        ]
      },
      workbox: {
        cleanupOutdatedCaches: true,
        skipWaiting: true,
        clientsClaim: true,
        maximumFileSizeToCacheInBytes: 5 * 1024 * 1024,
        globPatterns: ['**/*.{js,css,html,png,svg,woff,woff2,ttf,eot,json,webmanifest}'],
        navigateFallback: '/index.html'
      }
    })
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    }
  },
  ssgOptions: {
    script: 'async',
    formatting: 'minify',
    includedRoutes(paths) {
      // 1. 过滤掉包含 ':' (动态参数) 或 '?' (可选参数) 的非法模板路径（如 /:key?）
      const validPaths = paths.filter(p => !p.includes(':') && !p.includes('?'))

      // 2. 核心修改：移除旧的 '/tools/web-nav'，显式加入根路径 '/'
      // 当没有传入 key 时，/ 就会渲染出默认的 WebNav 首页。
      // 使用 Set 去重，防止 validPaths 中已经存在 '/' 或 '/temp-note' 导致重复渲染
      return Array.from(new Set([...validPaths, '/', '/temp-note']))
    }
  }
} as ViteSSGConfig)