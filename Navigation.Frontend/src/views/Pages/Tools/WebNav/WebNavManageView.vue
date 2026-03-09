<template>
    <div :class="['duration-500', filteredLinks.length === 0 ? 'animate-in fade-in slide-in-from-right-4' : '']">
        <div v-if="loading"
            class="absolute top-4 right-6 flex items-center gap-2 text-[11px] font-bold text-primary animate-pulse uppercase tracking-widest z-50">
            <Loader2 class="h-3 w-3 animate-spin" /> {{ t('tools.webNav.synchronizing') }}
        </div>
        <div class="bg-card/40 border border-border/30 rounded-3xl overflow-hidden backdrop-blur-md">
            <div class="p-6 border-b border-border/30 bg-muted/20 flex items-center justify-between">
                <div>
                    <h2 class="text-xl font-bold tracking-tight">{{ t('tools.webNav.manageTitle') }}</h2>
                    <p class="text-sm text-muted-foreground">{{ t('tools.webNav.manageDesc') }}</p>
                </div>
                <div class="flex gap-2">
                    <Button size="sm" variant="outline" class="h-8 rounded-lg" @click="$emit('clear-all')">
                        <Trash2 class="mr-2 h-3.5 w-3.5" /> {{ t('tools.webNav.clearAll') }}
                    </Button>
                </div>
            </div>
            <div class="overflow-x-auto">
                <table class="w-full text-left border-collapse">
                    <thead>
                        <tr class="text-[11px] uppercase tracking-wider text-muted-foreground/70 bg-muted/10">
                            <th class="p-4 font-bold border-b border-border/20 w-10"></th>
                            <th class="p-4 font-bold border-b border-border/20 uppercase tracking-widest">{{
                                t('tools.webNav.name') }} / URL</th>
                            <th class="p-4 font-bold border-b border-border/20">{{ t('tools.webNav.category') }}
                            </th>
                            <th class="p-4 font-bold border-b border-border/20">{{ t('tools.webNav.style') }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-center">{{
                                t('tools.webNav.isApp')
                            }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-center">{{
                                t('tools.webNav.isApplied')
                            }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-right">{{ t('tools.webNav.actions')
                                }}</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-border/20">
                        <tr v-for="link in filteredLinks" :key="link.id" draggable="true"
                            @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')"
                            @dragover.prevent @drop.stop="$emit('drop', $event, link)"
                            class="hover:bg-muted/10 transition-colors group/row"
                            :class="draggingId === link.id ? 'opacity-30 bg-primary/5' : ''">
                            <td class="p-4">
                                <GripVertical
                                    class="h-4 w-4 text-muted-foreground/20 group-hover/row:text-primary transition-colors cursor-grab" />
                            </td>
                            <td class="p-4">
                                <div class="flex items-center gap-3">
                                    <div class="h-8 w-8 rounded-lg bg-muted/40 flex items-center justify-center shrink-0"
                                        :style="{ color: link.content.color || 'inherit' }">
                                        <component :is="getIcon(link.content.icon)" class="h-4 w-4"
                                            v-if="link.content.icon" />
                                        <span v-else class="text-sm opacity-40 font-bold">{{
                                            link.content.name[0]?.toUpperCase() }}</span>
                                    </div>
                                    <div class="min-w-0">
                                        <div class="font-bold text-base truncate">{{ link.content.name }}</div>
                                        <div class="text-[11px] text-muted-foreground truncate">{{ link.content.url
                                        }}</div>
                                    </div>
                                </div>
                            </td>
                            <td class="p-4">
                                <Badge variant="outline" class="rounded-lg text-[11px] bg-muted/20 border-border/50">
                                    {{ link.content.category || t('tools.webNav.default') }}
                                </Badge>
                            </td>
                            <td class="p-4">
                                <div class="flex gap-1.5 items-center">
                                    <div v-if="link.content.color" class="w-3 h-3 rounded-full shadow-sm"
                                        :style="{ backgroundColor: link.content.color }" />
                                    <span class="text-[11px] font-mono opacity-50">{{ link.content.color ||
                                        t('tools.webNav.default')
                                        }}</span>
                                </div>
                            </td>
                            <td class="p-4 text-center">
                                <div @click="$emit('toggle-is-app', link)" class="flex justify-center group/toggle">
                                    <div :class="[
                                        'w-10 h-5 rounded-full relative transition-all duration-300 cursor-pointer border border-border/50',
                                        link.content.isApp ? 'bg-primary' : 'bg-muted-foreground/20'
                                    ]">
                                        <div :class="[
                                            'absolute top-0.5 w-3.5 h-3.5 rounded-full bg-white transition-all duration-300 shadow-sm',
                                            link.content.isApp ? 'left-[22px]' : 'left-0.5'
                                        ]"></div>
                                    </div>
                                </div>
                            </td>
                            <td class="p-4 text-center">
                                <div @click="$emit('toggle-apply', link)" class="flex justify-center group/toggle">
                                    <div :class="[
                                        'w-10 h-5 rounded-full relative transition-all duration-300 cursor-pointer border border-border/50',
                                        link.content.isApplied !== false ? 'bg-primary' : 'bg-muted-foreground/20'
                                    ]">
                                        <div :class="[
                                            'absolute top-0.5 w-3.5 h-3.5 rounded-full bg-white transition-all duration-300 shadow-sm',
                                            link.content.isApplied !== false ? 'left-[22px]' : 'left-0.5'
                                        ]"></div>
                                    </div>
                                </div>
                            </td>
                            <td class="p-4 text-right">
                                <div class="flex justify-end gap-1">
                                    <Button variant="ghost" size="icon"
                                        class="h-7 w-7 rounded-lg hover:bg-primary/10 hover:text-primary"
                                        @click="$emit('edit-link', link)">
                                        <Edit2 class="h-3.5 w-3.5" />
                                    </Button>
                                    <Button variant="ghost" size="icon"
                                        class="h-7 w-7 rounded-lg hover:bg-destructive/10 hover:text-destructive"
                                        @click="$emit('delete-link', link.id)">
                                        <Trash2 class="h-3.5 w-3.5" />
                                    </Button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { GripVertical, Edit2, Trash2, Loader2, Bookmark } from 'lucide-vue-next'
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
        isApplied?: boolean
    }
}

interface Props {
    filteredLinks: Link[]
    draggingId: string | null
    loading?: boolean
}

defineProps<Props>()

defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, link: Link]
    'edit-link': [link: Link]
    'delete-link': [id: string]
    'toggle-apply': [link: Link]
    'toggle-is-app': [link: Link]
    'clear-all': []
}>()

const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
}
</script>
