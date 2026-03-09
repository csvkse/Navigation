<template>
    <Dialog :open="open" @update:open="$emit('update:open', $event)">
        <DialogContent class="sm:max-w-[700px] rounded-3xl">
            <DialogHeader>
                <DialogTitle>{{ t('tools.webNav.bulkAction') || 'Bulk Import/Export' }}</DialogTitle>
                <DialogDescription>
                    {{ t('tools.webNav.bulkFormat') }}
                </DialogDescription>
            </DialogHeader>
            <div class="py-4">
                <textarea v-model="localText"
                    class="w-full h-80 p-4 rounded-2xl bg-muted/30 border border-border/50 text-xs font-mono focus:ring-2 focus:ring-primary/20 outline-none resize-none"
                    :placeholder="t('tools.webNav.bulkPlaceholder')"></textarea>
            </div>
            <DialogFooter class="gap-2">
                <Button variant="outline" @click="$emit('update:open', false)" class="rounded-xl">{{ t('common.cancel')
                }}</Button>
                <Button v-if="!isReadOnly" @click="$emit('import')" :disabled="loading" class="rounded-xl px-8">
                    <Loader2 v-if="loading" class="mr-2 h-4 w-4 animate-spin" />
                    {{ t('tools.webNav.import') || 'Execute Import' }}
                </Button>
            </DialogFooter>
        </DialogContent>
    </Dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog'
import { Loader2 } from 'lucide-vue-next'

const { t } = useI18n()

interface Props {
    open: boolean
    text: string
    loading: boolean
    isReadOnly?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
    'update:open': [value: boolean]
    'update:text': [value: string]
    'import': []
}>()

const localText = ref(props.text)

watch(() => props.text, (newText) => {
    localText.value = newText
})

watch(localText, (newText) => {
    emit('update:text', newText)
})
</script>
