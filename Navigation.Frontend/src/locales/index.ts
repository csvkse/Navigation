import zhCN from './zh-CN'
import en from './en'

export type Locale = 'zh-CN' | 'en'

export const locales = { 'zh-CN': zhCN, en }

export const localeNames: Record<Locale, string> = {
  'zh-CN': '简体中文',
  en: 'English'
}

/**
 * 根据浏览器语言检测并返回应用支持的语言
 * @returns 检测到的语言代码
 */
export const detectBrowserLocale = (): Locale => {
  // 获取浏览器语言
  const browserLang = navigator.language || (navigator.languages && navigator.languages[0]) || 'zh-CN'
  const lang = browserLang.toLowerCase()

  // 映射浏览器语言到应用支持的语言
  if (lang.startsWith('zh')) {
    return 'zh-CN'
  } else if (lang.startsWith('en')) {
    return 'en'
  }

  // 默认返回简体中文
  return 'zh-CN'
}

export type TranslationSchema = typeof zhCN
