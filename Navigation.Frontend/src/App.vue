<template>
  <Toaster position="top-center" :theme="isDark ? 'dark' : 'light'" richColors closeButton />

  <div class="flex h-screen bg-background overflow-hidden">
    <!-- 主内容区 -->
    <div class="flex-1 flex flex-col overflow-hidden relative">
      <main class="flex-1 overflow-y-auto relative bg-background/50">
        <div id="main-content" class="h-full">
          <RouterView />
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, computed, watch, ref } from 'vue'
import { RouterView, useRoute, useRouter } from 'vue-router'
import { useAppStore } from '@/stores/appStore'
import { useTheme } from '@/composables/useTheme'
import { detectBrowserLocale } from '@/locales'

import Toaster from '@/components/ui/sonner/Sonner.vue'

const gameStore = useAppStore()
const { isDark } = useTheme()
const route = useRoute()
const router = useRouter()

const isRouterSettled = ref(import.meta.env.SSR)

// 同步控制器：Vue 计算加载状态并控制 HTML 原生遮罩
const isLoading = computed(() => {
  if (route.query.loading === 'true') return true
  if (gameStore.globalLoading) return true

  // 核心拦截：仅针对携带特定 Key 的深层链接执行全局遮罩
  if (!isRouterSettled.value) {
    const p = route.path
    const isDeepWebNav = p.startsWith('/tools/web-nav/') && p.length > 15
    if (isDeepWebNav) return true
  }
  return false
})

watch(isLoading, (loading) => {
  if (typeof window === 'undefined') return
  if (loading) {
    document.documentElement.setAttribute('data-app-loading', 'true')
  } else {
    document.documentElement.removeAttribute('data-app-loading')
    // 当全局 Loading 结束时，顺便清理 Session 隔离标志，确保内容可见
    document.documentElement.removeAttribute('data-app-session')
  }
}, { immediate: true })

// 额外兜底：当路由稳定后，确保清理所有拦截状态
watch(isRouterSettled, (settled) => {
  if (settled && typeof window !== 'undefined') {
    document.documentElement.removeAttribute('data-app-loading')
    document.documentElement.removeAttribute('data-app-session')
  }
})

watch(() => route.query.loading, (val) => {
  if (val === 'true') {
    setTimeout(() => {
      const q = { ...route.query }; delete q.loading
      router.replace({ query: q })
    }, 1500)
  }
})

onMounted(async () => {
  await router.isReady()
  isRouterSettled.value = true

  if (!localStorage.getItem('webtool')) {
    gameStore.locale = detectBrowserLocale()
  }
})
</script>

<style>
/* 找回所有精致的 Toast 提示框样式 */
body>[data-sonner-toaster],
[data-sonner-toaster] {
  top: 20px !important;
  bottom: auto !important;
  left: 50% !important;
  right: auto !important;
  transform: translateX(-50%) !important;
  position: fixed !important;
  z-index: 999999 !important;
}

[data-sonner-toast] {
  width: fit-content !important;
  min-width: 280px !important;
  max-width: 400px !important;
  display: flex !important;
  flex-direction: row !important;
  align-items: center !important;
  padding: 12px 18px !important;
  margin: 8px auto !important;
  background: rgba(255, 255, 255, 0.75) !important;
  backdrop-filter: blur(12px) !important;
  -webkit-backdrop-filter: blur(12px) !important;
  border: 1px solid rgba(255, 255, 255, 0.4) !important;
  border-radius: 12px !important;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06), inset 0 0 0 1px rgba(255, 255, 255, 0.5) !important;
}

[data-sonner-toast] [data-icon] {
  display: none !important;
}

[data-sonner-toast] [data-content] {
  margin: 0 !important;
  display: flex !important;
  align-items: center !important;
  font-weight: 500 !important;
  font-size: 14px !important;
}

.dark [data-sonner-toast] {
  background: rgba(24, 24, 27, 0.8) !important;
  border: 1px solid rgba(255, 255, 255, 0.1) !important;
  color: #fff !important;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.5) !important;
}

.dark [data-sonner-toast] [data-close-button] {
  color: #a1a1aa !important;
}

.dark {
  color-scheme: dark;
}

.animate-fade-in {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
