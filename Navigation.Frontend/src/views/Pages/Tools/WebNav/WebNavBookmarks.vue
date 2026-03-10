<template>
    <div v-if="bookmarkGroups.length > 0 || isManageMode" class="space-y-6">
        <!-- 标题区 -->
        <div class="flex items-center gap-3 px-2">
            <h2 class="text-base font-black tracking-tighter uppercase" :class="`theme-${layoutScheme}-title`">
                {{ t('tools.webNav.bookmarkCollections') }}
            </h2>
            <div class="h-px flex-1" :class="`theme-${layoutScheme}-divider`"></div>
            <button v-if="Object.keys(treeNodes).length > 1" @click="toggleAllCollapse"
                class="p-1.5 rounded-lg hover:bg-muted/50 text-muted-foreground/50 hover:text-muted-foreground transition-all active:scale-90"
                :title="allCollapsed ? (t('tools.webNav.expandAll') || 'Expand All') : (t('tools.webNav.collapseAll') || 'Collapse All')">
                <ChevronsUpDown v-if="allCollapsed" class="w-4 h-4" />
                <ChevronsDownUp v-else class="w-4 h-4" />
            </button>
        </div>

        <div class="columns-2 sm:columns-2 lg:columns-3 xl:columns-4 2xl:columns-5 gap-x-4 sm:gap-x-10"
            @dragover.prevent
            @drop="$emit('drop', $event, { content: { isApp: false, category: '', isRootDrop: true } })">
            <div v-for="(node, rootKey, index) in treeNodes" :key="rootKey" @dragover.prevent.stop
                @drop.stop="$emit('drop', $event, { content: { isApp: false, category: rootKey, sortIndex: -1 } })"
                class="break-inside-avoid mb-6 group/cat p-3 sm:p-5 space-y-4 min-h-[80px] bookmark-group-card"
                :class="`theme-${layoutScheme}-card`" :data-category-color="getPrimaryColor(node)"
                :data-count="countTotalLinks(node)">

                <div class="flex items-center justify-between relative z-10 bookmark-group-header cursor-pointer select-none"
                    :draggable="isManageMode"
                    @dragstart.stop="$emit('drag-start', $event, { id: 'folder:' + rootKey, content: { isCategory: true, category: rootKey, name: rootKey } } as any)"
                    @dragend.stop="$emit('drag-end')" @click="toggleCategoryCollapse(rootKey as string, index)" :class="[
                        `theme-${layoutScheme}-header`,
                        draggingId === 'folder:' + rootKey ? 'opacity-50 scale-95' : ''
                    ]">
                    <h3 class="bookmark-group-title flex items-center gap-1.5"
                        :class="`theme-${layoutScheme}-group-title`" :data-color="getPrimaryColor(node)">
                        <span class="w-1.5 h-1.5 rounded-full" :class="`theme-${layoutScheme}-dot`"></span>
                        {{ rootKey }}
                    </h3>
                    <div class="flex items-center gap-1.5">
                        <!-- Category Actions (manage mode only) -->
                        <div v-if="isManageMode"
                            class="flex items-center gap-0.5 opacity-0 group-hover/cat:opacity-100 transition-opacity">
                            <button draggable="false" @mousedown.stop
                                @click.stop="$emit('disable-category', rootKey as string)"
                                class="p-1 rounded-md hover:bg-muted/50 text-muted-foreground/50 hover:text-amber-500 transition-all active:scale-90"
                                :title="t('tools.webNav.disableCategory') || 'Suspend Category'">
                                <EyeOff class="w-3.5 h-3.5" />
                            </button>
                            <button draggable="false" @mousedown.stop
                                @click.stop="$emit('delete-category', rootKey as string)"
                                class="p-1 rounded-md hover:bg-destructive/10 text-muted-foreground/50 hover:text-destructive transition-all active:scale-90"
                                :title="t('tools.webNav.deleteCategory') || 'Delete Category'">
                                <Trash2 class="w-3.5 h-3.5" />
                            </button>
                        </div>
                        <span class="hidden sm:inline-block text-[11px] font-mono px-1.5 py-0.5"
                            :class="`theme-${layoutScheme}-badge`">
                            {{ countTotalLinks(node) }}
                        </span>
                        <component :is="isCategoryCollapsed(rootKey as string, index) ? ChevronDown : ChevronUp"
                            class="w-4 h-4 opacity-40 hover:opacity-100 transition-opacity" />
                    </div>
                </div>

                <div v-if="!isCategoryCollapsed(rootKey as string, index)" class="space-y-4 relative z-10">
                    <!-- Recursive Category Node handles everything inside this root (both its own links and subfolders) -->
                    <WebNavCategoryNode :node="node" :depth="0" :is-manage-mode="isManageMode" :dragging-id="draggingId"
                        :layout-scheme="layoutScheme" :category-order="categoryOrder" @drag-start="handleDragStart"
                        @drag-end="handleDragEnd" @drop="handleDrop" @drop-folder="handleDropIntoFolder"
                        @open-link="handleOpenLink" @edit-link="handleEditLink" @delete-link="handleDeleteLink"
                        @update-category-order="(o: Record<string, number>) => $emit('update-category-order', o)" />
                </div>
            </div>
        </div>

        <!-- 回到顶部按钮 -->
        <div v-if="Object.keys(treeNodes).length > 5" class="flex justify-center pt-2 pb-4">
            <button @click="scrollToTop"
                class="inline-flex items-center gap-1.5 px-4 py-2 rounded-full text-xs font-medium text-muted-foreground/50 hover:text-muted-foreground hover:bg-muted/30 transition-all active:scale-95">
                <ArrowUp class="w-3.5 h-3.5" />
                {{ t('tools.webNav.backToTop') || 'Back to Top' }}
            </button>
        </div>

        <!-- 空状态 -->
        <div v-if="bookmarkGroups.length === 0 && isManageMode" @dragover.prevent
            @drop.stop="$emit('drop', $event, { content: { isApp: false, category: t('tools.webNav.defaultCategory'), sortIndex: -1 } })"
            class="h-24 sm:h-32 flex flex-col items-center justify-center gap-2 transition-all p-4 cursor-default"
            :class="`theme-${layoutScheme}-empty`">
            <div class="p-2 rounded-full" :class="`theme-${layoutScheme}-empty-icon`">
                <Bookmark class="h-6 w-6" />
            </div>
            <p class="text-xs sm:text-sm font-medium" :class="`theme-${layoutScheme}-empty-text`">
                {{ t('tools.webNav.dragBookmarkHere') }}
            </p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Bookmark, ChevronDown, ChevronUp, ChevronsDownUp, ChevronsUpDown, ArrowUp, EyeOff, Trash2 } from 'lucide-vue-next'
import WebNavCategoryNode from './WebNavCategoryNode.vue'

const { t } = useI18n()

interface Link {
    id: string
    content: {
        name: string
        url: string
        icon?: string
        color?: string
        category?: string
        isApp?: boolean
    }
}

interface CategoryNode {
    name: string
    fullPath: string
    links: Link[]
    children: Record<string, CategoryNode>
}

interface Props {
    bookmarkGroups: Link[]
    groupedBookmarks: Record<string, Link[]>
    isManageMode: boolean
    draggingId: string | null
    layoutScheme: string  // premium / minimal / cyber / retro / midnight-slate
    categoryOrder?: Record<string, number>
}

const props = defineProps<Props>()

const emit = defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, target: any]
    'open-link': [url: string]
    'edit-link': [link: Link]
    'delete-link': [id: string]
    'update-category-order': [order: Record<string, number>]
    'disable-category': [category: string]
    'delete-category': [category: string]
}>()

const handleDragStart = (e: DragEvent, l: Link) => emit('drag-start', e, l)
const handleDragEnd = () => emit('drag-end')
const handleDrop = (e: DragEvent, t: any) => emit('drop', e, t)
const handleOpenLink = (url: string) => emit('open-link', url)
const handleEditLink = (l: Link) => emit('edit-link', l)
const handleDeleteLink = (id: string) => emit('delete-link', id)

// --- Tree Building Logic ---

const treeNodes = computed(() => {
    // Return root nodes mapped by their top-level category name
    const roots: Record<string, CategoryNode> = {}

    // Iterate through all links in bookmarkGroups (ignoring groupedBookmarks since we rebuild it)
    props.bookmarkGroups.forEach(link => {
        let catStr = link.content.category || t('tools.webNav.defaultCategory')
        catStr = catStr.trim()

        // Split by " / " or just "/" into segments
        const segments = catStr.split(/\s*\/\s*/).filter(Boolean)

        if (segments.length === 0) {
            segments.push(t('tools.webNav.defaultCategory'))
        }

        const rootName = segments[0]!
        if (!roots[rootName]) {
            roots[rootName] = {
                name: rootName,
                fullPath: rootName,
                links: [],
                children: {}
            }
        }

        let currentNode = roots[rootName]!
        let currentPath = rootName

        // Traverse down the tree, creating nodes as needed
        for (let i = 1; i < segments.length; i++) {
            const seg = segments[i]!
            currentPath = `${currentPath} / ${seg}`

            if (!currentNode.children[seg]) {
                currentNode.children[seg] = {
                    name: seg,
                    fullPath: currentPath,
                    links: [],
                    children: {}
                }
            }
            currentNode = currentNode.children[seg]!
        }

        // Add the link to the deepest node
        currentNode.links.push(link)
    })

    // Return the roots sorted alphabetically by key or using customized order
    const sortedRoots: Record<string, CategoryNode> = {}
    Object.keys(roots).sort((a, b) => {
        const orderA = props.categoryOrder?.[a] ?? 999999
        const orderB = props.categoryOrder?.[b] ?? 999999
        if (orderA !== orderB) return orderA - orderB
        return a.localeCompare(b)
    }).forEach(key => {
        sortedRoots[key] = roots[key]!
    })

    return sortedRoots
})

// 计算某个根节点及其所有子节点下的书签总数
const countTotalLinks = (node: CategoryNode): number => {
    let count = node.links.length
    for (const key in node.children) {
        count += countTotalLinks(node.children[key]!)
    }
    return count
}

// 提取一个能代表整个根卡片颜色的节点
const getPrimaryColor = (node: CategoryNode): string | undefined => {
    if (node.links.length > 0 && node.links[0]?.content.color) {
        return node.links[0]!.content.color
    }
    for (const key in node.children) {
        const childColor = getPrimaryColor(node.children[key]!)
        if (childColor) return childColor
    }
    return undefined
}

// --- Collapse Logic ---
const manuallyToggledCollapse = ref<Record<string, boolean>>({})

const isCategoryCollapsed = (cat: string, index: number) => {
    if (manuallyToggledCollapse.value[cat] !== undefined) {
        return manuallyToggledCollapse.value[cat]
    }
    const totalCategories = Object.keys(treeNodes.value).length
    return totalCategories > 10 && index >= 10
}

const toggleCategoryCollapse = (cat: string, index: number) => {
    const current = isCategoryCollapsed(cat, index)
    manuallyToggledCollapse.value[cat] = !current
}

const allCollapsed = computed(() => {
    const keys = Object.keys(treeNodes.value)
    if (keys.length === 0) return false
    return keys.every((key, index) => isCategoryCollapsed(key, index))
})

const toggleAllCollapse = () => {
    const keys = Object.keys(treeNodes.value)
    const shouldCollapse = !allCollapsed.value
    keys.forEach(key => {
        manuallyToggledCollapse.value[key] = shouldCollapse
    })
}

// Handler for dropping specifically onto a folder node (from recursive component)
const handleDropIntoFolder = (e: DragEvent, fullPath: string) => {
    emit('drop', e, { content: { isApp: false, category: fullPath, sortIndex: -1 } })
}

const scrollToTop = () => {
    const scrollContainer = document.querySelector('main.overflow-y-auto') || document.querySelector('#main-content')?.parentElement
    if (scrollContainer) {
        scrollContainer.scrollTo({ top: 0, behavior: 'smooth' })
    } else {
        window.scrollTo({ top: 0, behavior: 'smooth' })
    }
}

</script>

<style scoped>
/* 
 * 所有主题样式完全由全局CSS控制
 * 组件只负责结构，不负责具体样式
 */
.bookmark-group-card {
    transition: all 0.2s ease;
}

.dragging {
    opacity: 0.5;
    scale: 0.98;
}
</style>