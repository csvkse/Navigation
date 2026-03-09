<template>
    <section v-if="appGroups.length > 0 || isManageMode" class="space-y-3">
        <!-- 标题区 -->
        <div class="flex items-center gap-3 px-2">
            <h2 class="text-sm font-black tracking-tighter uppercase" :class="`theme-${layoutScheme}-section-title`">
                {{ t('tools.webNav.appCenter') }}
            </h2>
            <div class="h-px flex-1" :class="`theme-${layoutScheme}-section-divider`"></div>
        </div>

        <div @dragover.prevent
            @drop.stop="$emit('drop', $event, { content: { isApp: true, category: t('tools.webNav.appCenter'), sortIndex: -1 } })"
            class="grid grid-cols-4 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-8 gap-1.5 sm:gap-4 min-h-[50px]">

            <div v-for="link in appGroups" :key="link.id" draggable="true"
                @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')" @dragover.prevent
                @drop.stop="$emit('drop', $event, link)" @click="$emit('open-link', link.content.url)"
                class="app-card flex flex-col items-center justify-center group cursor-pointer relative overflow-hidden text-center transition-all duration-200"
                :class="[
                    `theme-${layoutScheme}-card`,
                    draggingId === link.id ? 'dragging' : ''
                ]" :data-color="link.content.color" :style="{ '--cyber-color': link.content.color || undefined }">

                <!-- 图标容器 -->
                <div class="app-card-icon flex items-center justify-center shrink-0 relative group/icon"
                    :class="`theme-${layoutScheme}-icon`" :data-color="link.content.color">

                    <component :is="getIcon(link.content.icon)" class="theme-icon-svg"
                        :class="`theme-${layoutScheme}-icon-svg`" v-if="link.content.icon"
                        :data-color="link.content.color" />
                    <span v-else class="theme-fallback-text" :class="`theme-${layoutScheme}-fallback`">
                        {{ link.content.name[0]?.toUpperCase() }}
                    </span>
                </div>

                <!-- 文字信息 -->
                <div class="w-full flex flex-col items-center min-w-0 relative z-10">
                    <div class="w-full truncate px-0.5 app-grid-name" :class="`theme-${layoutScheme}-name`"
                        :data-color="link.content.color">
                        {{ link.content.name }}
                    </div>
                    <div v-if="layoutScheme !== 'cyber'" class="hidden sm:block truncate app-grid-category"
                        :class="`theme-${layoutScheme}-category`">
                        {{ link.content.category || t('tools.webNav.app') }}
                    </div>
                    <div v-if="layoutScheme === 'cyber'" class="hidden sm:block" :class="`theme-${layoutScheme}-badge`">
                        {{ link.content.category ? link.content.category.slice(0, 4) : 'APP' }}
                    </div>
                </div>

                <!-- 管理按钮 -->
                <div v-if="isManageMode"
                    class="absolute top-0.5 right-0.5 flex flex-col gap-0.5 items-center opacity-0 group-hover:opacity-100 transition-opacity z-20"
                    :class="`theme-${layoutScheme}-actions`">
                    <Button variant="ghost" size="icon" class="h-5 w-5" :class="`theme-${layoutScheme}-btn`"
                        @click.stop="$emit('edit-link', link)">
                        <Edit2 :class="`theme-${layoutScheme}-btn-icon`" />
                    </Button>
                </div>
            </div>
        </div>

        <!-- 空状态 -->
        <div v-if="appGroups.length === 0 && isManageMode" @dragover.prevent
            @drop.stop="$emit('drop', $event, { content: { isApp: true, category: t('tools.webNav.appCenter'), sortIndex: -1 } })"
            class="h-24 sm:h-32 flex flex-col items-center justify-center gap-2 transition-all p-4 cursor-default"
            :class="`theme-${layoutScheme}-empty`">
            <div class="p-2 rounded-full" :class="`theme-${layoutScheme}-empty-icon`">
                <LucideIcons.Box :class="`theme-${layoutScheme}-empty-svg`" />
            </div>
            <p class="text-xs sm:text-sm font-medium" :class="`theme-${layoutScheme}-empty-text`">
                {{ t('tools.webNav.dragAppHere') }}
            </p>
        </div>
    </section>
</template>

<script setup lang="ts">
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Edit2, Bookmark } from 'lucide-vue-next'
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
    appGroups: Link[]
    layoutScheme: string  // premium / minimal / cyber / retro / midnight-slate
    isManageMode: boolean
    draggingId: string | null
    icons: Record<string, any>
}

defineProps<Props>()

defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, target: any]
    'open-link': [url: string]
    'edit-link': [link: Link]
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
 * 这里只保留组件核心结构和拖拽状态
 */
.app-card {
    transition: all 0.2s ease;
}

.dragging {
    opacity: 0.3;
    scale: 0.98;
}
</style>