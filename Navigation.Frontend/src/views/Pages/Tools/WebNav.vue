<template>
    <div :class="['container mx-auto p-3 sm:p-6 space-y-6 max-w-7xl min-h-screen relative',
        currentThemeConfig?.containerClass || '']" @dragover="handleDragOver" @dragenter="handleDragEnter"
        @drop="handleGlobalDrop" @paste="handleGlobalPaste">

        <!-- Ambient Background -->
        <WebNavBackground :theme-id="layoutScheme" />

        <!-- Header Component -->
        <WebNavHeader :current-key="currentKey" :current-user="currentUser" :current-time="currentTime"
            :search-query="searchQuery" :is-dark="isDark" :is-manage-mode="isManageMode" :layout-scheme="layoutScheme"
            :day-scheme="dayScheme" :night-scheme="nightScheme" :schemes="WEBNAV_THEMES" :user-keys="userBookmarkKeys"
            :active-user-key="activeUserKey" :is-read-only="isReadOnly" @update:search-query="searchQuery = $event"
            @toggle-theme="isDark = !isDark" @toggle-manage="isManageMode = !isManageMode" :manage-tab="manageTab"
            @update:manage-tab="manageTab = $event" @open-modal="openModal()" @open-auth="openAuthModal"
            @logout="handleLogout" @switch-key="handleSwitchKey" @update:day-scheme="dayScheme = $event"
            @update:night-scheme="nightScheme = $event" @bulk-export="handleBulkExport" @clear-all="handleClearAll"
            @create-user-key="handleCreateUserKey" @select-user-key="handleSelectUserKey"
            @delete-user-key="handleDeleteUserKey" @copy-read-only="handleCopyReadOnly" />

        <!-- Dashboard Content -->
        <WebNavManageView v-if="isManageMode && manageTab === 'data'" :filtered-links="filteredLinks"
            :dragging-id="draggingId" :loading="loading" :category-order="categoryOrder" @drag-start="handleDragStart"
            @drag-end="handleDragEnd" @drop="handleDrop" @edit-link="openModal" @delete-link="handleDelete"
            @toggle-apply="toggleApply" @toggle-is-app="toggleIsApp" @clear-all="handleClearAll"
            @update-category-order="handleUpdateCategoryOrder" @disable-category="handleCategoryDisable"
            @delete-category="handleCategoryDelete" />

        <!-- Navigation View -->
        <!-- 骨架屏状态 -->
        <div v-else-if="loading && links.length === 0" class="space-y-12 py-6">
            <section class="space-y-4">
                <div class="h-4 w-24 bg-muted/40 rounded-full" />
                <div class="grid grid-cols-4 sm:grid-cols-6 lg:grid-cols-8 gap-4">
                    <div v-for="i in 8" :key="i" class="aspect-square bg-muted/20 rounded-3xl" />
                </div>
            </section>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
                <div v-for="i in 4" :key="i" class="space-y-4">
                    <div class="h-4 w-20 bg-muted/40 rounded-full" />
                    <div class="space-y-2">
                        <div v-for="j in 5" :key="j" class="h-10 bg-muted/10 rounded-xl" />
                    </div>
                </div>
            </div>
        </div>

        <div v-else-if="filteredLinks.length === 0"
            class="flex flex-col items-center justify-center py-32 rounded-3xl bg-muted/20 border border-dashed border-border/50 overflow-hidden relative">
            <div class="absolute inset-0 bg-gradient-to-b from-transparent to-primary/5 pointer-events-none" />
            <div class="p-6 bg-background/50 rounded-full shadow-xl border border-border/50 mb-6 relative">
                <Compass class="h-10 w-10 text-muted-foreground/30" />
            </div>
            <p class="text-base text-muted-foreground font-medium relative">{{ searchQuery ?
                t('tools.webNav.noMatchingResults') :
                t('tools.webNav.noLinks') }}</p>
            <Button v-if="!searchQuery && !isReadOnly" variant="link"
                class="mt-2 text-primary/60 hover:text-primary transition-colors relative" @click="openModal()">
                {{ t('tools.webNav.addFirst') }}
            </Button>
        </div>

        <div v-else class="space-y-10" @dragover.prevent
            @drop="$emit('drop', $event, { content: { isApp: false, category: '', isRootDrop: true } })">
            <!-- App Grid Section -->
            <WebNavAppGrid :app-groups="appGroups" :layout-scheme="layoutScheme"
                :is-manage-mode="isManageMode && manageTab === 'drag'" :dragging-id="draggingId" :icons="ICONS"
                @drag-start="handleDragStart" @drag-end="handleDragEnd" @drop="handleDrop" @open-link="openLink"
                @edit-link="openModal" />

            <!-- Bookmark Columns -->
            <WebNavBookmarks :bookmark-groups="bookmarkGroups" :grouped-bookmarks="groupedBookmarks"
                :is-manage-mode="isManageMode && manageTab === 'drag'" :dragging-id="draggingId"
                :layout-scheme="layoutScheme" :category-order="categoryOrder" @drag-start="handleDragStart"
                @drag-end="handleDragEnd" @drop="handleDrop" @open-link="openLink" @edit-link="openModal"
                @delete-link="handleDelete" @update-category-order="handleUpdateCategoryOrder"
                @disable-category="handleCategoryDisable" @delete-category="handleCategoryDelete" />
        </div>

        <!-- Link Modal -->
        <WebNavLinkModal :open="modal.open" :is-edit="modal.isEdit" :form="modal.form" :saving="saving" :icons="ICONS"
            :recommended-colors="RECOMMENDED_COLORS" @update:open="modal.open = $event" @save="handleSave" />

        <!-- Auth Modal -->
        <AuthModal :open="auth.open" :mode="auth.mode" :form="auth.form" :loading="auth.loading"
            @update:open="auth.open = $event" @update:mode="auth.mode = $event" @login="wrappedHandleLogin"
            @register="handleRegister" @update-profile="wrappedHandleUpdate" />

        <!-- Bulk Modal -->
        <WebNavBulkModal :open="bulkModal.open" :text="bulkModal.text" :loading="bulkModal.loading"
            :is-read-only="isReadOnly" @update:open="bulkModal.open = $event" @update:text="bulkModal.text = $event"
            @import="handleBulkImport" />

        <WebNavSuspended :suspended-links="suspendedLinks" :is-manage-mode="isManageMode && manageTab === 'drag'"
            @drop="handleDrop" @drag-start="handleDragStart" @drag-end="handleDragEnd" @delete-link="handleDelete"
            @restore-link="handleRestoreLink" @restore-all="handleRestoreAll" />

        <!-- Scroll To Top Button -->
        <Transition enter-active-class="transition-all duration-300 ease-out"
            enter-from-class="opacity-0 translate-y-4 scale-90" enter-to-class="opacity-100 translate-y-0 scale-100"
            leave-active-class="transition-all duration-300 ease-in"
            leave-from-class="opacity-100 translate-y-0 scale-100" leave-to-class="opacity-0 translate-y-4 scale-90">
            <button v-show="showScrollTop" @click="scrollToTop"
                class="fixed bottom-6 right-6 z-50 p-3 rounded-full bg-primary text-primary-foreground shadow-lg hover:shadow-xl hover:bg-primary/90 hover:-translate-y-1 active:translate-y-0 active:scale-95 transition-all duration-300 group">
                <ArrowUp class="h-6 w-6 transition-transform group-hover:-translate-y-0.5" />
            </button>
        </Transition>

        <!-- Footer with Version & GitHub -->
        <div class="text-center text-sm text-muted-foreground/40 py-8 space-y-2 mt-8">
            <div>v{{ version }}</div>
            <a href="https://github.com/csvkse/Navigation" target="_blank"
                class="hover:text-primary/80 transition-colors inline-flex items-center space-x-1">
                <Github class="w-4 h-4" />
                <span>GitHub</span>
            </a>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, shallowRef } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from '@/composables/useI18n'
import { useTheme } from '@/composables/useTheme'
import { Button } from '@/components/ui/button'
import { Compass, Bookmark, Layers, Globe, Activity, Box, ArrowUp } from 'lucide-vue-next'
import { Gamepad, Layout, Terminal, Cloud, Database, Cpu, MessageSquare, Monitor, Link, Github, Twitter, Mail, ExternalLink, Shield, Server, Zap } from 'lucide-vue-next'
import { toast } from 'vue-sonner'
import CryptoJS from 'crypto-js'

// Import子组件
import WebNavHeader from './WebNav/WebNavHeader.vue'
import WebNavAppGrid from './WebNav/WebNavAppGrid.vue'
import WebNavBookmarks from './WebNav/WebNavBookmarks.vue'
import WebNavManageView from './WebNav/WebNavManageView.vue'
import WebNavBackground from './WebNav/WebNavBackground.vue'
import WebNavSuspended from './WebNav/WebNavSuspended.vue'
import { WEBNAV_THEMES, DEFAULT_THEME_ID, DEFAULT_THEME_ID_Night } from '@/config/webNav/webNavTheme'
import { useAppStore } from '@/stores/appStore'
import WebNavLinkModal from './WebNav/WebNavLinkModal.vue'
import AuthModal from '@/components/AuthModal.vue'
import WebNavBulkModal from './WebNav/WebNavBulkModal.vue'
import { useAuth } from '@/composables/useAuth'

const version = __APP_VERSION__

const ICONS: Record<string, any> = {
    Gamepad, Layout, Globe, Activity, Terminal, Cloud, Database, Cpu, MessageSquare, Monitor, Bookmark, Link, Layers,
    Github, Twitter, Mail, ExternalLink, Shield, Server, Box, Zap
}

const RECOMMENDED_COLORS = [
    '#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899',
    '#06b6d4', '#84cc16', '#f43f5e', '#a855f7', '#14b8a6', '#64748b'
]

interface LinkContent {
    name: string
    url: string
    category?: string
    icon?: string
    color?: string
    isApp?: boolean
    isApplied?: boolean
    sortIndex?: number
}

interface LinkRecord {
    id: string
    content: LinkContent
    hashKey?: string
    createTime?: string
    updateTime?: string
}

const BASE_URL = import.meta.env.VITE_API_BASE_URL || ''
const STATIC_SALT = 'webnav_an_v1_920930589'

const generateSecureKey = (username: string) => {
    const random = Math.random().toString(36).substring(2) + Math.random().toString(36).substring(2)
    const timestamp = Date.now().toString()
    return CryptoJS.SHA256(`${username}:${STATIC_SALT}:${random}:${timestamp}`).toString()
}

// Manage Mode Tabs
const manageTab = ref<'drag' | 'data'>('drag')

// Scroll to Top Logic
const showScrollTop = ref(false)
const scrollToTop = () => {
    const scrollContainer = document.querySelector('main.overflow-y-auto') || document.querySelector('#main-content')?.parentElement
    if (scrollContainer) {
        scrollContainer.scrollTo({ top: 0, behavior: 'smooth' })
    } else {
        window.scrollTo({ top: 0, behavior: 'smooth' })
    }
}

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const { isDark } = useTheme()

// --- Dashboard Time ---
let timeInterval: any
const currentTime = ref({
    date: '',
    time: '',
    day: ''
})

const updateTime = () => {
    const now = new Date()
    const options: Intl.DateTimeFormatOptions = {
        year: 'numeric', month: 'long', day: 'numeric',
        weekday: 'long', hour: '2-digit', minute: '2-digit', second: '2-digit',
        hour12: false
    }
    const parts = new Intl.DateTimeFormat((locale.value as string) === 'zh' ? 'zh-CN' : 'en-US', options).formatToParts(now)
    const find = (type: string) => parts.find(p => p.type === type)?.value || ''

    if ((locale.value as string) === 'zh') {
        currentTime.value = {
            date: `${find('year')}年${find('month')}${find('day')}日`,
            day: find('weekday'),
            time: `${find('hour')}:${find('minute')}:${find('second')}`
        }
    } else {
        currentTime.value = {
            date: `${find('month')} ${find('day')}, ${find('year')}`,
            day: find('weekday'),
            time: `${find('hour')}:${find('minute')}:${find('second')}`
        }
    }
}

// --- State ---
const {
    currentUser, auth,
    openAuthModal, handleLogin, handleRegister, handleUpdateProfile, handleLogout,
    initAuth
} = useAuth(t)

initAuth()

const dayScheme = ref(DEFAULT_THEME_ID)
const nightScheme = ref(DEFAULT_THEME_ID_Night)

if (typeof window !== 'undefined') {
    const storedDay = localStorage.getItem('webnav_theme_day')
    const storedNight = localStorage.getItem('webnav_theme_night')
    if (storedDay) dayScheme.value = storedDay
    if (storedNight) nightScheme.value = storedNight
}

watch(dayScheme, (val) => {
    localStorage.setItem('webnav_theme_day', val)
    saveThemeConfig()
})
watch(nightScheme, (val) => {
    localStorage.setItem('webnav_theme_night', val)
    saveThemeConfig()
})

const layoutScheme = computed(() => isDark.value ? nightScheme.value : dayScheme.value)
const currentThemeConfig = computed(() => WEBNAV_THEMES.find(t => t.id === layoutScheme.value) || WEBNAV_THEMES[0])

const activeUserKey = ref('')
const userBookmarkKeys = computed(() => currentUser.value?.bookmarkKeys || [])

const defaultKey = '562f5a89-a155-4292-b16f-b218bc99d2b0'

const currentKey = computed(() => {
    const routeKey = route.params.key as string
    if (routeKey) return routeKey

    if (currentUser.value) {
        return `u_${activeUserKey.value || currentUser.value.dataKey}`
    }

    return defaultKey
})

const getCacheKey = (key: string) => `webnav_cache_${key}`

const initFromCache = () => {
    if (typeof window === 'undefined') return []

    let keyToUse = currentKey.value

    if ((!keyToUse || keyToUse === defaultKey) && !currentUser.value) {
        const pathParts = window.location.pathname.split('/')
        if (pathParts.includes('web-nav') && pathParts.length > 3) {
            keyToUse = decodeURIComponent(pathParts[pathParts.length - 1] || '')
        }
    }

    if (keyToUse) {
        if (keyToUse === defaultKey) return []

        const cacheKey = getCacheKey(keyToUse)
        const cached = localStorage.getItem(cacheKey)
        if (cached) {
            try {
                return JSON.parse(cached)
            } catch (e) { return [] }
        }
    }
    return []
}

const links = shallowRef<LinkRecord[]>(initFromCache())
const loading = ref(currentKey.value === defaultKey ? false : links.value.length === 0)
const isInitialLoading = ref(currentKey.value === defaultKey ? false : links.value.length === 0)
const appStore = useAppStore()

if (currentKey.value && links.value.length === 0 && currentKey.value !== defaultKey) {
    // appStore.globalLoading = true // Disable global full-screen mask, use skeleton instead
}

const saving = ref(false)
const searchQuery = ref('')
const isManageMode = ref(false)
const isReadOnly = ref(false)

watch(isManageMode, (val) => {
    if (val && window.innerWidth < 640) {
        manageTab.value = 'data'
    }
})

const modal = ref({
    open: false,
    isEdit: false,
    id: '',
    form: { name: '', url: '', category: '', icon: '', color: '', isApp: false, isApplied: true, sortIndex: 0 }
})

const draggingId = ref<string | null>(null)

const bulkModal = ref({
    open: false,
    text: '',
    loading: false
})

const themeConfigId = ref<string>('')
let saveThemeTimer: any = null
let themeConfigDirty = false

const categoryOrderId = ref<string>('')
const categoryOrder = ref<Record<string, number>>({})
let saveCategoryOrderTimer: any = null

const doSaveCategoryOrder = async () => {
    if (isReadOnly.value) return
    const payload = {
        type: 'WEBNAV_CATEGORY_ORDER',
        order: categoryOrder.value
    }

    try {
        const url = categoryOrderId.value
            ? `${BASE_URL}/FastDB/${categoryOrderId.value}?key=${encodeURIComponent(currentKey.value)}`
            : `${BASE_URL}/FastDB?key=${encodeURIComponent(currentKey.value)}`

        const method = categoryOrderId.value ? 'PUT' : 'POST'

        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })

        if (res.ok && !categoryOrderId.value) {
            const data = await res.json()
            if (data.id) categoryOrderId.value = data.id
        }
    } catch (e) {
        console.error('Failed to save category order', e)
    }
}

const saveCategoryOrder = () => {
    if (isReadOnly.value) return
    clearTimeout(saveCategoryOrderTimer)
    saveCategoryOrderTimer = setTimeout(doSaveCategoryOrder, 1500)
}

const handleUpdateCategoryOrder = (newOrder: Record<string, number>) => {
    categoryOrder.value = newOrder
    saveCategoryOrder()
}

const doSaveThemeConfig = async () => {
    if (isReadOnly.value) return
    themeConfigDirty = false
    const payload = {
        type: 'WEBNAV_THEME_CONFIG',
        day: dayScheme.value,
        night: nightScheme.value
    }

    try {
        const url = themeConfigId.value
            ? `${BASE_URL}/FastDB/${themeConfigId.value}?key=${encodeURIComponent(currentKey.value)}`
            : `${BASE_URL}/FastDB?key=${encodeURIComponent(currentKey.value)}`

        const method = themeConfigId.value ? 'PUT' : 'POST'

        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })

        if (res.ok && !themeConfigId.value) {
            const data = await res.json()
            if (data.id) themeConfigId.value = data.id
        }
    } catch (e) {
        console.error('Failed to save theme config', e)
    }
}

const saveThemeConfig = () => {
    if (isReadOnly.value) return
    themeConfigDirty = true
    clearTimeout(saveThemeTimer)
    saveThemeTimer = setTimeout(doSaveThemeConfig, 1500)
}

// 页面离开前立即保存未完成的主题配置
const flushThemeConfig = () => {
    if (themeConfigDirty) {
        clearTimeout(saveThemeTimer)
        // 使用 sendBeacon 确保离开页面时也能发送
        const payload = {
            type: 'WEBNAV_THEME_CONFIG',
            day: dayScheme.value,
            night: nightScheme.value
        }
        const url = themeConfigId.value
            ? `${BASE_URL}/FastDB/${themeConfigId.value}?key=${encodeURIComponent(currentKey.value)}`
            : `${BASE_URL}/FastDB?key=${encodeURIComponent(currentKey.value)}`
        navigator.sendBeacon(url, new Blob([JSON.stringify(payload)], { type: 'application/json' }))
        themeConfigDirty = false
    }
}

// ===== 辅助函数: 从 URL 生成智能标题 =====
const generateTitleFromUrl = (url: string): string => {
    try {
        const urlObj = new URL(url)

        // =========================================================
        // 1. [优先级变更] 优先尝试从域名生成 (原步骤 3 提前)
        // =========================================================
        let hostname = urlObj.hostname.replace(/^www\./, '')
        const parts = hostname.split('.')

        // 移除顶级域名 (如 .com, .cn, .net)，保留主要部分
        if (parts.length > 1) {
            parts.pop()
        }

        const domainName = parts
            .join(' ')
            .replace(/[-_]/g, ' ') // 处理域名中的连字符
            .replace(/\b\w/g, c => c.toUpperCase()) // 首字母大写

        // 如果域名解析出的内容有效且长度足够，直接返回
        if (domainName && domainName.trim().length >= 2) {
            return domainName
        }

        // =========================================================
        // 2. [降级] 尝试从路径提取 (原步骤 1)
        // =========================================================
        const pathSegments = urlObj.pathname
            .split('/')
            .filter(p => {
                if (!p) return false
                const lower = p.toLowerCase()
                return !['index.html', 'index.php', 'index.htm', 'index.jsp',
                    'default.html', 'home.html', 'index', 'home', 'default'].includes(lower)
            })

        if (pathSegments.length > 0) {
            const lastSegment = pathSegments[pathSegments.length - 1] || ''
            const segment = lastSegment.replace(/\.(html?|php|aspx?|jsp|htm)$/i, '')
            const withoutQ = segment.split('?')[0] ?? segment
            const withoutExt = withoutQ.split('#')[0] ?? withoutQ

            const title = withoutExt
                .replace(/[-_]/g, ' ')
                .replace(/([a-z])([A-Z])/g, '$1 $2') // 处理驼峰
                .split(' ')
                .map(word => {
                    // 如果是全大写且较短（如 ID, API），保持原样，否则首字母大写
                    if (word === word.toUpperCase() && word.length <= 4) {
                        return word
                    }
                    return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
                })
                .join(' ')
                .trim()

            if (title.length >= 3) {
                return title
            }
        }

        // =========================================================
        // 3. [降级] 尝试从查询参数提取 (原步骤 2)
        // =========================================================
        const queryTitle = urlObj.searchParams.get('title') ||
            urlObj.searchParams.get('name') ||
            urlObj.searchParams.get('page')
        if (queryTitle && queryTitle.length >= 3) {
            return queryTitle
                .replace(/[-_]/g, ' ')
                .replace(/\b\w/g, c => c.toUpperCase())
        }

        // 4. 最后保底：直接返回处理过的 hostname
        return domainName || hostname

    } catch (error) {
        console.error('❌ URL parsing failed:', error)
        return url.substring(0, 50)
    }
}

// ===== 统一的数据提取函数 (支持拖拽和粘贴) =====
const extractDropOrPasteData = (e: DragEvent | ClipboardEvent) => {
    let dt: DataTransfer | null = null

    if ('clipboardData' in e) {
        dt = e.clipboardData
    } else if ('dataTransfer' in e) {
        dt = e.dataTransfer
    }

    if (!dt) {
        console.error('❌ No DataTransfer object')
        return null
    }

    // 详细调试日志
    console.group('📦 DataTransfer Analysis')
    console.log('Types:', Array.from(dt.types))
    console.log('Files:', dt.files.length)

    // 打印所有可用数据
    dt.types.forEach(type => {
        const data = dt!.getData(type)
        console.log(`${type} (${data.length} chars):`, data.substring(0, 200))
    })
    console.groupEnd()

    // 提取 URL
    let url = dt!.getData('text/uri-list')?.split('\n')[0]?.trim() ||
        dt!.getData('text/plain')?.trim() ||
        dt!.getData('URL')?.trim() ||
        ''

    if (!url) {
        console.error('❌ No URL found')
        return null
    }

    // 验证 URL 格式
    try {
        new URL(url.startsWith('http') ? url : 'https://' + url)
    } catch {
        console.error('❌ Invalid URL:', url)
        return null
    }

    console.log('🔗 Extracted URL:', url)

    let title = ''

    // 1. 最高优先级: Firefox 的 text/x-moz-url 格式
    try {
        const mozUrl = dt!.getData('text/x-moz-url')
        console.log('🦊 text/x-moz-url:', mozUrl)

        if (mozUrl) {
            const parts = mozUrl.split('\n')
            if (parts.length >= 2 && parts[1]?.trim()) {
                title = parts[1].trim()
                console.log('✅ Title from text/x-moz-url:', title)
                return { url, title }
            }
        }
    } catch (e) {
        console.warn('Failed to read text/x-moz-url:', e)
    }

    // 2. 次优先级: 解析 HTML
    if (!title) {
        try {
            let textHTML = dt!.getData('text/html')
            console.log('📄 Raw HTML length:', textHTML?.length || 0)

            if (textHTML) {
                // 完整打印 HTML 用于调试 (可选)
                if (textHTML.length < 1000) {
                    console.log('📄 Full HTML:', textHTML)
                }

                const doc = new DOMParser().parseFromString(textHTML, 'text/html')

                // 优先级 1: 查找 <title> 标签
                const titleElement = doc.querySelector('title')
                if (titleElement?.textContent?.trim()) {
                    title = titleElement.textContent.trim()
                    console.log('✅ Title from <title> tag:', title)
                    return { url, title }
                }

                // 优先级 2: 查找 <a> 标签的文本
                const anchor = doc.querySelector('a')
                if (anchor) {
                    const anchorText = anchor.textContent?.trim() || ''
                    const anchorHref = anchor.getAttribute('href')

                    if (anchorText &&
                        anchorText !== anchorHref &&
                        anchorText !== url &&
                        anchorText.toLowerCase() !== 'untitled') {
                        title = anchorText
                        console.log('✅ Title from <a> tag:', title)
                        return { url, title }
                    }
                }

                // 优先级 3: 查找 meta 标签
                const metaTitle = doc.querySelector('meta[property="og:title"]') ||
                    doc.querySelector('meta[name="title"]')
                if (metaTitle?.getAttribute('content')?.trim()) {
                    title = metaTitle.getAttribute('content')!.trim()
                    console.log('✅ Title from <meta> tag:', title)
                    return { url, title }
                }

                // 优先级 4: 尝试从 body 提取
                if (doc.body) {
                    const bodyText = doc.body.textContent?.trim() || ''

                    if (bodyText &&
                        bodyText !== url &&
                        bodyText.length > 3 &&
                        bodyText.length < 200 &&
                        !bodyText.includes('\n')) {
                        title = bodyText
                        console.log('✅ Title from body text:', title)
                        return { url, title }
                    }
                }

                // 特殊处理: 检测纯 URL 拖拽
                const plainTextFromHtml = doc.body?.textContent?.trim() || ''
                if (plainTextFromHtml === url) {
                    console.log('⚠️ Detected plain URL drag (no title in HTML)')
                    title = ''
                }
            }
        } catch (error) {
            console.error('❌ HTML parsing failed:', error)
        }
    }

    // 3. 最后的 Fallback: 从 URL 智能生成标题
    if (!title) {
        title = generateTitleFromUrl(url)
        console.log('🔧 Generated title from URL:', title)
    }

    return { url, title }
}

// ===== 统一的保存逻辑 =====
const saveBookmark = async (url: string, title: string, target: any) => {
    const category = target.content?.category || t('tools.webNav.defaultCategory')
    const isApp = target.content?.isApp === true

    const newLink = {
        name: title.substring(0, 50),
        url: url,
        category: category,
        icon: '',
        isApp: isApp,
        isApplied: true,
        sortIndex: 0
    }

    saving.value = true
    try {
        const res = await fetch(
            `${BASE_URL}/FastDB?key=${encodeURIComponent(currentKey.value)}`,
            {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newLink)
            }
        )

        if (res.ok) {
            toast.success(t('tools.webNav.added') + ': ' + title)
            await fetchData(true)
        } else {
            throw new Error('Server returned non-OK status')
        }
    } catch (error) {
        console.error('Save failed:', error)
        toast.error(t('tools.webNav.addFailed'))
    } finally {
        saving.value = false
    }
}

// --- Core Logic ---
const safeParse = (content: any): any => {
    if (!content) return {}
    if (typeof content !== 'string') return content
    try {
        let parsed = JSON.parse(content)
        let depth = 0
        while (typeof parsed === 'string' && depth < 3) {
            parsed = JSON.parse(parsed)
            depth++
        }
        return parsed
    } catch (e) {
        return { _raw: content, _error: true }
    }
}

const fetchData = async (silent = false) => {
    if (!currentKey.value) return
    const currentKeySnap = currentKey.value

    if (currentKeySnap === defaultKey) {
        links.value = []
        loading.value = false
        isInitialLoading.value = false
        if (!silent) appStore.globalLoading = false
        isReadOnly.value = true
        isManageMode.value = false
        return
    }

    const cacheKey = getCacheKey(currentKeySnap)

    if (links.value.length === 0 && !silent) {
        loading.value = true
        isInitialLoading.value = true
    }

    const keyStr = currentKeySnap
    const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(keyStr)
    isReadOnly.value = isGuid
    if (isGuid) isManageMode.value = false

    try {
        const url = isGuid
            ? `${BASE_URL}/FastDB/ReadOnly?ReadOnlyId=${keyStr}`
            : `${BASE_URL}/FastDB?key=${encodeURIComponent(keyStr)}`

        const res = await fetch(url)
        if (!res.ok) throw new Error('Network error')
        const data = await res.json()

        const rawItems = (data || []).map((item: any) => ({
            ...item,
            content: safeParse(item.content)
        }))

        const configItem = rawItems.find((item: any) => item.content?.type === 'WEBNAV_THEME_CONFIG')
        if (configItem) {
            themeConfigId.value = configItem.id
            // localStorage 优先：只有在本地没有存储值时，才用服务端的值
            const hasLocalDay = localStorage.getItem('webnav_theme_day')
            const hasLocalNight = localStorage.getItem('webnav_theme_night')
            if (!hasLocalDay && configItem.content.day) dayScheme.value = configItem.content.day
            if (!hasLocalNight && configItem.content.night) nightScheme.value = configItem.content.night
        }

        const orderItem = rawItems.find((item: any) => item.content?.type === 'WEBNAV_CATEGORY_ORDER')
        if (orderItem) {
            categoryOrderId.value = orderItem.id
            categoryOrder.value = orderItem.content.order || {}
        }

        const newData: LinkRecord[] = rawItems.filter((item: any) =>
            item.content?.type !== 'READ_ONLY_ANCHOR' &&
            item.content?.type !== 'WEBNAV_THEME_CONFIG' &&
            item.content?.type !== 'WEBNAV_CATEGORY_ORDER'
        )

        const currentLinks = links.value
        const currentIdMap = new Map(currentLinks.map(l => [l.id, l]))
        let hasMeaningfulChange = currentLinks.length !== newData.length

        const mergedLinks = newData.map((newItem: LinkRecord) => {
            const oldItem = currentIdMap.get(newItem.id)
            if (oldItem) {
                // If updateTime matches, skip deep comparison
                if (newItem.updateTime && oldItem.updateTime === newItem.updateTime) {
                    return oldItem
                }
                
                // fallback comparison for items without updateTime
                const oldContentStr = JSON.stringify(oldItem.content)
                const newContentStr = JSON.stringify(newItem.content)

                if (oldContentStr === newContentStr) {
                    return oldItem
                }
                hasMeaningfulChange = true
                return newItem
            }
            hasMeaningfulChange = true
            return newItem
        })

        if (hasMeaningfulChange) {
            links.value = mergedLinks
            localStorage.setItem(cacheKey, JSON.stringify(newData))
        }
    } catch (e) {
        console.error('FetchData Error:', e)
        if (links.value.length === 0) {
            toast.error('Failed to load data')
        }
    } finally {
        loading.value = false
        isInitialLoading.value = false
        appStore.globalLoading = false
    }
}

const updateCache = () => {
    if (typeof window === 'undefined') return
    const cacheKey = getCacheKey(currentKey.value)
    if (cacheKey && links.value.length > 0) {
        localStorage.setItem(cacheKey, JSON.stringify(links.value))
    }
}

// ===== 新增: dragover 和 dragenter 处理 =====
const handleDragOver = (e: DragEvent) => {
    e.preventDefault()
    e.stopPropagation()

    if (e.dataTransfer) {
        if (draggingId.value) {
            e.dataTransfer.dropEffect = 'move'
        } else {
            e.dataTransfer.dropEffect = 'copy'
        }
    }
}

const handleDragEnter = (e: DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

// --- Drag & Drop ---
const handleDragStart = (e: DragEvent, link: any) => {
    if (!isManageMode.value || isReadOnly.value) return e.preventDefault()
    const id = link.id || ''
    draggingId.value = id
    if (e.dataTransfer && id) {
        e.dataTransfer.effectAllowed = 'move'
        e.dataTransfer.setData('text/plain', id)
    }
}

const handleDragEnd = () => {
    draggingId.value = null
}

const handleDrop = async (e: DragEvent, target: any) => {
    e.preventDefault()
    e.stopPropagation()

    const sourceId = e.dataTransfer?.getData('text/plain')

    // 0. Folder Move or Suspend
    if (draggingId.value && sourceId?.startsWith('folder:') && !isReadOnly.value) {
        const oldPrefix = sourceId.substring(7)

        // 0a. Folder Suspend (drag to suspended area)
        if (target.content?.isApplied === false) {
            const bulkItems: { id: string; content: any }[] = []
            links.value.forEach(l => {
                const cat = l.content.category || ''
                if (cat === oldPrefix || cat.startsWith(oldPrefix + ' /')) {
                    l.content.isApplied = false
                    bulkItems.push({ id: l.id, content: { ...l.content } })
                }
            })

            if (bulkItems.length > 0) {
                links.value = [...links.value]
                updateCache()

                fetch(`${BASE_URL}/FastDB/bulk?key=${encodeURIComponent(currentKey.value)}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(bulkItems)
                }).then(() => {
                    toast.success(`${oldPrefix}: ${bulkItems.length} links suspended`)
                    fetchData(true)
                }).catch(() => {
                    toast.error(t('tools.webNav.moveFailed') || 'Suspend failed')
                    fetchData(true)
                })
            }

            draggingId.value = null
            return
        }

        let droppedCategory = target.content?.category || t('tools.webNav.defaultCategory')

        // Support dropping to root
        if (target.content?.isRootDrop) {
            droppedCategory = ''
        }

        // Cannot drop into itself or its own children
        if ((droppedCategory && droppedCategory === oldPrefix) || droppedCategory.startsWith(oldPrefix + ' /')) {
            toast.error(t('tools.webNav.moveFailed') || 'Cannot move folder into itself')
            draggingId.value = null
            return
        }

        const leafName = oldPrefix.split('/').pop()?.trim() || ''
        const newPrefix = droppedCategory ? (leafName ? `${droppedCategory} / ${leafName}` : droppedCategory) : leafName

        let changedCount = 0
        const bulkItems: { id: string; content: any }[] = []

        links.value.forEach(l => {
            const cat = l.content.category || ''
            if (cat === oldPrefix || cat.startsWith(oldPrefix + ' /')) {
                const suffix = cat.substring(oldPrefix.length)
                l.content.category = newPrefix + suffix
                changedCount++
                bulkItems.push({ id: l.id, content: { ...l.content } })
            }
        })

        if (changedCount > 0) {
            links.value = [...links.value]
            updateCache()

            fetch(`${BASE_URL}/FastDB/bulk?key=${encodeURIComponent(currentKey.value)}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(bulkItems)
            }).then(() => {
                toast.success(t('tools.webNav.moved') || 'Folder moved')
                fetchData(true)
            }).catch(() => {
                toast.error(t('tools.webNav.moveFailed') || 'Move failed')
                fetchData(true)
            })
        }

        draggingId.value = null
        return
    }

    // 1. Internal Move (Link)
    if (draggingId.value && sourceId && sourceId !== target.id && !isReadOnly.value && !sourceId.startsWith('folder:')) {
        const source = links.value.find(l => l.id === sourceId)
        if (source) {
            if (target.content.isApplied !== undefined) {
                source.content.isApplied = target.content.isApplied
            } else {
                source.content.isApplied = true
            }

            source.content.isApp = target.content.isApp !== undefined ? !!target.content.isApp : source.content.isApp
            source.content.category = target.content.category !== undefined ? target.content.category : source.content.category
            source.content.sortIndex = (target.content.sortIndex || 0) - 1

            links.value = [...links.value]
            updateCache()

            try {
                await fetch(`${BASE_URL}/FastDB/${source.id}?key=${encodeURIComponent(currentKey.value)}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(source.content)
                })
                toast.success(t('tools.webNav.moved'))
                fetchData(true)
            } catch (e) {
                toast.error(t('tools.webNav.moveFailed'))
                fetchData(true)
            }
        }
        draggingId.value = null
        return
    }

    // 2. External Drop (New Link)
    if (!draggingId.value && !isReadOnly.value && isManageMode.value) {
        await handleExternalDrop(e, target)
    }

    draggingId.value = null
}

// ===== 处理拖拽 =====
const handleExternalDrop = async (e: DragEvent, target: any) => {
    const data = extractDropOrPasteData(e)
    if (!data) {
        console.warn('⚠️ No valid data extracted')
        return
    }

    console.log('🎯 Drop detected:', data)
    await saveBookmark(data.url, data.title, target)
}

// ===== 处理粘贴 =====
const handlePaste = async (e: ClipboardEvent, target: any) => {
    if (!isManageMode.value || isReadOnly.value || loading.value) return

    if (e.clipboardData?.files && e.clipboardData.files.length > 0) {
        return
    }

    const data = extractDropOrPasteData(e)
    if (!data) return

    try {
        new URL(data.url)
    } catch {
        return
    }

    e.preventDefault()
    e.stopPropagation()

    console.log('📋 Paste detected:', data)
    await saveBookmark(data.url, data.title, target)
}

// ===== 全局事件处理 =====
const handleGlobalDrop = (e: DragEvent) => {
    // 🔴 关键修改：必须先阻止默认行为
    e.preventDefault()
    e.stopPropagation()

    console.group('🎯 Global Drop Event')
    console.log('Types:', e.dataTransfer?.types)
    console.log('text/html:', e.dataTransfer?.getData('text/html')?.substring(0, 500))
    console.log('text/plain:', e.dataTransfer?.getData('text/plain'))
    console.log('text/uri-list:', e.dataTransfer?.getData('text/uri-list'))
    console.log('text/x-moz-url:', e.dataTransfer?.getData('text/x-moz-url'))
    console.groupEnd()

    // 现在才检查条件
    if (!isManageMode.value || isReadOnly.value || loading.value) {
        console.log('❌ Dropped but conditions not met')
        return
    }

    if (draggingId.value) {
        console.log('❌ Internal drag operation')
        return
    }

    handleExternalDrop(e, {
        content: {
            isApp: false,
            category: t('tools.webNav.defaultCategory')
        }
    })
}

const handleGlobalPaste = (e: ClipboardEvent) => {
    if (!isManageMode.value || isReadOnly.value || loading.value) return
    if (draggingId.value) return

    handlePaste(e, {
        content: {
            isApp: false,
            category: t('tools.webNav.defaultCategory')
        }
    })
}

const openLink = (url: string) => {
    if (isManageMode.value) return
    if (!url) return
    const targetUrl = url.startsWith('http') ? url : `https://${url}`
    window.open(targetUrl, '_blank')
}

// --- User Auth Logic ---
const onLoginSuccess = () => {
    fetchData()
}

const wrappedHandleLogin = () => {
    handleLogin(onLoginSuccess)
}

const wrappedHandleUpdate = () => {
    handleUpdateProfile(async () => {
        fetchData()
    })
}

// --- Content Actions ---
const handleSave = async () => {
    if (isReadOnly.value) return toast.error(t('tools.webNav.readOnlyAlert'))
    if (!modal.value.form.name || !modal.value.form.url) return toast.error(t('tools.webNav.required'))
    const url = modal.value.form.url.startsWith('http') ? modal.value.form.url : 'https://' + modal.value.form.url
    saving.value = true
    try {
        const isEdit = modal.value.isEdit
        const targetUrl = isEdit
            ? `${BASE_URL}/FastDB/${modal.value.id}?key=${encodeURIComponent(currentKey.value)}`
            : `${BASE_URL}/FastDB?key=${encodeURIComponent(currentKey.value)}`

        const res = await fetch(targetUrl, {
            method: isEdit ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ...modal.value.form, url })
        })

        if (res.ok) {
            toast.success(t('tools.webNav.saved'))
            modal.value.open = false
            setTimeout(fetchData, 300)
        }
    } catch (e) {
        toast.error(t('tools.webNav.saveFailed'))
    } finally {
        saving.value = false
    }
}

const handleDelete = async (id: string) => {
    if (isReadOnly.value) return toast.error(t('tools.webNav.readOnlyAlert'))
    if (!confirm(t('tools.webNav.deleteConfirm'))) return
    try {
        await fetch(`${BASE_URL}/FastDB/${id}?key=${encodeURIComponent(currentKey.value)}`, { method: 'DELETE' })
        fetchData()
    } catch (e) { toast.error(t('tools.webNav.deleteFailed')) }
}

const handleClearAll = async () => {
    if (isReadOnly.value) return
    if (!confirm(t('tools.webNav.clearConfirm'))) return
    loading.value = true
    try {
        const res = await fetch(`${BASE_URL}/FastDB/clear?key=${encodeURIComponent(currentKey.value)}`, { method: 'DELETE' })
        if (!res.ok) throw new Error('Clear failed')
        links.value = []
        toast.success(t('tools.webNav.clearAll'))
    } catch (e) {
        toast.error('Clear failed')
    } finally { loading.value = false }
}

const handleBulkImport = async () => {
    if (isReadOnly.value) return toast.error(t('tools.webNav.readOnlyAlert'))
    if (!bulkModal.value.text.trim()) return
    bulkModal.value.loading = true
    const lines = bulkModal.value.text.split('\n').filter(l => l.trim())
    const newLinks = lines.map(line => {
        const [name, url, note, icon, category, color] = line.split('\t').map(s => s?.trim() || '')
        if (!name || !url) return null
        const actualCategory = category || (note && note !== '[Flare 应用]' ? note : '') || t('tools.webNav.defaultCategory')
        return {
            name,
            url: url.startsWith('http') ? url : 'https://' + url,
            icon: icon || '',
            category: actualCategory,
            isApp: !!(note?.includes('[Flare 应用]')),
            isApplied: true,
            color: color || '',
            sortIndex: 0
        }
    }).filter(Boolean)

    if (newLinks.length === 0) {
        bulkModal.value.loading = false
        return toast.error('No valid data found')
    }

    try {
        const res = await fetch(`${BASE_URL}/FastDB/bulk?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newLinks)
        })

        if (!res.ok) throw new Error('Bulk API response not ok')

        toast.success(`Imported ${newLinks.length} links`)
        bulkModal.value.open = false
        fetchData()
    } catch (e) {
        toast.error('Import failed')
    } finally {
        bulkModal.value.loading = false
    }
}

const handleBulkExport = () => {
    const text = links.value.map(l => {
        const c = l.content
        return [
            c.name,
            c.url,
            c.isApp ? '[Flare 应用]' : '',
            c.icon || '',
            c.category || '',
            c.color || ''
        ].join('\t')
    }).join('\n')
    bulkModal.value.text = text
    bulkModal.value.open = true
}

const handleCategoryDisable = async (categoryPrefix: string) => {
    console.log('[handleCategoryDisable] categoryPrefix:', categoryPrefix, 'isReadOnly:', isReadOnly.value, 'totalLinks:', links.value.length)
    if (isReadOnly.value) return toast.error(t('tools.webNav.readOnlyAlert'))

    // Find all matching links
    const matchingLinks = links.value.filter(l => {
        const cat = l.content.category || ''
        return cat === categoryPrefix || cat.startsWith(categoryPrefix + ' /')
    })

    if (matchingLinks.length === 0) return

    // Toggle: if all are disabled, enable them; otherwise disable all
    const allDisabled = matchingLinks.every(l => l.content.isApplied === false)
    const newAppliedState = allDisabled // if all disabled → enable (true); else → disable (false)

    const bulkItems: { id: string; content: any }[] = []
    matchingLinks.forEach(l => {
        l.content.isApplied = newAppliedState
        bulkItems.push({ id: l.id, content: { ...l.content } })
    })

    links.value = [...links.value]
    updateCache()

    try {
        await fetch(`${BASE_URL}/FastDB/bulk?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(bulkItems)
        })
        const actionLabel = newAppliedState
            ? (t('tools.webNav.restored') || 'links restored')
            : (t('tools.webNav.suspended') || 'links suspended')
        toast.success(`${categoryPrefix}: ${bulkItems.length} ${actionLabel}`)
        fetchData(true)
    } catch (e) {
        toast.error(t('tools.webNav.moveFailed') || 'Toggle failed')
        fetchData(true)
    }
}

const handleCategoryDelete = async (categoryPrefix: string) => {
    console.log('[handleCategoryDelete] categoryPrefix:', categoryPrefix, 'isReadOnly:', isReadOnly.value, 'totalLinks:', links.value.length)
    if (isReadOnly.value) return toast.error(t('tools.webNav.readOnlyAlert'))

    const matchingLinks = links.value.filter(l => {
        const cat = l.content.category || ''
        return cat === categoryPrefix || cat.startsWith(categoryPrefix + ' /')
    })

    if (matchingLinks.length === 0) return
    if (!confirm(`${t('tools.webNav.deleteCategoryConfirm') || 'Delete all links under'} "${categoryPrefix}"? (${matchingLinks.length} ${t('tools.webNav.links') || 'links'})`)) return

    loading.value = true
    try {
        const ids = matchingLinks.map(l => l.id)
        await fetch(`${BASE_URL}/FastDB/bulk-delete?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ids)
        })
        toast.success(`${categoryPrefix}: ${matchingLinks.length} ${t('tools.webNav.deleted') || 'links deleted'}`)
        fetchData()
    } catch (e) {
        toast.error(t('tools.webNav.deleteFailed'))
    } finally {
        loading.value = false
    }
}

const handleRestoreLink = async (id: string) => {
    if (isReadOnly.value) return
    const link = links.value.find(l => l.id === id)
    if (!link) return

    link.content.isApplied = true
    links.value = [...links.value]
    updateCache()

    try {
        await fetch(`${BASE_URL}/FastDB/${id}?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(link.content)
        })
        toast.success(t('tools.webNav.moved'))
        fetchData(true)
    } catch (e) {
        toast.error(t('tools.webNav.moveFailed'))
        fetchData(true)
    }
}

const handleRestoreAll = async () => {
    if (isReadOnly.value) return
    const suspended = links.value.filter(l => l.content.isApplied === false)
    if (suspended.length === 0) return

    const bulkItems: { id: string; content: any }[] = []
    suspended.forEach(l => {
        l.content.isApplied = true
        bulkItems.push({ id: l.id, content: { ...l.content } })
    })

    links.value = [...links.value]
    updateCache()

    try {
        await fetch(`${BASE_URL}/FastDB/bulk?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(bulkItems)
        })
        toast.success(`${bulkItems.length} ${t('tools.webNav.restored') || 'links restored'}`)
        fetchData(true)
    } catch (e) {
        toast.error(t('tools.webNav.moveFailed'))
        fetchData(true)
    }
}

// --- Utils ---
const openModal = (link?: any) => {
    modal.value = {
        open: true,
        isEdit: !!link,
        id: link?.id || '',
        form: link ? { isApplied: true, ...link.content } : {
            name: '',
            url: '',
            category: t('tools.webNav.defaultCategory'),
            icon: '',
            color: '',
            isApp: false,
            isApplied: true,
            sortIndex: 0
        }
    }
}

const toggleApply = async (link: any) => {
    if (isReadOnly.value) return toast.error('Read Only Mode')
    const newVal = link.content.isApplied === false
    link.content.isApplied = newVal
    links.value = [...links.value]
    updateCache()

    try {
        await fetch(`${BASE_URL}/FastDB/${link.id}?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(link.content)
        })
        toast.success(newVal ? 'Applied' : 'Disabled')
        fetchData(true)
    } catch (e) {
        link.content.isApplied = !newVal
        links.value = [...links.value]
        updateCache()
        toast.error('Failed to update status')
    }
}

const toggleIsApp = async (link: any) => {
    if (isReadOnly.value) return toast.error('Read Only Mode')
    const newVal = !link.content.isApp
    link.content.isApp = newVal
    links.value = [...links.value]
    updateCache()

    try {
        await fetch(`${BASE_URL}/FastDB/${link.id}?key=${encodeURIComponent(currentKey.value)}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(link.content)
        })
        toast.success(newVal ? 'Set as App' : 'Set as Bookmark')
        fetchData(true)
    } catch (e) {
        link.content.isApp = !newVal
        links.value = [...links.value]
        updateCache()
        toast.error('Failed to update status')
    }
}

const handleSwitchKey = (key: string) => {
    if (key.trim()) {
        router.push(`/tools/web-nav/${encodeURIComponent(key.trim())}`)
    }
}

const updateUserProfile = async (newUserData: any) => {
    if (!currentUser.value) return
    const authKey = currentUser.value.authKey
    try {
        await fetch(`${BASE_URL}/FastDB/${currentUser.value.dbId}?key=${authKey}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newUserData)
        })
        currentUser.value = { ...currentUser.value, ...newUserData }
        localStorage.setItem('webnav_user', JSON.stringify(currentUser.value))
    } catch (e) {
        console.error('Update user failed', e)
        toast.error('Failed to save user profile')
    }
}

const generateGuid = () => {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return crypto.randomUUID()
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8)
        return v.toString(16)
    })
}

const handleCreateUserKey = async (name: string) => {
    if (!currentUser.value) return
    const newKey = generateSecureKey(currentUser.value.username + name)
    const readOnlyKey = generateGuid()
    const newEntry = { name, key: newKey, readOnlyKey }

    const keys = [...(currentUser.value.bookmarkKeys || []), newEntry]

    await updateUserProfile({
        ...currentUser.value,
        bookmarkKeys: keys
    })

    try {
        const anchorContent = {
            name: 'System Read-Only Anchor',
            url: 'about:readonly',
            type: 'READ_ONLY_ANCHOR',
            isApp: false,
            description: 'DO NOT DELETE. This record enables Read-Only access.'
        }
        await fetch(`${BASE_URL}/FastDB?key=u_${newKey}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(anchorContent)
        })
    } catch (e) {
        console.error('Failed to init anchor for new key', e)
    }

    handleSelectUserKey(newKey)
    toast.success(`Created "${name}"`)
}

const handleSelectUserKey = (key: string) => {
    activeUserKey.value = key
    localStorage.setItem('webnav_active_key_id', key)
    fetchData()
}

const handleDeleteUserKey = async (key: string) => {
    if (!currentUser.value || key === currentUser.value.dataKey) return
    if (!confirm('Delete this collection?')) return

    const keys = (currentUser.value.bookmarkKeys || []).filter((k: any) => k.key !== key)

    await updateUserProfile({
        ...currentUser.value,
        bookmarkKeys: keys
    })

    if (activeUserKey.value === key) {
        handleSelectUserKey(currentUser.value.dataKey)
    }
    toast.success('Collection deleted')
}

const handleCopyReadOnly = async () => {
    if (!currentUser.value) return

    const targetKey = activeUserKey.value || currentUser.value.dataKey
    const targetKeyObj = currentUser.value.bookmarkKeys?.find((k: any) => k.key === targetKey)

    if (!targetKeyObj) {
        toast.error('Current collection not found in profile')
        return
    }

    const doCopy = (guid: string) => {
        const url = `${window.location.origin}/tools/web-nav/${guid}`
        navigator.clipboard.writeText(url)
        toast.success('Read-Only Link Copied!')
    }

    let readKeyToUse = targetKeyObj.readOnlyKey

    try {
        const res = await fetch(`${BASE_URL}/FastDB?key=u_${targetKey}`)
        const data = await res.json()
        const items = data.map((i: any) => ({ ...i, content: safeParse(i.content) }))

        const existingAnchor = items.find((i: any) => i.content?.type === 'READ_ONLY_ANCHOR')

        if (existingAnchor) {
            if (existingAnchor.id !== readKeyToUse) {
                console.log('Fixing mismatch ReadOnly Key in Profile')
                readKeyToUse = existingAnchor.id

                const newKeys = currentUser.value.bookmarkKeys.map((k: any) => {
                    if (k.key === targetKey) return { ...k, readOnlyKey: existingAnchor.id }
                    return k
                })

                await updateUserProfile({
                    ...currentUser.value,
                    bookmarkKeys: newKeys
                })
            }
            doCopy(readKeyToUse)
            return
        } else {
            console.log('Anchor missing in DB, creating new one...')
            const anchorContent = {
                name: 'System Read-Only Anchor',
                url: 'about:readonly',
                type: 'READ_ONLY_ANCHOR',
                isApp: false,
                description: 'DO NOT DELETE. This record enables Read-Only access.'
            }

            const createRes = await fetch(`${BASE_URL}/FastDB?key=u_${targetKey}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(anchorContent)
            })
            const createData = await createRes.json()
            const newId = createData.id

            const newKeys = currentUser.value.bookmarkKeys.map((k: any) => {
                if (k.key === targetKey) return { ...k, readOnlyKey: newId }
                return k
            })

            await updateUserProfile({
                ...currentUser.value,
                bookmarkKeys: newKeys
            })

            doCopy(newId)
            return
        }

    } catch (e) {
        console.error('JIT ReadOnly Verification Failed', e)
        if (readKeyToUse) {
            doCopy(readKeyToUse)
        } else {
            toast.error('Failed to generate link. Network error?')
        }
    }
}

const ensureUserKeysIntegrity = async () => {
    if (!currentUser.value) return
    const user = currentUser.value
    let keys = [...(user.bookmarkKeys || [])]
    let changed = false

    if (!keys.find((k: any) => k.key === user.dataKey)) {
        keys.unshift({
            name: 'Main Collection',
            key: user.dataKey,
            readOnlyKey: generateGuid()
        })
        changed = true
    }

    keys = keys.map((k: any) => {
        if (!k.readOnlyKey) {
            changed = true
            return { ...k, readOnlyKey: generateGuid() }
        }
        return k
    })

    if (changed) {
        console.log('Self-healing user keys integrity...')
        await updateUserProfile({
            ...user,
            bookmarkKeys: keys
        })
    }
}

watch(currentUser, (newUser) => {
    if (newUser) {
        ensureUserKeysIntegrity()
        const storedActive = typeof window !== 'undefined' ? localStorage.getItem('webnav_active_key_id') : null
        const validKey = newUser.bookmarkKeys?.find((k: any) => k.key === storedActive)
        if (validKey) {
            activeUserKey.value = validKey.key
        } else {
            activeUserKey.value = newUser.dataKey
        }
    } else {
        activeUserKey.value = ''
    }
}, { immediate: true })

// --- Computed ---
const suspendedLinks = computed(() => {
    return links.value.filter(l => l.content.isApplied === false)
})

const filteredLinks = computed(() => {
    const q = searchQuery.value.toLowerCase()
    const filtered = links.value.filter(l => {
        const matchesQuery = !q ||
            l.content.name?.toLowerCase().includes(q) ||
            l.content.url?.toLowerCase().includes(q) ||
            (l.content.category || '').toLowerCase().includes(q) ||
            (l.content.color || '').toLowerCase().includes(q) ||
            (l.content.icon || '').toLowerCase().includes(q)

        if (!matchesQuery) return false
        if (isManageMode.value) {
            return manageTab.value === 'data' || l.content.isApplied !== false
        }
        return l.content.isApplied !== false
    })
    return [...filtered].sort((a, b) => {
        const isAppA = !!a.content.isApp
        const isAppB = !!b.content.isApp
        if (isAppA !== isAppB) return isAppA ? -1 : 1

        const catA = (a.content.category || '').toLowerCase()
        const catB = (b.content.category || '').toLowerCase()
        if (catA !== catB) return catA.localeCompare(catB)

        const idxA = a.content.sortIndex || 0
        const idxB = b.content.sortIndex || 0
        if (idxA !== idxB) return idxA - idxB

        return (a.content.name || '').toLowerCase().localeCompare((b.content.name || '').toLowerCase())
    })
})

const groupedBookmarks = computed(() => {
    const groups: Record<string, any[]> = {}
    const defaultCat = t('tools.webNav.defaultCategory')
    
    // Grouping
    bookmarkGroups.value.forEach(link => {
        const cat = link.content.category || defaultCat
        if (!groups[cat]) groups[cat] = []
        groups[cat]!.push(link) // Use non-null assertion as it's initialized above
    })

    // Pre-sort keys to avoid repeated sorting in template
    const sortedKeys = Object.keys(groups).sort()
    const result: Record<string, any[]> = {}
    for (const key of sortedKeys) {
        result[key] = groups[key]! // Use non-null assertion as key comes from Object.keys
    }
    return result
})

const displayedLinks = computed(() => {
    return filteredLinks.value
})

const APP_CATS = ['应用', 'Apps', 'Primary', '常用', 'Application']
const appGroups = computed(() => {
    return displayedLinks.value.filter(l => {
        if (l.content.isApp) return true
        const cat = l.content.category || ''
        return APP_CATS.includes(cat)
    })
})

const bookmarkGroups = computed(() => {
    return displayedLinks.value.filter(l => {
        if (l.content.isApp) return false
        const cat = l.content.category || ''
        return !APP_CATS.includes(cat)
    })
})

const ensureUniqueKeys = async () => {
    if (!currentUser.value || !currentUser.value.bookmarkKeys) return

    const seen = new Set()
    const unique: any[] = []
    let hasChanges = false

    for (const k of currentUser.value.bookmarkKeys) {
        if (!seen.has(k.key)) {
            seen.add(k.key)
            unique.push(k)
        } else {
            hasChanges = true
        }
    }

    if (hasChanges) {
        console.log('Fixed duplicate keys:', currentUser.value.bookmarkKeys, '->', unique)
        await updateUserProfile({
            ...currentUser.value,
            bookmarkKeys: unique
        })
    }
}

onMounted(() => {
    ensureUniqueKeys()

    if (links.value.length > 0) {
        setTimeout(() => fetchData(true), 200)
    } else {
        fetchData()
    }

    updateTime()
    timeInterval = setInterval(updateTime, 1000)

    // 监听滚动事件
    window.addEventListener('scroll', () => {
        showScrollTop.value = window.scrollY > 300
    })

    // 页面关闭/刷新时立即保存主题配置
    window.addEventListener('beforeunload', flushThemeConfig)
})

onUnmounted(() => {
    if (timeInterval) clearInterval(timeInterval)
    // 组件卸载时也确保保存
    flushThemeConfig()
    window.removeEventListener('beforeunload', flushThemeConfig)
})

watch(currentKey, () => {
    fetchData()
})

watch([() => t('tools.webNav.title'), currentUser], ([newTitle, user]) => {
    if (typeof document !== 'undefined') {
        let title: string
        if (user?.username) {
            title = user.username
        } else {
            title = newTitle || 'WebNav'
        }
        document.title = title
        // 持久化标题，防止刷新时闪烁
        try { localStorage.setItem('webnav_page_title', title) } catch (e) { }
    }
}, { immediate: true, deep: true })
</script>

<style scoped>
.writing-vertical {
    writing-mode: vertical-rl;
}

@keyframes blob-float {
    0% {
        transform: translate(0, 0) scale(1);
    }

    100% {
        transform: translate(10%, 10%) scale(1.1);
    }
}

:deep(:not(.dark)) {
    --background: 240 10% 97%;
}

:deep(.dark) {
    --background: 240 10% 5%;
    --card: 240 10% 10%;
    --border: 240 5% 18%;
    --muted: 240 5% 12%;
    --foreground: 240 5% 82%;
    --muted-foreground: 240 5% 60%;
}

:deep(.glass-card) {
    transition: all 0.15s cubic-bezier(0, 0, 0, 1);
}

:deep(.dark .glass-card) {
    background: linear-gradient(165deg, rgba(255, 255, 255, 0.08) 0%, rgba(255, 255, 255, 0.02) 100%);
    box-shadow:
        inset 0 1px 1px 0 rgba(255, 255, 255, 0.1),
        0 8px 32px -8px rgba(0, 0, 0, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.08);
}

:deep(.glass-card:hover) {
    transform: translateY(-4px) scale(1.01);
    box-shadow:
        0 20px 40px -15px rgba(0, 0, 0, 0.2);
}

:deep(.dark .glass-card:hover) {
    box-shadow:
        inset 0 1px 1px 0 rgba(255, 255, 255, 0.15),
        0 25px 50px -12px rgba(0, 0, 0, 0.8),
        0 0 15px -5px var(--primary);
}
</style>