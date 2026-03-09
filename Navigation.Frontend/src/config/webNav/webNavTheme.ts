import { Layers, Globe, Activity, Box, Zap, Coffee, Moon } from 'lucide-vue-next'

export interface WebNavTheme {
    id: string
    name: string
    icon: any
    // CSS classes applied to the root container
    containerClass: string
    // Whether this theme supports specific background effects
    hasBackgroundEffect: boolean
}

export const WEBNAV_THEMES: WebNavTheme[] = [
    {
        id: 'midnight-slate',
        name: 'Midnight Slate',
        icon: Moon,
        containerClass: 'theme-midnight-slate',
        hasBackgroundEffect: true
    },
    {
        id: 'premium',
        name: 'Premium Glass',
        icon: Layers,
        containerClass: 'theme-premium',
        hasBackgroundEffect: true
    },
    {
        id: 'minimal',
        name: 'Ultra Pure',
        icon: Globe,
        containerClass: 'theme-minimal',
        hasBackgroundEffect: false
    },
    {
        id: 'cyber',
        name: 'Cyberpunk',
        icon: Activity,
        containerClass: 'theme-cyber font-mono tracking-tight',
        hasBackgroundEffect: true
    },
    {
        id: 'retro',
        name: 'Neo-Retro',
        icon: Box,
        containerClass: 'theme-retro font-serif',
        hasBackgroundEffect: false
    },
    {
        id: 'forest',
        name: 'Zen Forest',
        icon: Coffee,
        containerClass: 'theme-forest',
        hasBackgroundEffect: true
    },
    {
        id: 'neon',
        name: 'Neon Nights',
        icon: Zap,
        containerClass: 'theme-neon font-bold tracking-wide',
        hasBackgroundEffect: true
    }
]

export const DEFAULT_THEME_ID = 'premium'
export const DEFAULT_THEME_ID_Night = 'neon'
