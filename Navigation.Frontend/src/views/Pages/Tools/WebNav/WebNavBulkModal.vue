<template>
    <Dialog :open="open" @update:open="$emit('update:open', $event)">
        <DialogContent
            class="sm:max-w-[700px] rounded-2xl sm:rounded-3xl max-h-[90dvh] flex flex-col overflow-hidden p-4 sm:p-6"
            @dragover.prevent="isDragging = true" @dragleave.prevent="isDragging = false" @drop.prevent="handleDrop">
            <DialogHeader class="shrink-0">
                <DialogTitle class="text-base sm:text-lg">{{ t('tools.webNav.bulkAction') || 'Bulk Import/Export' }}
                </DialogTitle>
                <DialogDescription class="text-xs sm:text-sm">
                    {{ t('tools.webNav.bulkFormat') }}
                </DialogDescription>
            </DialogHeader>
            <div class="py-2 sm:py-4 relative flex-1 min-h-0">
                <textarea v-model="localText"
                    class="w-full h-48 sm:h-80 p-3 sm:p-4 rounded-xl sm:rounded-2xl bg-muted/30 border border-border/50 text-[11px] sm:text-xs font-mono focus:ring-2 focus:ring-primary/20 outline-none resize-none relative z-10"
                    :placeholder="t('tools.webNav.bulkPlaceholder')"></textarea>

                <!-- Drop Overlay -->
                <div v-if="isDragging"
                    class="absolute inset-2 sm:inset-4 rounded-xl sm:rounded-2xl bg-background/80 backdrop-blur-sm border-2 border-dashed border-primary z-20 flex items-center justify-center transition-all pointer-events-none">
                    <div class="flex flex-col items-center text-primary">
                        <Upload class="h-8 w-8 sm:h-10 sm:w-10 mb-2 sm:mb-3 animate-bounce" />
                        <span class="font-bold text-sm sm:text-lg">Drop Bookmarks HTML here</span>
                    </div>
                </div>
            </div>
            <DialogFooter
                class="flex flex-col gap-2 sm:gap-2 w-full shrink-0 sm:flex-row sm:justify-between sm:items-center">
                <!-- File Actions: Upload & Export -->
                <div class="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
                    <input type="file" ref="fileInput" accept=".html" class="hidden" @change="handleFileUpload" />
                    <Button variant="outline" @click="triggerFileInput"
                        class="rounded-xl w-full sm:w-auto text-xs h-9 sm:h-10">
                        <FileText class="h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5 sm:mr-2 shrink-0" />
                        <span class="truncate">{{ t('tools.webNav.uploadBookmarks') || 'Import Bookmarks HTML' }}</span>
                    </Button>
                    <Button variant="outline" @click="exportAsBookmarksHtml" :disabled="!localText.trim()"
                        class="rounded-xl w-full sm:w-auto text-xs h-9 sm:h-10">
                        <Download class="h-3.5 w-3.5 sm:h-4 sm:w-4 mr-1.5 sm:mr-2 shrink-0" />
                        <span class="truncate">{{ t('tools.webNav.exportBookmarksHtml') || 'Export Bookmarks HTML'
                            }}</span>
                    </Button>
                </div>

                <!-- Primary Actions -->
                <div class="flex items-center gap-2 w-full sm:w-auto sm:justify-end">
                    <Button variant="outline" @click="$emit('update:open', false)"
                        class="rounded-xl flex-1 sm:flex-none h-9 sm:h-10 text-xs">
                        {{ t('common.cancel') }}
                    </Button>
                    <Button v-if="!isReadOnly" @click="$emit('import')" :disabled="loading"
                        class="rounded-xl flex-1 sm:flex-none sm:px-8 h-9 sm:h-10 text-xs">
                        <Loader2 v-if="loading" class="mr-1.5 sm:mr-2 h-3.5 w-3.5 sm:h-4 sm:w-4 animate-spin" />
                        {{ t('tools.webNav.import') || 'Execute Import' }}
                    </Button>
                </div>
            </DialogFooter>
        </DialogContent>
    </Dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog'
import { Loader2, Upload, FileText, Download } from 'lucide-vue-next'
import { toast } from 'vue-sonner'

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
const isDragging = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)

watch(() => props.text, (newText) => {
    localText.value = newText
})

watch(localText, (newText) => {
    emit('update:text', newText)
})

const triggerFileInput = () => {
    fileInput.value?.click()
}

const handleFileUpload = async (event: Event) => {
    const target = event.target as HTMLInputElement
    const file = target.files?.[0]
    if (file) {
        await processFile(file)
    }
    // Reset file input so same file can be uploaded again if needed
    if (fileInput.value) fileInput.value.value = ''
}

const handleDrop = async (event: DragEvent) => {
    isDragging.value = false
    const file = event.dataTransfer?.files?.[0]
    if (file) {
        if (!file.name.endsWith('.html') && file.type !== 'text/html') {
            toast.error(t('tools.webNav.invalidFile') || 'Please upload a valid HTML file')
            return
        }
        await processFile(file)
    }
}

const processFile = async (file: File) => {
    try {
        const text = await file.text()
        const parsedTsv = parseBookmarks(text)
        if (parsedTsv) {
            // Append or Overwrite? Better to append if there's already text, or just overwrite if empty
            if (localText.value.trim()) {
                localText.value += '\n' + parsedTsv
            } else {
                localText.value = parsedTsv
            }
            toast.success(t('tools.webNav.parseSuccess') || 'Bookmarks parsed successfully')
        } else {
            toast.warning(t('tools.webNav.noBookmarksFound') || 'No bookmarks found in the file')
        }
    } catch (e) {
        console.error('File parsing failed:', e)
        toast.error(t('tools.webNav.parseFailed') || 'Failed to parse bookmarks file')
    }
}

const parseBookmarks = (htmlContent: string): string => {
    const parser = new DOMParser()
    const doc = parser.parseFromString(htmlContent, 'text/html')

    const aTags = Array.from(doc.querySelectorAll('a'))
    const results: string[] = []

    aTags.forEach((a) => {
        const url = a.getAttribute('href') || ''
        let name = a.textContent || ''
        const icon = a.getAttribute('icon') || ''

        if (!url || url.startsWith('javascript:')) return

        // Clean name (remove newlines and tabs)
        name = name.replace(/[\n\t\r]/g, ' ').trim()

        let categories: string[] = []
        let node: HTMLElement | null = a

        while (node) {
            if (node.tagName === 'DL') {
                const prev = node.previousElementSibling
                let folderName = ''
                if (prev) {
                    if (prev.tagName === 'DT') {
                        const h3 = prev.querySelector('h3')
                        if (h3) {
                            folderName = h3.textContent || ''
                        }
                    } else if (prev.tagName === 'H3') {
                        folderName = prev.textContent || ''
                    }
                }

                if (folderName) {
                    // Prepend because we traverse from inner to outer
                    categories.unshift(folderName.replace(/[\n\t\r]/g, ' ').trim())
                }
            }
            node = node.parentElement
        }

        const ignoreRoots = ['Bookmarks bar', '书签栏', 'Bookmarks', '书签', 'Bookmarks Menu', '书签菜单', 'Favorites bar', '收藏夹栏', 'Other bookmarks', '其他书签']
        categories = categories.filter(c => !ignoreRoots.includes(c))

        const category = categories.join(' / ')

        // TSV Format: name \t url \t note(isApp) \t icon \t category
        // we leave 'note' empty so it treats it as a standard bookmark
        results.push(`${name}\t${url}\t\t${icon}\t${category}`)
    })

    return results.join('\n')
}

// ===== Export as Chrome Bookmarks HTML =====
interface BookmarkTreeNode {
    name: string
    url?: string
    icon?: string
    children: Record<string, BookmarkTreeNode>
    links: Array<{ name: string; url: string; icon: string }>
}

const exportAsBookmarksHtml = () => {
    const text = localText.value.trim()
    if (!text) return

    const lines = text.split('\n').filter(l => l.trim())

    // Build a tree from TSV lines
    const root: BookmarkTreeNode = { name: 'root', children: {}, links: [] }

    lines.forEach(line => {
        const parts = line.split('\t')
        const name = parts[0]?.trim() || ''
        const url = parts[1]?.trim() || ''
        const icon = parts[3]?.trim() || ''
        const category = parts[4]?.trim() || ''

        if (!name || !url) return

        let currentNode = root
        if (category) {
            const segments = category.split(/\s*\/\s*/).filter(Boolean)
            for (const seg of segments) {
                if (!currentNode.children[seg]) {
                    currentNode.children[seg] = { name: seg, children: {}, links: [] }
                }
                currentNode = currentNode.children[seg]!
            }
        }

        currentNode.links.push({ name, url, icon })
    })

    // Generate Netscape Bookmark File HTML
    const timestamp = Math.floor(Date.now() / 1000)

    const renderFolder = (node: BookmarkTreeNode, indent: string): string => {
        let html = ''

        // Render links at this level
        for (const link of node.links) {
            const iconAttr = link.icon ? ` ICON="${escapeHtml(link.icon)}"` : ''
            html += `${indent}<DT><A HREF="${escapeHtml(link.url)}" ADD_DATE="${timestamp}"${iconAttr}>${escapeHtml(link.name)}</A>\n`
        }

        // Render child folders
        for (const [folderName, childNode] of Object.entries(node.children)) {
            html += `${indent}<DT><H3 ADD_DATE="${timestamp}" LAST_MODIFIED="${timestamp}">${escapeHtml(folderName)}</H3>\n`
            html += `${indent}<DL><p>\n`
            html += renderFolder(childNode, indent + '    ')
            html += `${indent}</DL><p>\n`
        }

        return html
    }

    const escapeHtml = (str: string): string => {
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
    }

    let fileContent = `<!DOCTYPE NETSCAPE-Bookmark-file-1>
<!-- This is an automatically generated file.
     It will be read and overwritten.
     DO NOT EDIT! -->
<META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
<TITLE>Bookmarks</TITLE>
<H1>Bookmarks</H1>
<DL><p>
    <DT><H3 ADD_DATE="${timestamp}" LAST_MODIFIED="${timestamp}" PERSONAL_TOOLBAR_FOLDER="true">Bookmarks bar</H3>
    <DL><p>
`

    fileContent += renderFolder(root, '        ')

    fileContent += `    </DL><p>
</DL><p>
`

    // Trigger download
    const blob = new Blob([fileContent], { type: 'text/html;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    const dateStr = new Date().toISOString().slice(0, 10).replace(/-/g, '')
    a.href = url
    a.download = `bookmarks_export_${dateStr}.html`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)

    toast.success(t('tools.webNav.exportSuccess') || 'Bookmarks HTML exported successfully')
}
</script>
