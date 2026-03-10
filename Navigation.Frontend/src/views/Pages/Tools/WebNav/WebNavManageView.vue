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
            <div class="overflow-x-auto min-h-[300px]">
                <table class="w-full text-left border-collapse min-w-[800px]">
                    <thead>
                        <tr class="text-[11px] uppercase tracking-wider text-muted-foreground/70 bg-muted/10">
                            <th class="p-4 font-bold border-b border-border/20 w-10"></th>
                            <th class="p-4 font-bold border-b border-border/20 uppercase tracking-widest">{{
                                t('tools.webNav.name') }} / URL</th>
                            <th class="p-4 font-bold border-b border-border/20">{{ t('tools.webNav.category') }}</th>
                            <th class="p-4 font-bold border-b border-border/20">{{ t('tools.webNav.style') }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-center">{{ t('tools.webNav.isApp')
                            }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-center">{{
                                t('tools.webNav.isApplied') }}</th>
                            <th class="p-4 font-bold border-b border-border/20 text-right">{{ t('tools.webNav.actions')
                            }}</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-border/20">
                        <template v-for="(node, rootKey) in treeNodes" :key="rootKey">
                            <WebNavManageTreeNode :node="node" :depth="0" :dragging-id="draggingId"
                                :category-order="categoryOrder"
                                @drag-start="(e: DragEvent, l: Link) => $emit('drag-start', e, l)"
                                @drag-end="$emit('drag-end')" @drop="(e: DragEvent, l: Link) => $emit('drop', e, l)"
                                @edit-link="(l: Link) => $emit('edit-link', l)"
                                @delete-link="(id: string) => $emit('delete-link', id)"
                                @toggle-apply="(l: Link) => $emit('toggle-apply', l)"
                                @toggle-is-app="(l: Link) => $emit('toggle-is-app', l)"
                                @update-category-order="(o: Record<string, number>) => $emit('update-category-order', o)"
                                @disable-category="(cat: string) => $emit('disable-category', cat)"
                                @delete-category="(cat: string) => $emit('delete-category', cat)" />
                        </template>
                        <tr v-if="filteredLinks.length === 0">
                            <td colspan="7" class="p-8 text-center text-muted-foreground text-sm">
                                No links available
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Trash2, Loader2 } from 'lucide-vue-next'
import WebNavManageTreeNode from './WebNavManageTreeNode.vue'

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

interface CategoryNode {
    name: string
    fullPath: string
    links: Link[]
    children: Record<string, CategoryNode>
}

interface Props {
    filteredLinks: Link[]
    draggingId: string | null
    loading?: boolean
    categoryOrder?: Record<string, number>
}

const props = defineProps<Props>()

defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, link: Link]
    'edit-link': [link: Link]
    'delete-link': [id: string]
    'toggle-apply': [link: Link]
    'toggle-is-app': [link: Link]
    'clear-all': []
    'update-category-order': [order: Record<string, number>]
    'disable-category': [category: string]
    'delete-category': [category: string]
}>()

const treeNodes = computed(() => {
    const roots: Record<string, CategoryNode> = {}

    props.filteredLinks.forEach(link => {
        let catStr = link.content.category || t('tools.webNav.defaultCategory')
        catStr = catStr.trim()

        const segments = catStr.split(/\s*\/\s*/).filter(Boolean)

        if (segments.length === 0) {
            segments.push(t('tools.webNav.defaultCategory'))
        }

        const rootName = segments[0]!
        if (!roots[rootName]) {
            roots[rootName] = { name: rootName, fullPath: rootName, links: [], children: {} }
        }

        let currentNode = roots[rootName]!
        let currentPath = rootName

        for (let i = 1; i < segments.length; i++) {
            const seg = segments[i]!
            currentPath = `${currentPath} / ${seg}`

            if (!currentNode.children[seg]) {
                currentNode.children[seg] = { name: seg, fullPath: currentPath, links: [], children: {} }
            }
            currentNode = currentNode.children[seg]!
        }
        currentNode.links.push(link)
    })

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
</script>
