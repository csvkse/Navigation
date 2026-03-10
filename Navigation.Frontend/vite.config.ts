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
        name: 'WebTool - 极简工具箱',
        short_name: 'WebTool',
        description: '一个功能丰富的网页版工具箱',
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
        maximumFileSizeToCacheInBytes: 5 * 1024 * 1024, // 将上限调至 5MB
        globPatterns: ['**/*.{js,css,html,png,svg,woff,woff2,ttf,eot,json,webmanifest}'],
        // 针对 vite-ssg 的路由处理
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
      // 1. 过滤掉包含 ':' (动态参数) 或 '?' (可选参数) 的非法路径
      // 这些路径无法直接生成静态文档，必须要有具体的参数才能生成
      const validPaths = paths.filter(p => !p.includes(':') && !p.includes('?'))

      // 2. 手动添加你需要生成的具体页面
      // 如果 /temp-note 页面在没有参数时也能访问，就手动加进去
      return [...validPaths, '/tools/web-nav', '/temp-note']
    }
  }
} as ViteSSGConfig)