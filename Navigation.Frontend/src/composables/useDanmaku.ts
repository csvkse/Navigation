import { ref, onMounted, onUnmounted } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { toast } from 'vue-sonner'

export function useDanmaku(
    baseUrl: string,
    myMessages: any, // Reactive object
    saveUserData: () => Promise<void>,
    onFavorite: (msg: any) => void
) {

    const appStore = useAppStore()
    const danmakuContainer = ref<HTMLElement | null>(null)
    const DANMAKU_TRACKS = 6
    const DANMAKU_POOL_MAX = 500

    let danmakuPool: any[] = []
    let poolIndex = 0
    const trackLastEnd = new Array(DANMAKU_TRACKS).fill(0)
    let loopInterval: any = null
    let poolLoopTimeout: any = null
    let sseSource: EventSource | null = null

    const shootDanmaku = (msg: any) => {
        if (!danmakuContainer.value) return
        const el = document.createElement('div')
        el.className = 'danmaku-item'
        if (myMessages[msg.id]) el.classList.add('self')

        // Formatting
        const dateObj = new Date(msg.createTime)
        const timeStr = dateObj.toLocaleString('zh-CN', {
            year: 'numeric', month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit', hour12: false
        })
        el.innerHTML = `<div class="d-content">${msg.content}</div><div class="d-meta">${timeStr}</div>`

        el.onclick = (e) => { e.stopPropagation(); onFavorite(msg); }

        // Track logic
        const now = Date.now()
        // Calculate max tracks that fit in viewport: 
        // Window Height - TopOffset(80) - BottomBar(approx 150 with padding/safe area) - ItemHeight(approx 100)
        // We want the BOTTOM of the item to be above the bottom bar.
        // Item Top = 80 + track * 120. Item Bottom ~= Item Top + 100.
        // So: 80 + track * 120 + 100 < window.innerHeight - 100 (bottom bar)
        // track * 120 < window.innerHeight - 280
        const maxTrackIndex = Math.max(0, Math.floor((window.innerHeight - 280) / 120))
        const effectiveTracks = Math.min(DANMAKU_TRACKS, maxTrackIndex + 1)

        let track = 0
        let min = Infinity
        // Only loop through effective tracks
        for (let i = 0; i < effectiveTracks; i++) {
            if (trackLastEnd[i] < min) { min = trackLastEnd[i]; track = i }
        }
        const dur = 12000 + Math.random() * 6000
        trackLastEnd[track] = now + (dur * 0.2) // spacing

        el.style.top = `${80 + (track * 120)}px` // Offset for header + larger track height for multiline
        danmakuContainer.value.appendChild(el)

        // Animate
        const startX = window.innerWidth
        el.style.transform = `translateX(${startX}px)`

        // Force reflow
        el.getBoundingClientRect()

        el.style.transition = `transform ${dur}ms linear`
        el.style.transform = `translateX(-100%)` // Move fully out

        setTimeout(() => { if (el.parentElement) el.remove() }, dur + 100)
    }

    const fillDanmakuPool = async () => {
        try {
            const res = await fetch(`${baseUrl}/api/Danmu/random`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ count: 20 })
            })
            if (!res.ok) return 0
            const list = await res.json()
            let added = 0
            list.forEach((msg: any) => {
                if (!danmakuPool.find(x => x.id === msg.id)) {
                    danmakuPool.push(msg)
                    added++
                }
            })
            return added
        } catch (e) { return 0 }
    }

    const startDanmakuLoop = () => {
        loopInterval = setInterval(() => {
            if (danmakuPool.length === 0) return
            shootDanmaku(danmakuPool[poolIndex])
            poolIndex++
            if (poolIndex >= danmakuPool.length) poolIndex = 0
        }, 2000)
    }

    const startPoolLoop = () => {
        const loop = async () => {
            if (danmakuPool.length > DANMAKU_POOL_MAX) {
                danmakuPool = danmakuPool.slice(-DANMAKU_POOL_MAX)
                poolIndex = 0
            }
            if (danmakuPool.length < 50) await fillDanmakuPool()
            poolLoopTimeout = setTimeout(loop, 10000)
        }
        loop()
    }

    const connectSSE = () => {
        if (!appStore.currentFingerId) return
        sseSource = new EventSource(`${baseUrl}/api/Danmu/sse?clientId=${appStore.currentFingerId}`)
        sseSource.addEventListener('audit_result', (e: MessageEvent) => {
            const data = JSON.parse(e.data)
            if (myMessages[data.id]) {
                myMessages[data.id].status = data.status
                if (data.reason) myMessages[data.id].auditReason = data.reason
                // Sync
                saveUserData()
                if (data.status === 'Approved') {
                    toast.success('Audited: Approved')
                    shootDanmaku(myMessages[data.id])
                }
            }
        })
    }

    onMounted(() => {
        connectSSE()
        fillDanmakuPool()
        startDanmakuLoop()
        startPoolLoop()
    })

    onUnmounted(() => {
        clearInterval(loopInterval)
        clearTimeout(poolLoopTimeout)
        if (sseSource) sseSource.close()
    })

    return {
        danmakuContainer,
        shootDanmaku
    }
}
