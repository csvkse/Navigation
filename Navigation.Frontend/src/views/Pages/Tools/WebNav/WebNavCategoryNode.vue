<template>
    <div class="category-node space-y-2">
        <!-- 文件夹标题栏（非根节点） -->
        <div v-if="depth > 0"
            class="flex items-center gap-1.5 py-1 px-1.5 rounded-md hover:bg-black/5 dark:hover:bg-white/5 cursor-pointer select-none transition-colors border max-w-fit max-w-[200px]"
            :class="[
                dragOverCategory === node.fullPath ? 'bg-primary/10 border-primary border-dashed' : 'border-transparent',
                `theme-${layoutScheme}-subfolder`,
                draggingId === 'folder:' + node.fullPath ? 'opacity-50 scale-95' : ''
            ]" @click="toggleCollapse" @dragover.prevent.stop="handleDragOver" :draggable="isManageMode"
            @dragstart.stop="$emit('drag-start', $event, { id: 'folder:' + node.fullPath, content: { isCategory: true, category: node.fullPath, name: node.name } } as any)"
            @dragend.stop="$emit('drag-end')" @dragleave.prevent.stop="handleDragLeave"
            @drop.stop="handleDropFolder($event, node.fullPath)">

            <component :is="isCollapsed ? ChevronRight : ChevronDown" class="w-3.5 h-3.5 opacity-50" />
            <Folder class="w-3.5 h-3.5 text-primary/70" :class="`theme-${layoutScheme}-folder-icon`" />
            <span class="text-xs font-semibold truncate" :title="node.name">{{ node.name }}</span>
            <span class="text-[10px] font-mono opacity-40 ml-1">{{ totalLinksCount }}</span>
        </div>

        <!-- 当前文件夹下的内容区 -->
        <div v-if="!isCollapsed" class="space-y-3"
            :class="{ 'pl-3 sm:pl-4 ml-1.5 border-l-2 border-border/40': depth > 0 }">

            <!-- 1. 当前层级的直接书签 -->
            <div v-if="visibleLinks.length > 0" class="space-y-1.5">
                <div v-for="link in visibleLinks" :key="link.id" draggable="true"
                    @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')" @dragover.prevent
                    @drop.stop="$emit('drop', $event, link)"
                    class="group relative flex items-center gap-2.5 p-1.5 transition-all duration-150 cursor-pointer bookmark-item"
                    :class="[
                        draggingId === link.id ? 'dragging' : '',
                        `theme-${layoutScheme}-item`
                    ]" :data-color="link.content.color" :style="{ '--cyber-color': link.content.color || undefined }">

                    <!-- 拖拽把手 - 只在管理模式下显示 -->
                    <GripVertical v-if="isManageMode" class="h-3 w-3 shrink-0" :class="`theme-${layoutScheme}-grip`" />

                    <!-- 图标容器 -->
                    <div class="h-6 w-6 flex items-center justify-center shrink-0 relative"
                        :class="`theme-${layoutScheme}-icon`" :data-color="link.content.color">
                        <component :is="getIcon(link.content.icon)" class="h-3 w-3"
                            :class="`theme-${layoutScheme}-icon-svg`" v-if="link.content.icon"
                            :data-color="link.content.color" />
                        <span v-else class="text-[8px] font-bold" :class="`theme-${layoutScheme}-fallback`">
                            {{ link.content.name[0]?.toUpperCase() }}
                        </span>
                    </div>

                    <!-- 链接信息 -->
                    <a :href="link.content.url" target="_blank" rel="noopener noreferrer"
                        class="flex-1 min-w-0 flex flex-col"
                        @click.prevent="!isManageMode && $emit('open-link', link.content.url)">
                        <div class="bookmark-item-name" :class="[
                            { 'truncate': !isManageMode, 'break-words': isManageMode },
                            `theme-${layoutScheme}-name`
                        ]" :data-color="link.content.color" :title="link.content.name">
                            {{ link.content.name }}
                        </div>
                    </a>

                    <!-- 管理按钮组 -->
                    <div v-if="isManageMode"
                        class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-20"
                        :class="`theme-${layoutScheme}-actions`">
                        <Button variant="ghost" size="icon" class="h-6 w-6" :class="`theme-${layoutScheme}-btn`"
                            @click.stop="$emit('edit-link', link)">
                            <Edit2 class="h-3 w-3" />
                        </Button>
                        <Button variant="ghost" size="icon" class="h-6 w-6" :class="`theme-${layoutScheme}-btn-delete`"
                            @click.stop="$emit('delete-link', link.id)">
                            <Trash2 class="h-3 w-3" />
                        </Button>
                    </div>
                </div>

                <!-- 展开/收起按钮 (针对当前目录项数超出限制的情况) -->
                <div v-if="node.links.length > 20" class="pt-0.5 pb-1 flex justify-center">
                    <button @click.stop="isLimitExpanded = !isLimitExpanded"
                        class="flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-md opacity-50 hover:opacity-100 transition-all bg-black/5 dark:bg-white/5"
                        :class="`theme-${layoutScheme}-expand-btn`">
                        <component :is="isLimitExpanded ? ChevronUp : ChevronDown" class="w-3 h-3" />
                        {{ isLimitExpanded ? '收起' : `展开其余 ${node.links.length - 20} 项` }}
                    </button>
                </div>
            </div>

            <!-- 2. 递归渲染子文件夹 -->
            <div v-if="hasChildren" class="space-y-3 pt-1">
                <WebNavCategoryNode v-for="subNode in sortedChildren" :key="subNode.fullPath" :node="subNode"
                    :depth="depth + 1" :is-manage-mode="isManageMode" :dragging-id="draggingId"
                    :layout-scheme="layoutScheme" :category-order="categoryOrder"
                    @drag-start="(e, l) => $emit('drag-start', e, l)" @drag-end="$emit('drag-end')"
                    @drop="(e, t) => $emit('drop', e, t)" @drop-folder="(e, path) => $emit('drop-folder', e, path)"
                    @open-link="(url) => $emit('open-link', url)" @edit-link="(l) => $emit('edit-link', l)"
                    @delete-link="(id) => $emit('delete-link', id)"
                    @update-category-order="(o) => $emit('update-category-order', o)" />
            </div>

            <!-- 如果当前文件夹为空且是管理模式，保留一个可以拖拽放进该文件夹的热区 -->
            <div v-if="node.links.length === 0 && !hasChildren && isManageMode && depth > 0"
                class="py-2 px-3 text-[10px] border border-dashed border-border/40 rounded-lg text-muted-foreground/50 flex justify-center"
                :class="[dragOverCategory === node.fullPath ? 'bg-primary/5 border-primary text-primary' : '']"
                @dragover.prevent.stop="handleDragOver" @dragleave.prevent.stop="handleDragLeave"
                @drop.stop="handleDropFolder($event, node.fullPath)">
                {{ t('tools.webNav.dragBookmarkHere') || '拖入此文件夹' }}
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { GripVertical, Edit2, Trash2, Bookmark, ChevronDown, ChevronUp, ChevronRight, Folder } from 'lucide-vue-next'
import * as LucideIcons from 'lucide-vue-next'

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
    node: CategoryNode
    depth: number
    isManageMode: boolean
    draggingId: string | null
    layoutScheme: string
    categoryOrder?: Record<string, number>
}

const props = defineProps<Props>()

const emit = defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, target: any]
    'drop-folder': [event: DragEvent, fullPath: string]
    'open-link': [url: string]
    'edit-link': [link: Link]
    'delete-link': [id: string]
    'update-category-order': [order: Record<string, number>]
}>()

// 状态
const isCollapsed = ref(props.depth > 0) // 子级默认折叠
const isLimitExpanded = ref(false)
const dragOverCategory = ref<string | null>(null)

const toggleCollapse = () => {
    isCollapsed.value = !isCollapsed.value
}

const handleDragOver = (_e: DragEvent) => {
    dragOverCategory.value = props.node.fullPath
}

const handleDragLeave = (_e: DragEvent) => {
    dragOverCategory.value = null
}

const handleDropFolder = (e: DragEvent, path: string) => {
    dragOverCategory.value = null
    emit('drop-folder', e, path)
}

// 计算
const visibleLinks = computed(() => {
    if (props.node.links.length <= 20 || isLimitExpanded.value) {
        return props.node.links
    }
    return props.node.links.slice(0, 20)
})

const hasChildren = computed(() => Object.keys(props.node.children).length > 0)

const sortedChildren = computed(() => {
    const keys = Object.keys(props.node.children)
    keys.sort((a, b) => {
        const pathA = props.node.children[a]!.fullPath
        const pathB = props.node.children[b]!.fullPath
        const orderA = props.categoryOrder?.[pathA] ?? 999999
        const orderB = props.categoryOrder?.[pathB] ?? 999999
        if (orderA !== orderB) return orderA - orderB
        return a.localeCompare(b)
    })
    return keys.map(k => props.node.children[k]!)
})

// 递归计算整棵树的所有链接数量
const getTotalLinks = (n: CategoryNode): number => {
    let count = n.links.length
    for (const key in n.children) {
        count += getTotalLinks(n.children[key]!)
    }
    return count
}

const totalLinksCount = computed(() => getTotalLinks(props.node))

// 工具
const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
}
</script>

<script lang="ts">
export default {
    name: 'WebNavCategoryNode'
}
</script>

<style scoped>
.dragging {
    opacity: 0.5;
    scale: 0.98;
}
</style>
