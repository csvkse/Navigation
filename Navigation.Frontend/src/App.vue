<template>
  <Toaster position="top-center" :theme="isDark ? 'dark' : 'light'" richColors closeButton />

  <div class="flex h-screen bg-background overflow-hidden">
    <!-- 侧边栏遮罩(仅移动端) -->
    <div v-if="!gameStore.sidebarCollapsed" class="fixed inset-0 bg-black/50 z-30 lg:hidden" @click="toggleSidebar" />

    <!-- 侧边导航栏 -->
    <aside v-if="!shouldHideSidebar" :class="[
      'border-r bg-card flex flex-col transition-all duration-300 ease-in-out shadow-lg z-40',
      'fixed lg:relative h-full',
      gameStore.sidebarCollapsed ? '-translate-x-full lg:translate-x-0 lg:w-16' : 'translate-x-0 w-64'
    ]">
      <div class="p-4 border-b flex items-center justify-center">
        <h1 v-if="!gameStore.sidebarCollapsed" class="text-xl font-bold flex items-center gap-2">
          <span class="text-2xl"><img src="/logo.svg" class="w-10" /></span>
          {{ pkg.title }}
        </h1>
        <span v-else class="text-2xl"><img src="/logo.svg" class="w-10" /></span>
      </div>

      <nav class="flex-1 p-2 space-y-1 overflow-y-auto">
        <RouterLink v-for="item in navItems" :key="item.path" :to="item.path">
          <Button :variant="isNavItemActive(item.path) ? 'secondary' : 'ghost'"
            :class="['w-full transition-all', gameStore.sidebarCollapsed ? 'justify-center px-0' : 'justify-start']"
            :title="gameStore.sidebarCollapsed ? item.name.value : undefined">
            <component :is="item.icon" :class="['h-4 w-4', !gameStore.sidebarCollapsed && 'mr-3']" />
            <span v-if="!gameStore.sidebarCollapsed">{{ item.name.value }}</span>
          </Button>
        </RouterLink>
      </nav>

      <div class="p-2 border-t text-muted-foreground/30 text-[10px] text-center font-mono">
        v{{ pkg.version }}
      </div>

      <div class="p-2 border-t">
        <Popover>
          <PopoverTrigger as-child>
            <Button variant="ghost" class="w-full" size="sm">
              <Languages class="h-4 w-4" />
              <span v-if="!gameStore.sidebarCollapsed" class="ml-2">{{ localeNames[gameStore.locale] }}</span>
            </Button>
          </PopoverTrigger>
          <PopoverContent class="w-48 p-2" :align="gameStore.sidebarCollapsed ? 'start' : 'center'">
            <div class="space-y-1">
              <Button v-for="locale in locales" :key="locale" @click="gameStore.locale = locale"
                :variant="gameStore.locale === locale ? 'secondary' : 'ghost'" class="w-full justify-start" size="sm">
                {{ localeNames[locale] }}
              </Button>
            </div>
          </PopoverContent>
        </Popover>
      </div>

      <div class="p-2 border-t">
        <Button @click="isDark = !isDark" variant="ghost" class="w-full" size="sm">
          <Sun v-if="isDark" class="h-4 w-4" />
          <Moon v-else class="h-4 w-4" />
          <span v-if="!gameStore.sidebarCollapsed" class="ml-2">{{ isDark ? t('sidebar.lightMode') :
            t('sidebar.darkMode') }}</span>
        </Button>
      </div>

      <div class="p-2 border-t">
        <Button @click="toggleSidebar" variant="ghost" class="w-full" size="sm">
          <ChevronLeft v-if="!gameStore.sidebarCollapsed" class="h-4 w-4" />
          <ChevronRight v-else class="h-4 w-4" />
          <span v-if="!gameStore.sidebarCollapsed" class="ml-2">{{ t('sidebar.collapse') }}</span>
        </Button>
      </div>
    </aside>

    <!-- 主内容区 -->
    <div class="flex-1 flex flex-col overflow-hidden relative">
      <header v-if="showGlobalMobileHeader && !shouldHideSidebar"
        class="lg:hidden h-14 border-b bg-card/80 backdrop-blur-md flex items-center px-4 shrink-0 z-20">
        <Button variant="ghost" size="icon" class="h-9 w-9 text-muted-foreground mr-2"
          @click="gameStore.sidebarCollapsed = false">
          <Menu class="h-5 w-5" />
        </Button>
        <span class="font-bold text-sm tracking-tight uppercase truncate">{{ currentRouteTitle }}</span>
      </header>

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
import { RouterView, RouterLink, useRoute, useRouter } from 'vue-router'
import { useAppStore } from '@/stores/appStore'
import { useTheme } from '@/composables/useTheme'
import { useI18n } from '@/composables/useI18n'
import { localeNames, detectBrowserLocale, type Locale } from '@/locales'
import { Button } from '@/components/ui/button'
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover'

import {
  Home, FileText, Link, Globe, ListTree, StickyNote,
  ChevronLeft, ChevronRight, Menu, Moon, Sun, Languages, Dices
} from 'lucide-vue-next'
import Toaster from '@/components/ui/sonner/Sonner.vue'
import pkg from '../package.json'

const gameStore = useAppStore()
const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const isRouterSettled = ref(import.meta.env.SSR)

// 同步控制器：Vue 计算加载状态并控制 HTML 原生遮罩
const isLoading = computed(() => {
  if (route.query.loading === 'true') return true
  if (gameStore.globalLoading) return true

  // 核心拦截：仅针对携带特定 Key 的深层链接（而非基础工具路径）执行全局遮罩
  if (!isRouterSettled.value) {
    const p = route.path
    const isDeepWebNav = p.startsWith('/tools/web-nav/') && p.length > 15
    const isDeepTempNote = p.startsWith('/temp-note/') && p.length > 11
    if (isDeepWebNav || isDeepTempNote) return true
  }
  return false
})

watch(isLoading, (loading) => {
  if (typeof window === 'undefined') return
  if (loading) {
    document.documentElement.setAttribute('data-app-loading', 'true')
  } else {
    document.documentElement.removeAttribute('data-app-loading')
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

const showGlobalMobileHeader = computed(() => {
  if (!route.name) return false
  return !['temp-note', 'secure-chat', 'web-nav', 'message-board'].includes(route.name as string)
})

const currentRouteTitle = computed(() => {
  const item = navItems.find(i => isNavItemActive(i.path))
  return item ? item.name.value : t('nav.overview')
})

const shouldHideSidebar = computed(() => {
  if (route.name === 'web-nav' || route.path?.startsWith('/tools/web-nav')) return true
  if (route.name === 'message-board' || route.path?.startsWith('/tools/message-board')) return true
  return false
})

const isNavItemActive = (path: string) => {
  if (path === '/') return route.path === '/'
  if (path === '/temp-note' && route.path.startsWith('/temp-note')) return true
  return route.path.startsWith(path)
}

const locales: Locale[] = ['zh-CN', 'en']

onMounted(async () => {
  await router.isReady()
  isRouterSettled.value = true

  if (!localStorage.getItem('webtool')) {
    gameStore.locale = detectBrowserLocale()
  }

  // 兜底安全网：5秒后强制清除所有拦截标志，防止极端情况下内容永远不可见
  setTimeout(() => {
    if (document.documentElement.hasAttribute('data-app-loading')) {
      console.warn('[App] Force clearing data-app-loading after timeout')
      document.documentElement.removeAttribute('data-app-loading')
    }
    if (document.documentElement.hasAttribute('data-app-session')) {
      console.warn('[App] Force clearing data-app-session after timeout')
      document.documentElement.removeAttribute('data-app-session')
    }
  }, 5000)
})

const navItems = [
  { name: computed(() => t('nav.overview')), path: '/', icon: Home },
  { name: computed(() => t('nav.tempText')), path: '/temp-text', icon: FileText },
  { name: computed(() => t('nav.shortLink')), path: '/short-link', icon: Link },
  { name: computed(() => t('nav.dnsSettings')), path: '/dns-settings', icon: Globe },
  { name: computed(() => t('nav.tempMenu')), path: '/temp-menu', icon: ListTree },
  { name: computed(() => t('nav.tempNote')), path: '/temp-note', icon: StickyNote },
  { name: computed(() => t('nav.tools')), path: '/tools', icon: Dices }
]

const toggleSidebar = () => { gameStore.sidebarCollapsed = !gameStore.sidebarCollapsed }
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
