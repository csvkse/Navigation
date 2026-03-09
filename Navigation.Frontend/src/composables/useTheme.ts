import { ref, watch } from 'vue'
import { useAppStore } from '@/stores/appStore'

// Global state to share across components
const isDark = ref<boolean>(false)
let isInitialized = false

// Pre-initialize state from localStorage to ensure 0-delay dark mode matching index.html
if (typeof window !== 'undefined') {
  try {
    const raw = localStorage.getItem('theme-raw')
    if (raw === 'dark') isDark.value = true
    else if (raw === 'light') isDark.value = false
    else if (window.matchMedia) {
      isDark.value = window.matchMedia('(prefers-color-scheme: dark)').matches
    }
  } catch (e) {
    // Ignore errors during pre-initialization
  }
}

export const useTheme = () => {
  const gameStore = useAppStore()

  // Apply theme to DOM
  const applyTheme = () => {
    if (typeof window === 'undefined' || typeof document === 'undefined') return
    try {
      if (isDark.value) {
        document.documentElement.classList.add('dark')
        localStorage.setItem('theme-raw', 'dark')
      } else {
        document.documentElement.classList.remove('dark')
        localStorage.setItem('theme-raw', 'light')
      }
    } catch (e) {
      // Ignore DOM interaction errors
    }
  }

  // Initialize theme immediately
  const initTheme = () => {
    if (isInitialized) return
    isInitialized = true

    try {
      if (!gameStore.isDark) {
        // First visit or no store state, sync local state to store
        gameStore.isDark = isDark.value ? 'dark' : 'light'
      } else {
        // Store has state, respect it (store is source of truth)
        const savedTheme = gameStore.isDark
        isDark.value = savedTheme === 'dark'
      }
      applyTheme()
    } catch (e) {
      console.warn('Theme initialization issue:', e)
      // Fallback: just apply what we have locally
      applyTheme()
    }
  }

  // Execute initialization immediately
  initTheme()

  // Watch for changes
  watch(isDark, () => {
    applyTheme()
    try {
      // Sync back to store
      gameStore.isDark = isDark.value ? 'dark' : 'light'
    } catch (e) {
      // Ignore store write errors
    }
  })

  // Toggle theme function
  const toggleTheme = () => {
    isDark.value = !isDark.value
  }

  return {
    isDark,
    toggleTheme
  }
}
