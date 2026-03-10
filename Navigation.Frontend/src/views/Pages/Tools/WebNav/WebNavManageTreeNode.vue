<template>
    <template v-if="node.links.length > 0 || hasChildren">
        <!-- Node Header Row -->
        <tr class="hover:bg-muted/10 transition-colors group/row border-b border-border/10" :class="[
            depth === 0 ? 'bg-muted/5' : '',
            draggingId === 'folder:' + node.fullPath ? 'opacity-30 bg-primary/5' : ''
        ]" draggable="true"
            @dragstart.stop="$emit('drag-start', $event, { id: 'folder:' + node.fullPath, content: { isCategory: true, category: node.fullPath, name: node.name } } as any)"
            @dragend.stop="$emit('drag-end')" @dragover.prevent="handleFolderDragOver($event)"
            @drop.stop="handleFolderDrop($event, node)">
            <td class="p-4 pl-4" :style="{ paddingLeft: `${(depth * 1.5) + 1}rem` }">
                <div class="flex items-center gap-2 relative">
                    <!-- Tree line decoration -->
                    <div v-if="depth > 0" class="absolute -left-3 top-1/2 w-4 h-[1px] bg-border/40"></div>
                    <div v-if="depth > 0" class="absolute -left-3 -top-4 w-[1px] h-[calc(50%+1rem)] bg-border/40"></div>

                    <button @click="isCollapsed = !isCollapsed"
                        class="p-1 hover:bg-black/5 dark:hover:bg-white/5 rounded">
                        <component :is="isCollapsed ? ChevronRight : ChevronDown" class="w-4 h-4 opacity-50" />
                    </button>
                    <!-- Drag handle & Folder Icon -->
                    <GripVertical
                        class="h-4 w-4 text-muted-foreground/20 group-hover/row:text-primary transition-colors cursor-grab shrink-0" />
                    <div class="h-8 w-8 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
                        <Folder class="h-4 w-4 text-primary" />
                    </div>
                </div>
            </td>
            <td class="p-4" colspan="6">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-3">
                        <div class="font-bold text-base truncate" :title="node.name">{{ node.name.length > 15 ?
                            node.name.substring(0, 15) + '...' : node.name }}</div>
                        <Badge variant="outline" class="rounded-lg text-[10px] bg-muted/20 border-border/50 font-mono">
                            {{ totalLinksCount }}
                        </Badge>
                    </div>
                    <div class="flex items-center gap-2">
                        <!-- Category Enable/Disable Toggle -->
                        <div @click.stop="$emit('disable-category', node.fullPath)"
                            class="flex justify-center group/toggle"
                            :title="t('tools.webNav.disableCategory') || 'Toggle Category'">
                            <div :class="[
                                'w-10 h-5 rounded-full relative transition-all duration-300 cursor-pointer border border-border/50',
                                allLinksApplied ? 'bg-primary' : 'bg-muted-foreground/20'
                            ]">
                                <div :class="[
                                    'absolute top-0.5 w-3.5 h-3.5 rounded-full bg-white transition-all duration-300 shadow-sm',
                                    allLinksApplied ? 'left-[22px]' : 'left-0.5'
                                ]"></div>
                            </div>
                        </div>
                        <!-- Delete Category -->
                        <button @click.stop="$emit('delete-category', node.fullPath)"
                            class="p-1.5 rounded-md hover:bg-destructive/10 text-muted-foreground/30 hover:text-destructive transition-all active:scale-90"
                            :title="t('tools.webNav.deleteCategory') || 'Delete Category'">
                            <Trash2 class="w-3.5 h-3.5" />
                        </button>
                    </div>
                </div>
            </td>
        </tr>

        <!-- Children Area -->
        <template v-if="!isCollapsed">
            <!-- Links for this folder -->
            <tr v-for="link in node.links" :key="link.id" draggable="true"
                @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')" @dragover.prevent
                @drop.stop="$emit('drop', $event, link)" class="hover:bg-muted/10 transition-colors group/row"
                :class="draggingId === link.id ? 'opacity-30 bg-primary/5' : ''">

                <td class="p-4 pl-4 relative" :style="{ paddingLeft: `${((depth + 1) * 1.5) + 1}rem` }">
                    <div v-if="depth >= 0" class="absolute -left-3 top-1/2 w-4 h-[1px] bg-border/40"
                        :style="{ left: `${(depth * 1.5) + 1}rem` }"></div>
                    <div v-if="depth >= 0" class="absolute -left-3 -top-4 w-[1px] h-[calc(50%+1rem)] bg-border/40"
                        :style="{ left: `${(depth * 1.5) + 1}rem` }"></div>

                    <GripVertical
                        class="h-4 w-4 text-muted-foreground/20 group-hover/row:text-primary transition-colors cursor-grab inline-block ml-8" />
                </td>

                <td class="p-4">
                    <div class="flex items-center gap-3">
                        <div class="h-8 w-8 rounded-lg bg-muted/40 flex items-center justify-center shrink-0"
                            :style="{ color: link.content.color || 'inherit' }">
                            <component :is="getIcon(link.content.icon)" class="h-4 w-4" v-if="link.content.icon" />
                            <span v-else class="text-sm opacity-40 font-bold">{{ link.content.name[0]?.toUpperCase()
                                }}</span>
                        </div>
                        <div class="min-w-0">
                            <div class="font-bold text-base truncate" :title="link.content.name">{{
                                link.content.name.length > 15 ? link.content.name.substring(0, 15) + '...' :
                                    link.content.name }}</div>
                            <div class="text-[11px] text-muted-foreground truncate" :title="link.content.url">{{
                                link.content.url }}</div>
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
                            t('tools.webNav.default') }}</span>
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

            <!-- Subfolders -->
            <template v-for="subNode in sortedChildren" :key="subNode.fullPath">
                <WebNavManageTreeNode :node="subNode" :depth="depth + 1" :dragging-id="draggingId"
                    :category-order="categoryOrder" @drag-start="(e: DragEvent, l: Link) => $emit('drag-start', e, l)"
                    @drag-end="$emit('drag-end')" @drop="(e: DragEvent, l: Link) => $emit('drop', e, l)"
                    @edit-link="(l: Link) => $emit('edit-link', l)"
                    @delete-link="(id: string) => $emit('delete-link', id)"
                    @toggle-apply="(l: Link) => $emit('toggle-apply', l)"
                    @toggle-is-app="(l: Link) => $emit('toggle-is-app', l)"
                    @update-category-order="(o: Record<string, number>) => $emit('update-category-order', o)"
                    @disable-category="(cat: string) => $emit('disable-category', cat)"
                    @delete-category="(cat: string) => $emit('delete-category', cat)" />
            </template>
        </template>
    </template>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { GripVertical, Edit2, Trash2, Bookmark, Folder, ChevronDown, ChevronRight } from 'lucide-vue-next'
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

interface CategoryNode {
    name: string
    fullPath: string
    links: Link[]
    children: Record<string, CategoryNode>
}

interface Props {
    node: CategoryNode
    depth: number
    draggingId: string | null
    categoryOrder?: Record<string, number>
}

const props = defineProps<Props>()

const emit = defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, link: Link]
    'edit-link': [link: Link]
    'delete-link': [id: string]
    'toggle-apply': [link: Link]
    'toggle-is-app': [link: Link]
    'update-category-order': [order: Record<string, number>]
    'disable-category': [category: string]
    'delete-category': [category: string]
}>()

const isCollapsed = ref(true)

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

// Drag and drop for folder sorting
let dragOverQuadrant = 0 // 1: top, 2: bottom

const handleFolderDragOver = (e: DragEvent) => {
    const target = e.currentTarget as HTMLElement
    const rect = target.getBoundingClientRect()
    const y = e.clientY - rect.top
    dragOverQuadrant = y < rect.height / 2 ? 1 : 2

    // Add visual feedback
    target.style.borderTop = dragOverQuadrant === 1 ? '2px solid hsl(var(--primary))' : ''
    target.style.borderBottom = dragOverQuadrant === 2 ? '2px solid hsl(var(--primary))' : ''
}

const handleFolderDrop = (e: DragEvent, node: CategoryNode) => {
    const targetEl = e.currentTarget as HTMLElement
    targetEl.style.borderTop = ''
    targetEl.style.borderBottom = ''

    const sourceId = e.dataTransfer?.getData('text/plain')
    if (sourceId?.startsWith('folder:')) {
        const sourcePath = sourceId.substring(7)
        if (sourcePath === node.fullPath) return // Dropped on itself

        // Prevent sorting dropping inside its own children (already checked in folder move, but good to ensure order holds)
        if (node.fullPath.startsWith(sourcePath + ' /')) return

        // We only allow sorting within the SAME parent folder level for simplicity
        const parentOfSource = sourcePath.includes(' / ') ? sourcePath.substring(0, sourcePath.lastIndexOf(' / ')) : ''
        const parentOfTarget = node.fullPath.includes(' / ') ? node.fullPath.substring(0, node.fullPath.lastIndexOf(' / ')) : ''

        if (parentOfSource === parentOfTarget) {
            e.preventDefault()
            e.stopPropagation()

            // Generate new order
            const currentOrder = { ...props.categoryOrder }

            // Get all siblings in current sorted order
            if (parentOfTarget === '') {
                // Root level
                // We'd have to access the global roots list to sort accurately, but we can just use the provided categoryOrder
            }

            // A simpler approach: just assign a sort index based on the target
            const targetSortIndex = currentOrder[node.fullPath] ?? 999999
            let newSortIndex = targetSortIndex

            if (dragOverQuadrant === 1) {
                newSortIndex -= 0.5
            } else {
                newSortIndex += 0.5
            }

            currentOrder[sourcePath] = newSortIndex

            emit('update-category-order', currentOrder)
            return
        }
    }

    // If not handled as a sibling sort, pass onto normal drop (folder move inside)
    emit('drop', e, { content: { isApp: false, category: node.fullPath } } as unknown as Link)
}

const getTotalLinks = (n: CategoryNode): number => {
    let count = n.links.length
    for (const key in n.children) {
        count += getTotalLinks(n.children[key]!)
    }
    return count
}

const totalLinksCount = computed(() => getTotalLinks(props.node))

const getAllLinksApplied = (n: CategoryNode): boolean => {
    if (n.links.some(l => l.content.isApplied === false)) return false
    for (const key in n.children) {
        if (!getAllLinksApplied(n.children[key]!)) return false
    }
    return n.links.length > 0 || Object.keys(n.children).length > 0
}

const allLinksApplied = computed(() => getAllLinksApplied(props.node))

const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
}
</script>

<script lang="ts">
export default {
    name: 'WebNavManageTreeNode'
}
</script>
