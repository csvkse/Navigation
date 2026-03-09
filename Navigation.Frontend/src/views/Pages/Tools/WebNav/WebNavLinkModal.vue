<template>
    <Dialog :open="open" @update:open="$emit('update:open', $event)">
        <DialogContent class="sm:max-w-[425px]">
            <DialogHeader>
                <DialogTitle>{{ isEdit ? t('tools.webNav.editLink') : t('tools.webNav.addLink') }}
                </DialogTitle>
            </DialogHeader>
            <div class="grid gap-6 py-4">
                <div class="grid grid-cols-2 gap-4">
                    <div class="grid gap-2">
                        <Label>{{ t('tools.webNav.name') }}</Label>
                        <Input v-model="form.name" placeholder="Google" />
                    </div>
                    <div class="grid gap-2">
                        <Label>{{ t('tools.webNav.category') }}</Label>
                        <Input v-model="form.category" :placeholder="t('tools.webNav.defaultCategory')" />
                        <div class="flex flex-wrap gap-1.5 mt-1">
                            <button v-for="cat in ['应用', '搜索', '社交', '工具', '技术', '娱乐', '工作', '常用']" :key="cat"
                                @click="form.category = cat"
                                class="text-[10px] px-2 py-0.5 rounded-md bg-muted/50 hover:bg-primary/20 hover:text-primary transition-colors border border-border/50">
                                {{ cat }}
                            </button>
                        </div>
                    </div>
                </div>
                <div class="grid gap-2">
                    <Label>{{ t('tools.webNav.url') }}</Label>
                    <Input v-model="form.url" placeholder="https://google.com" />
                </div>

                <div class="grid grid-cols-2 gap-4">
                    <div class="grid gap-2">
                        <Label>Icon (Lucide Name)</Label>
                        <div class="flex flex-col gap-2">
                            <div class="flex gap-2">
                                <Input v-model="form.icon" placeholder="Gamepad, Layout, etc." />
                                <div
                                    class="h-10 w-10 flex-shrink-0 bg-muted/50 rounded-lg flex items-center justify-center border border-border/50">
                                    <component :is="getIcon(form.icon)" class="h-4 w-4" v-if="form.icon" />
                                </div>
                            </div>
                            <div
                                class="grid grid-cols-6 gap-2 p-2 bg-muted/30 rounded-xl max-h-32 overflow-y-auto border border-border/20">
                                <button v-for="(comp, name) in icons" :key="name" @click="form.icon = name"
                                    class="aspect-square flex items-center justify-center rounded-lg hover:bg-primary/20 hover:text-primary transition-all p-1.5 group"
                                    :title="name">
                                    <component :is="comp" class="w-full h-full opacity-60 group-hover:opacity-100" />
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class="grid gap-2">
                        <Label>Color (Hex)</Label>
                        <div class="space-y-3">
                            <div class="flex gap-2">
                                <Input v-model="form.color" placeholder="#3b82f6" />
                                <div class="h-10 w-10 flex-shrink-0 rounded-lg border border-border/50 shadow-inner"
                                    :style="{ backgroundColor: form.color || 'transparent' }" />
                            </div>
                            <div class="grid grid-cols-6 gap-1.5 p-2 bg-muted/30 rounded-xl border border-border/20">
                                <button v-for="color in recommendedColors" :key="color" @click="form.color = color"
                                    class="aspect-square rounded-md transition-all hover:scale-110 active:scale-95 border border-border/30 hover:shadow-lg"
                                    :style="{ backgroundColor: color }" :title="color">
                                </button>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="grid grid-cols-2 gap-4">
                    <div class="flex items-center justify-between p-3 rounded-2xl bg-muted/30 border border-border/40 cursor-pointer hover:bg-muted/50 transition-colors"
                        @click="form.isApp = !form.isApp">
                        <div class="space-y-0.5">
                            <Label class="cursor-pointer text-[11px] font-bold">
                                {{ t('tools.webNav.isApp') || 'Set as Application' }}
                            </Label>
                        </div>
                        <div class="relative inline-flex items-center">
                            <input type="checkbox" :checked="form.isApp"
                                @change="form.isApp = ($event.target as HTMLInputElement).checked"
                                class="sr-only peer" />
                            <div class="w-9 h-5 bg-muted-foreground/20 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-primary transition-all"
                                :class="{ 'bg-primary': form.isApp }">
                                <div class="absolute top-[2px] left-[2px] bg-white rounded-full h-4 w-4 transition-all"
                                    :class="form.isApp ? 'translate-x-4' : 'translate-x-0'"></div>
                            </div>
                        </div>
                    </div>

                    <div class="flex items-center justify-between p-3 rounded-2xl bg-muted/30 border border-border/40 cursor-pointer hover:bg-muted/50 transition-colors"
                        @click="form.isApplied = !form.isApplied">
                        <div class="space-y-0.5">
                            <Label class="cursor-pointer text-[11px] font-bold">
                                {{ t('tools.webNav.isApplied') }}
                            </Label>
                        </div>
                        <div class="relative inline-flex items-center">
                            <input type="checkbox" :checked="form.isApplied"
                                @change="form.isApplied = ($event.target as HTMLInputElement).checked"
                                class="sr-only peer" />
                            <div class="w-9 h-5 bg-muted-foreground/20 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-primary transition-all"
                                :class="{ 'bg-primary': form.isApplied }">
                                <div class="absolute top-[2px] left-[2px] bg-white rounded-full h-4 w-4 transition-all"
                                    :class="form.isApplied ? 'translate-x-4' : 'translate-x-0'"></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <DialogFooter>
                <Button :disabled="saving" @click="$emit('save')" class="rounded-xl w-full">
                    <Loader2 v-if="saving" class="mr-2 h-4 w-4 animate-spin" />
                    {{ t('common.save') }}
                </Button>
            </DialogFooter>
        </DialogContent>
    </Dialog>
</template>

<script setup lang="ts">
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Loader2, Bookmark } from 'lucide-vue-next'
import * as LucideIcons from 'lucide-vue-next'

const { t } = useI18n()

interface LinkForm {
    name: string
    url: string
    category: string
    icon: string
    color: string
    isApp: boolean
    isApplied: boolean
    sortIndex?: number
}

interface Props {
    open: boolean
    isEdit: boolean
    form: LinkForm
    saving: boolean
    icons: Record<string, any>
    recommendedColors: string[]
}

defineProps<Props>()

defineEmits<{
    'update:open': [value: boolean]
    'save': []
}>()

const getIcon = (name: string) => {
    if (!name) return Bookmark
    const iconName = name.charAt(0).toUpperCase() + name.slice(1)
    return (LucideIcons as any)[iconName] || Bookmark
}
</script>
