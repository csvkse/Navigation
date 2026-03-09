<template>
    <div v-if="bookmarkGroups.length > 0 || isManageMode" class="space-y-6">
        <!-- 标题区 -->
        <div class="flex items-center gap-3 px-2">
            <h2 class="text-base font-black tracking-tighter uppercase" :class="`theme-${layoutScheme}-title`">
                {{ t('tools.webNav.bookmarkCollections') }}
            </h2>
            <div class="h-px flex-1" :class="`theme-${layoutScheme}-divider`"></div>
        </div>

        <div class="columns-2 sm:columns-2 lg:columns-3 xl:columns-4 2xl:columns-5 gap-x-4 sm:gap-x-10">
            <div v-for="(links, cat) in groupedBookmarks" :key="cat" @dragover.prevent
                @drop.stop="$emit('drop', $event, { content: { isApp: false, category: cat, sortIndex: -1 } })"
                class="break-inside-avoid mb-6 group/cat p-3 sm:p-5 space-y-4 min-h-[80px] bookmark-group-card"
                :class="`theme-${layoutScheme}-card`" :data-category-color="links[0]?.content.color"
                :data-count="links.length">

                <div class="flex items-center justify-between relative z-10 bookmark-group-header"
                    :class="`theme-${layoutScheme}-header`">
                    <h3 class="bookmark-group-title flex items-center gap-1.5"
                        :class="`theme-${layoutScheme}-group-title`" :data-color="links[0]?.content.color">
                        <span class="w-1.5 h-1.5 rounded-full" :class="`theme-${layoutScheme}-dot`"></span>
                        {{ cat }}
                    </h3>
                    <span class="hidden sm:inline-block text-[11px] font-mono px-1.5 py-0.5"
                        :class="`theme-${layoutScheme}-badge`">
                        {{ links.length }}
                    </span>
                </div>

                <div class="space-y-1.5 px-0.5 relative z-10">
                    <div v-for="link in links" :key="link.id" draggable="true"
                        @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')" @dragover.prevent
                        @drop.stop="$emit('drop', $event, link)"
                        class="group relative flex items-center gap-2.5 p-1.5 transition-all duration-150 cursor-pointer bookmark-item"
                        :class="[
                            draggingId === link.id ? 'dragging' : '',
                            `theme-${layoutScheme}-item`
                        ]" :data-color="link.content.color"
                        :style="{ '--cyber-color': link.content.color || undefined }">

                        <!-- 拖拽把手 - 只在管理模式下显示 -->
                        <GripVertical v-if="isManageMode" class="h-3 w-3 shrink-0"
                            :class="`theme-${layoutScheme}-grip`" />

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
                            <Button variant="ghost" size="icon" class="h-6 w-6"
                                :class="`theme-${layoutScheme}-btn-delete`" @click.stop="$emit('delete-link', link.id)">
                                <Trash2 class="h-3 w-3" />
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- 空状态 -->
        <div v-if="bookmarkGroups.length === 0 && isManageMode" @dragover.prevent
            @drop.stop="$emit('drop', $event, { content: { isApp: false, category: t('tools.webNav.defaultCategory'), sortIndex: -1 } })"
            class="h-24 sm:h-32 flex flex-col items-center justify-center gap-2 transition-all p-4 cursor-default"
            :class="`theme-${layoutScheme}-empty`">
            <div class="p-2 rounded-full" :class="`theme-${layoutScheme}-empty-icon`">
                <LucideIcons.Bookmark class="h-6 w-6" />
            </div>
            <p class="text-xs sm:text-sm font-medium" :class="`theme-${layoutScheme}-empty-text`">
                {{ t('tools.webNav.dragBookmarkHere') }}
            </p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { GripVertical, Edit2, Trash2, Bookmark } from 'lucide-vue-next'
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

interface Props {
    bookmarkGroups: Link[]
    groupedBookmarks: Record<string, Link[]>
    isManageMode: boolean
    draggingId: string | null
    layoutScheme: string  // premium / minimal / cyber / retro / midnight-slate
}

defineProps<Props>()

defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, target: any]
    'open-link': [url: string]
    'edit-link': [link: Link]
    'delete-link': [id: string]
}>()

const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
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