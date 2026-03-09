import { defineStore } from 'pinia'

import type { Locale } from '@/locales'
import pkg from '../../package.json'
import { encryptData, decryptData } from '@/utils/crypto'

export const useAppStore = defineStore('app', {
  state: () => ({
    currentFingerId: '',
    isDark: '',
    locale: 'zh-CN' as Locale,
    sidebarCollapsed: typeof window !== 'undefined' ? (window.innerWidth < 1024 ? false : true) : true,
    globalLoading: false,
  }),
  getters: {
  },
  persist: {
    key: pkg.name,
    storage: typeof window !== 'undefined' ? localStorage : undefined,
    serializer: {
      serialize: state => encryptData(state),
      deserialize: value => decryptData(value)
    }
  }
})
