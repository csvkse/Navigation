import { ref, reactive, computed, watch } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { toast } from 'vue-sonner'

export function useMessageBoard(t: (key: string) => string, currentUserArg?: any, activeUserKeyArg?: any) {
    const appStore = useAppStore()
    const baseUrl = import.meta.env.VITE_API_BASE_URL || ''

    // State
    // If external refs passed, use them, else fallback (though useAuth is preferred now)
    const currentUser = currentUserArg || ref<any>(null)
    const activeUserKey = activeUserKeyArg || ref('')
    const loading = ref(false)
    const dbRecordId = ref<string | null>(null)
    const currentTab = ref<'my' | 'public' | 'favorites'>('public')
    const searchKeyword = ref('')

    // Data
    const myMessages = reactive<Record<string, any>>({})
    const favorites = reactive<Record<string, any>>({})
    const publicList = ref<any[]>([])
    const publicPage = ref(1)
    const publicTotal = ref(0)

    // Sync Data when key changes
    watch(activeUserKey, (newKey) => {
        if (newKey) {
            fetchUserData()
        } else {
            // Clear if key removed
            Object.keys(myMessages).forEach(k => delete myMessages[k])
            Object.keys(favorites).forEach(k => delete favorites[k])
            dbRecordId.value = null
        }
    }, { immediate: true })

    // Auth State (Legacy/Internal if not external)
    // removed local auth state

    // Computed
    const totalPages = computed(() => Math.ceil(publicTotal.value / 20) || 1)

    const displayList = computed(() => {
        if (currentTab.value === 'my') {
            return Object.values(myMessages).sort((a: any, b: any) => new Date(b.createTime || 0).getTime() - new Date(a.createTime || 0).getTime())
        } else if (currentTab.value === 'favorites') {
            return Object.values(favorites).sort((a: any, b: any) => new Date(b.time || 0).getTime() - new Date(a.time || 0).getTime())
        } else {
            return publicList.value
        }
    })

    // Helpers - Remove auth helpers if not needed, or keep for internal logic?
    // Actually getAuthKey etc were used in `handleLogin`, now removed. Only used in fetchUserData maybe? No.
    // Removed helpers.

    // Data Actions
    const fetchUserData = async () => {
        if (!activeUserKey.value) return
        loading.value = true
        try {
            const key = activeUserKey.value
            const storageKey = `u_msg_${key}`
            const res = await fetch(`${baseUrl}/FastDB?key=${encodeURIComponent(storageKey)}`)
            const data = await res.json()

            // Clear current
            for (const k in myMessages) delete myMessages[k]
            for (const k in favorites) delete favorites[k]

            if (data && data.length > 0) {
                const record = data[0]
                dbRecordId.value = record.id
                const content = typeof record.content === 'string' ? JSON.parse(record.content) : record.content

                if (content.myMessages) Object.assign(myMessages, content.myMessages)
                if (content.favorites) Object.assign(favorites, content.favorites)
            } else {
                dbRecordId.value = null
            }
        } catch (e) {
            toast.error('Sync failed')
        } finally {
            loading.value = false
        }
    }

    const saveUserData = async () => {
        if (!currentUser.value || !activeUserKey.value) return

        const storageKey = `u_msg_${activeUserKey.value}`
        const content = {
            myMessages,
            favorites,
            updatedAt: Date.now()
        }

        try {
            if (dbRecordId.value) {
                await fetch(`${baseUrl}/FastDB/${dbRecordId.value}?key=${encodeURIComponent(storageKey)}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(content)
                })
            } else {
                const res = await fetch(`${baseUrl}/FastDB?key=${encodeURIComponent(storageKey)}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(content)
                })
                const data = await res.json()
                dbRecordId.value = data.id
            }
        } catch (e) {
            console.error('Save failed', e)
        }
    }

    const loadPublicMessages = async (page: number, keyword = '') => {
        loading.value = true
        try {
            let url = `${baseUrl}/api/Danmu/messages?page=${page}&pageSize=20`
            if (keyword) url += `&keyword=${encodeURIComponent(keyword)}`;
            const res = await fetch(url)
            const data = await res.json()
            publicTotal.value = data.total || 0
            publicList.value = data.list || data || []
        } catch (e) { }
        finally { loading.value = false }
    }

    const changePage = (delta: number) => {
        const next = publicPage.value + delta
        if (next < 1 || next > totalPages.value) return
        publicPage.value = next
        loadPublicMessages(next, searchKeyword.value || '')
    }

    const toggleFavorite = (msg: any) => {
        if (favorites[msg.id]) {
            delete favorites[msg.id]
            toast.info(t('tools.messageBoard.favoritesRemoved'))
        } else {
            favorites[msg.id] = { ...msg, time: new Date().toISOString() }
            toast.success(t('tools.messageBoard.favoritesAdded'))
        }
        saveUserData()
    }

    const sendMessage = async (content: string, onSuccess: (msg: any) => void) => {
        if (!content) return

        const formData = new FormData()
        formData.append('content', content)
        formData.append('clientId', appStore.currentFingerId)

        try {
            const res = await fetch(`${baseUrl}/api/Danmu`, { method: 'POST', body: formData })
            const data = await res.json()

            const localMsg = {
                id: data.id || 'temp_' + Date.now(),
                content: content,
                createTime: new Date().toISOString(),
                status: data.status,
                auditReason: data.auditReason
            }

            myMessages[localMsg.id] = localMsg
            saveUserData()

            if (data.status === 'Approved') {
                toast.success(t('tools.messageBoard.sendSuccess'))
                onSuccess(localMsg)
            } else {
                toast.info(t('tools.messageBoard.auditPending'))
            }
            return true
        } catch (e) {
            toast.error(t('tools.messageBoard.sendFailed'))
            return false
        }
    }

    // Auth Actions - REMOVED internal auth management in favor of useAuth
    // Kept fetchUserData and saveUserData as they are data-specific

    return {
        // currentUser, // Don't export if passed in? Or export for convenience
        // activeUserKey,
        currentTab,
        searchKeyword,
        loading,
        myMessages,
        favorites,
        publicList,
        publicPage,
        publicTotal,
        displayList,
        totalPages,

        fetchUserData,
        saveUserData,
        loadPublicMessages,
        changePage,
        toggleFavorite,
        sendMessage,
    }
}
