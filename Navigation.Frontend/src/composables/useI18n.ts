import { computed } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { locales, type Locale } from '@/locales'

export const useI18n = () => {
  const gameStore = useAppStore()

  const currentLocale = computed(() => gameStore.locale)

  const messages = computed(() => locales[currentLocale.value])

  // 获取翻译文本的辅助函数
  const t = (key: string): string => {
    const keys = key.split('.')
    let value: any = messages.value

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k]
      } else {
        return key // 如果找不到翻译，返回原始 key
      }
    }

    return typeof value === 'string' ? value : key
  }

  const setLocale = (locale: Locale) => {
    gameStore.locale = locale
  }

  return {
    t,
    locale: currentLocale,
    setLocale,
    messages
  }
}
