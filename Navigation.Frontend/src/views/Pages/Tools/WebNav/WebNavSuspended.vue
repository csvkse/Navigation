<template>
    <div class="fixed bottom-24 left-6 z-40 flex flex-col items-start gap-4" v-if="isManageMode">
        <!-- Drop Zone / Toggle Button -->
        <div class="relative group" @dragover.prevent
            @drop.stop="$emit('drop', $event, { content: { isApplied: false } })">

            <!-- Badge -->
            <div v-if="suspendedLinks.length > 0"
                class="absolute -top-2 -right-2 w-6 h-6 rounded-full bg-destructive text-destructive-foreground flex items-center justify-center text-xs font-bold shadow-lg z-50 pointer-events-none border-2 border-background">
                {{ suspendedLinks.length }}
            </div>

            <Button size="icon" :variant="isOpen ? 'default' : 'secondary'"
                class="h-14 w-14 rounded-full shadow-xl transition-all duration-300 hover:scale-110" :class="[
                    isOpen ? 'rotate-90' : '',
                    isDragOver ? 'scale-125 ring-4 ring-destructive/30 bg-destructive/10' : ''
                ]" @click="isOpen = !isOpen" @dragenter="isDragOver = true" @dragleave="isDragOver = false"
                @drop="isDragOver = false">
                <ArchiveRestore class="h-6 w-6" :class="isDragOver ? 'text-destructive' : ''" />
            </Button>

            <!-- Expanded List -->
            <Transition enter-active-class="transition duration-200 ease-out"
                enter-from-class="opacity-0 translate-y-4 scale-95" enter-to-class="opacity-100 translate-y-0 scale-100"
                leave-active-class="transition duration-150 ease-in"
                leave-from-class="opacity-100 translate-y-0 scale-100"
                leave-to-class="opacity-0 translate-y-4 scale-95">

                <div v-if="isOpen"
                    class="absolute bottom-full left-0 mb-4 w-72 bg-card/95 backdrop-blur-xl border border-border/50 rounded-2xl shadow-2xl p-4 origin-bottom-left flex flex-col max-h-[60vh]">

                    <div class="flex items-center justify-between mb-3 px-1">
                        <h3 class="font-bold text-sm text-foreground/80">{{ t('tools.webNav.suspendedLinks') }}</h3>
                        <div class="flex items-center gap-1">
                            <Button v-if="suspendedLinks.length > 0" variant="ghost" size="sm"
                                class="h-6 px-2 rounded-full text-[10px] font-bold text-primary hover:bg-primary/10"
                                @click="$emit('restore-all')">
                                <RotateCcw class="h-3 w-3 mr-1" />
                                {{ t('tools.webNav.enableAll') || 'Enable All' }}
                            </Button>
                            <Button variant="ghost" size="icon" class="h-6 w-6 rounded-full" @click="isOpen = false">
                                <X class="h-4 w-4" />
                            </Button>
                        </div>
                    </div>

                    <div class="flex-1 overflow-y-auto space-y-2 pr-1 custom-scrollbar">
                        <div v-if="suspendedLinks.length === 0"
                            class="text-center py-8 text-muted-foreground/50 text-xs border-2 border-dashed border-border/30 rounded-xl">
                            {{ t('tools.webNav.dragHereToSuspend') }}
                        </div>

                        <div v-for="link in suspendedLinks" :key="link.id" draggable="true"
                            @dragstart="$emit('drag-start', $event, link)" @dragend="$emit('drag-end')"
                            class="group relative flex items-center gap-3 p-2.5 rounded-xl hover:bg-muted/50 transition-all border border-transparent hover:border-border/30 cursor-grab active:cursor-grabbing bg-card/50">

                            <!-- Icon -->
                            <div class="h-8 w-8 rounded-lg bg-muted/50 flex items-center justify-center shrink-0 text-muted-foreground/70"
                                :style="{ color: link.content.color || 'inherit' }">
                                <component :is="getIcon(link.content.icon)" class="h-4 w-4" v-if="link.content.icon" />
                                <span v-else class="text-[10px] font-bold opacity-60">{{
                                    link.content.name[0]?.toUpperCase() }}</span>
                            </div>

                            <div class="min-w-0 flex-1">
                                <div
                                    class="text-sm font-medium truncate opacity-70 group-hover:opacity-100 transition-opacity">
                                    {{ link.content.name }}
                                </div>
                                <div class="text-[10px] text-muted-foreground truncate opacity-50">
                                    {{ link.content.category || t('tools.webNav.uncategorized') }}
                                </div>
                            </div>

                            <!-- Restore Action -->
                            <Button variant="ghost" size="icon"
                                class="h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity text-primary hover:bg-primary/10 hover:text-primary rounded-lg"
                                @click.stop="$emit('restore-link', link.id)">
                                <Undo2 class="h-3.5 w-3.5" />
                            </Button>
                            <!-- Delete Action -->
                            <Button variant="ghost" size="icon"
                                class="h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity text-destructive hover:bg-destructive/10 hover:text-destructive rounded-lg"
                                @click.stop="$emit('delete-link', link.id)">
                                <Trash2 class="h-3.5 w-3.5" />
                            </Button>
                        </div>
                    </div>
                </div>
            </Transition>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/components/ui/button'
import { ArchiveRestore, Trash2, X, Bookmark, Undo2, RotateCcw } from 'lucide-vue-next'
import * as LucideIcons from 'lucide-vue-next'
import { useI18n } from '@/composables/useI18n'

const { t } = useI18n()
const isOpen = ref(false)
const isDragOver = ref(false)

interface Link {
    id: string
    content: {
        name: string
        url: string
        icon?: string
        color?: string
        category?: string
        isApplied?: boolean
    }
}

interface Props {
    suspendedLinks: Link[]
    isManageMode: boolean
}

defineProps<Props>()

defineEmits<{
    'drag-start': [event: DragEvent, link: Link]
    'drag-end': []
    'drop': [event: DragEvent, target: any]
    'delete-link': [id: string]
    'restore-link': [id: string]
    'restore-all': []
}>()

const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
}
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
    width: 4px;
}

.custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
}

.custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: rgba(156, 163, 175, 0.3);
    border-radius: 20px;
}

.custom-scrollbar::-webkit-scrollbar-thumb:hover {
    background-color: rgba(156, 163, 175, 0.5);
}
</style>
