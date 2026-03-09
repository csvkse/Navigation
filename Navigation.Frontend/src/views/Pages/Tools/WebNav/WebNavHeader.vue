<template>
    <header class="relative pb-4 sm:pb-8 border-b border-border/5 transition-all duration-200">
        <div class="flex flex-col gap-4">
            <!-- Row 1: Branding & Clock -->
            <div class="flex items-start justify-between gap-4">
                <!-- Branding Section -->
                <div class="flex items-start gap-4 flex-shrink-0">
                    <!-- Vertical Title Area -->
                    <div class="flex gap-4 items-end">
                        <div class="flex flex-col">
                            <h1 class="text-3xl sm:text-5xl font-black tracking-tighter leading-[0.85] flex flex-col drop-shadow-sm select-none"
                                :class="layoutScheme === 'premium' ? 'bg-gradient-to-br from-foreground via-foreground/90 to-primary bg-clip-text text-transparent' : 'text-foreground'">
                                <span>{{ t('tools.webNav.title').slice(0, 2) }}</span>
                                <span>{{ t('tools.webNav.title').slice(2, 4) }}</span>
                            </h1>
                            <!-- Badges -->
                            <div class="mt-2 flex flex-wrap gap-1.5 items-center">
                                <code
                                    class="text-[10px] font-mono text-muted-foreground/40 bg-muted/20 px-1.5 py-0.5 rounded border border-border/10 max-w-[80px] truncate">
                                    {{ currentKey }}
                                </code>
                                <Badge v-if="currentUser" variant="outline"
                                    class="h-3.5 px-1 text-[9px] font-black uppercase tracking-widest border-primary/20 text-primary/60">
                                    @{{ currentUser.username }}
                                </Badge>
                            </div>
                        </div>

                        <!-- Back Button -->
                        <!-- <Button variant="ghost" size="icon" @click="handleBack"
                            class="h-8 w-8 mb-6 sm:mb-8 rounded-xl bg-muted/10 border border-border/10 group">
                            <ChevronLeft class="h-4 w-4 text-muted-foreground/60 group-hover:text-primary" />
                        </Button> -->

                        <!-- Auxiliary Info (Restored for Desktop) -->
                        <div
                            class="hidden xl:flex flex-col gap-1 border-l border-border/20 pl-6 h-fit self-end mb-6 ml-2">
                            <div class="text-[11px] font-black uppercase tracking-[0.4em] text-muted-foreground/30">
                                System Status
                            </div>
                            <div class="flex items-center gap-2">
                                <div class="w-1.5 h-1.5 rounded-full bg-emerald-500/50 animate-pulse"></div>
                                <span class="text-[12px] font-bold text-muted-foreground/60 tracking-tight">{{
                                    currentTime.date }}</span>
                            </div>
                            <div class="text-[11px] font-medium text-muted-foreground/40">{{ currentTime.day }}</div>
                        </div>
                    </div>
                </div>

                <!-- Clock Section -->
                <div class="relative flex flex-col items-end pointer-events-none select-none z-0 mt-1 sm:mt-0">
                    <div
                        class="text-4xl sm:text-8xl font-black tracking-tighter tabular-nums text-foreground/5 absolute -bottom-2 right-0 blur-2xl opacity-40">
                        {{ currentTime.time }}
                    </div>
                    <div
                        class="text-3xl sm:text-7xl font-black tracking-tighter tabular-nums text-foreground/90 relative z-10">
                        {{ currentTime.time }}
                    </div>
                </div>
            </div>

            <!-- Row 2: Search & Actions -->
            <div class="flex flex-row items-center gap-2 w-full lg:max-w-none relative z-20">
                <div
                    :class="['flex items-center flex-1 min-w-0 h-10 sm:h-12 bg-muted/10 dark:bg-muted/5 border border-border/20 dark:border-border/10 rounded-full transition-all focus-within:ring-4 focus-within:ring-primary/5 focus-within:border-primary/40 backdrop-blur-md group/search shadow-sm hover:shadow-md hover:bg-muted/20', isManageMode ? 'hidden sm:flex' : '']">

                    <Select v-model="selectedEngine">
                        <SelectTrigger
                            class="h-full border-none shadow-none bg-transparent hover:bg-transparent focus:ring-0 focus:outline-none focus:ring-offset-0 focus-visible:ring-0 focus-visible:outline-none focus-visible:border-none ring-0 px-0 w-[36px] sm:w-[42px] shrink-0 text-muted-foreground group-focus-within/search:text-foreground transition-all flex justify-center [&>svg]:hidden">
                            <div class="flex items-center justify-center">
                                <svg viewBox="0 0 24 24"
                                    class="h-4 w-4 sm:h-5 sm:w-5 fill-current transition-transform group-active/search:scale-95"
                                    v-html="currentSearchIcon">
                                </svg>
                            </div>
                        </SelectTrigger>
                        <SelectContent
                            class="min-w-[140px] p-1 rounded-xl border-border/10 backdrop-blur-xl bg-card/95 shadow-xl">
                            <SelectItem v-for="engine in searchEngines" :key="engine.id" :value="engine.id"
                                class="rounded-lg text-xs sm:text-sm focus:bg-muted/50 cursor-pointer py-2">
                                <div class="flex items-center gap-3">
                                    <svg viewBox="0 0 24 24" class="h-4 w-4 fill-current opacity-70"
                                        v-html="engine.icon"></svg>
                                    <span class="font-medium">{{ engine.name }}</span>
                                </div>
                            </SelectItem>
                        </SelectContent>
                    </Select>

                    <div class="w-px h-4 sm:h-5 bg-border/20 shrink-0 mx-0 sm:mx-1" />

                    <div class="relative flex-1 flex items-center h-full min-w-0">
                        <Input ref="searchInputRef" v-model="searchQuery" :placeholder="t('tools.webNav.search')"
                            @keyup.enter="handleSearch"
                            class="border-none shadow-none bg-transparent focus-visible:ring-0 h-full w-full text-sm sm:text-base px-3 pr-9 placeholder:text-muted-foreground/40 font-medium" />

                        <button v-if="searchQuery" type="button" @click="searchQuery = ''; searchInputRef?.focus()"
                            class="absolute right-2 p-1 rounded-full text-muted-foreground/40 hover:text-foreground hover:bg-muted/50 transition-all active:scale-90">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 24 24" fill="none"
                                stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M18 6L6 18M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                </div>

                <div
                    class="flex items-center gap-1 sm:gap-2 p-1 sm:p-1.5 bg-muted/20 rounded-xl border border-border/30 backdrop-blur-sm shrink-0">
                    <!-- Theme Switch -->
                    <Button @click="$emit('toggle-theme')" variant="ghost" size="icon"
                        class="h-8 w-8 rounded-lg hover:bg-background/50">
                        <Sun v-if="isDark" class="h-3.5 w-3.5" />
                        <Moon v-else class="h-3.5 w-3.5" />
                    </Button>

                    <!-- Read Only Badge -->
                    <div v-if="isReadOnly"
                        class="flex h-8 px-3 rounded-lg font-black text-[11px] uppercase tracking-wider bg-muted/40 text-muted-foreground items-center justify-center border border-border/10 cursor-not-allowed shrink-0">
                        <Shield class="sm:mr-1.5 h-3 w-3 opacity-50" />
                        <span class="hidden sm:inline">{{ t('tools.webNav.readOnly') }}</span>
                    </div>

                    <!-- Manage Mode -->
                    <!-- Manage Mode Controls -->
                    <template v-if="isManageMode">
                        <!-- Mode Switcher -->
                        <div
                            class="hidden sm:flex items-center p-0.5 bg-muted/40 rounded-lg border border-border/20 backdrop-blur-sm mr-1">
                            <button @click="$emit('update:manage-tab', 'drag')"
                                :class="['h-7 px-2 sm:px-3 rounded-md flex items-center justify-center transition-all', manageTab === 'drag' ? 'bg-background shadow-sm text-foreground ring-1 ring-border/10' : 'text-muted-foreground hover:text-foreground/80']">
                                <LayoutGrid class="h-3.5 w-3.5" />
                            </button>
                            <button @click="$emit('update:manage-tab', 'data')"
                                :class="['h-7 px-2 sm:px-3 rounded-md flex items-center justify-center transition-all', manageTab === 'data' ? 'bg-background shadow-sm text-foreground ring-1 ring-border/10' : 'text-muted-foreground hover:text-foreground/80']">
                                <List class="h-3.5 w-3.5" />
                            </button>
                        </div>

                        <!-- Exit Button -->
                        <button @click="$emit('toggle-manage')"
                            class="flex h-8 px-2 sm:px-4 rounded-lg font-black text-[11px] uppercase tracking-wider bg-destructive text-destructive-foreground hover:bg-destructive/90 transition-all items-center justify-center shadow-lg shadow-destructive/20 active:scale-95 shrink-0">
                            <span class="hidden sm:inline">{{ t('tools.webNav.exitManage') }}</span>
                            <span class="sm:hidden">Exit</span>
                        </button>
                    </template>

                    <!-- Enter Manage Mode -->
                    <button v-else-if="!isReadOnly" @click="$emit('toggle-manage')"
                        class="flex h-8 px-2 sm:px-4 rounded-lg transition-all font-black text-[11px] uppercase tracking-wider items-center justify-center hover:bg-primary/10 hover:text-primary">
                        <Settings class="sm:mr-1.5 h-3 w-3" />
                        <span class="hidden sm:inline">{{ t('tools.webNav.manageMode') }}</span>
                        <span class="sm:hidden ml-1">M</span>
                    </button>

                    <!-- Add Button -->
                    <button v-if="isManageMode" @click="$emit('open-modal')"
                        class="flex h-8 px-3 sm:px-5 rounded-lg font-black text-[11px] uppercase tracking-wider bg-purple-600 text-white hover:bg-purple-500 transition-all items-center justify-center shadow-lg shadow-purple-600/20 active:scale-95 shrink-0">
                        <Plus class="h-3.5 w-3.5 mr-1 sm:mr-1.5" />
                        <span class="hidden sm:inline">{{ t('tools.webNav.addLink') }}</span>
                        <span class="sm:hidden">A</span>
                    </button>

                    <div class="w-px h-5 bg-border/20 mx-0.5"></div>

                    <!-- User Popover -->
                    <Popover>
                        <PopoverTrigger as-child>
                            <Button variant="ghost" size="icon" class="h-8 w-8 rounded-lg relative group">
                                <User class="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary" />
                                <div v-if="currentUser"
                                    class="absolute top-2 right-2 w-1 h-1 bg-emerald-500 rounded-full" />
                            </Button>
                        </PopoverTrigger>
                        <PopoverContent align="end"
                            class="w-64 p-3 rounded-2xl border-border/20 shadow-2xl backdrop-blur-xl bg-card/90">
                            <template v-if="currentUser">
                                <div class="flex items-center gap-3 p-2 mb-2 bg-muted/40 rounded-xl">
                                    <div
                                        class="h-8 w-8 rounded-full bg-primary/20 flex items-center justify-center font-black text-primary text-xs uppercase">
                                        {{ currentUser.username[0] }}
                                    </div>
                                    <div class="flex flex-col min-w-0">
                                        <span class="text-sm font-black truncate">{{ currentUser.username
                                        }}</span>
                                        <span
                                            class="text-[11px] text-muted-foreground/60 font-mono tracking-tighter truncate">{{
                                                currentUser.dataKey }}</span>
                                    </div>
                                </div>
                                <div class="grid gap-1">
                                    <Button variant="ghost"
                                        class="w-full justify-start text-[11px] font-bold h-9 rounded-lg"
                                        @click="$emit('open-auth', 'profile')">
                                        <Settings class="mr-3 h-3.5 w-3.5 opacity-50" /> {{
                                            t('tools.webNav.auth.profile') }}
                                    </Button>
                                    <Button variant="ghost"
                                        class="w-full justify-start text-[11px] font-bold h-9 rounded-lg"
                                        @click="handleCopyReadOnly">
                                        <Copy class="mr-3 h-3.5 w-3.5 opacity-50" />
                                        {{ t('tools.webNav.shareReadOnly') }}
                                    </Button>
                                    <Button variant="ghost"
                                        class="w-full justify-start text-[11px] font-bold h-9 rounded-lg text-destructive hover:bg-destructive/10"
                                        @click="$emit('logout')">
                                        <LogOut class="mr-3 h-3.5 w-3.5 opacity-50" /> {{
                                            t('tools.webNav.auth.logout') }}
                                    </Button>
                                </div>
                            </template>
                            <template v-else>
                                <div class="grid gap-2">
                                    <Button variant="outline" class="w-full h-10 rounded-xl font-bold"
                                        @click="$emit('open-auth', 'login')">
                                        <LogIn class="mr-3 h-4 w-4 opacity-50" /> {{
                                            t('tools.webNav.auth.login') }}
                                    </Button>
                                    <Button variant="secondary" class="w-full h-10 rounded-xl font-bold"
                                        @click="$emit('open-auth', 'register')">
                                        <UserPlus class="mr-3 h-4 w-4 opacity-50" /> {{
                                            t('tools.webNav.auth.register') }}
                                    </Button>
                                </div>
                            </template>
                        </PopoverContent>
                    </Popover>

                    <!-- Appearance Settings -->
                    <Popover>
                        <PopoverTrigger as-child>
                            <Button variant="ghost" size="icon" class="h-8 w-8 rounded-lg group">
                                <Layers class="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary" />
                            </Button>
                        </PopoverTrigger>
                        <PopoverContent align="end"
                            class="w-80 p-5 rounded-[2rem] border-border/20 shadow-2xl backdrop-blur-xl bg-card/90">
                            <div class="space-y-6">
                                <div class="flex items-center justify-between px-2">
                                    <h4 class="font-black text-xs uppercase tracking-widest text-muted-foreground/60">
                                        {{ t('tools.webNav.displaySettings') }}</h4>
                                </div>

                                <!-- Theme Mode Toggles -->
                                <div class="grid gap-4">
                                    <!-- Light Mode Themes (Only show if !isDark) -->
                                    <div v-if="!isDark" class="space-y-2">
                                        <div
                                            class="flex items-center gap-2 text-[11px] font-bold text-muted-foreground/80 px-2">
                                            <Sun class="h-3.5 w-3.5 text-amber-500" />
                                            {{ t('tools.webNav.dayTheme') || 'Day Theme' }}
                                        </div>
                                        <div class="grid grid-cols-2 gap-2">
                                            <button v-for="scheme in schemes" :key="'day-' + scheme.id"
                                                @click="$emit('update:dayScheme', scheme.id)"
                                                class="flex items-center gap-2 p-2 rounded-xl transition-all border text-xs font-medium"
                                                :class="dayScheme === scheme.id ? 'bg-amber-500/10 border-amber-500/50 text-amber-600 dark:text-amber-400' : 'border-transparent hover:bg-muted/50 text-muted-foreground'">
                                                <component :is="scheme.icon" class="h-3 w-3" />
                                                {{ scheme.name }}
                                            </button>
                                        </div>
                                    </div>

                                    <!-- Dark Mode Themes (Only show if isDark) -->
                                    <div v-if="isDark" class="space-y-2">
                                        <div
                                            class="flex items-center gap-2 text-[11px] font-bold text-muted-foreground/80 px-2">
                                            <Moon class="h-3.5 w-3.5 text-indigo-500" />
                                            {{ t('tools.webNav.nightTheme') || 'Night Theme' }}
                                        </div>
                                        <div class="grid grid-cols-2 gap-2">
                                            <button v-for="scheme in schemes" :key="'night-' + scheme.id"
                                                @click="$emit('update:nightScheme', scheme.id)"
                                                class="flex items-center gap-2 p-2 rounded-xl transition-all border text-xs font-medium"
                                                :class="nightScheme === scheme.id ? 'bg-indigo-500/10 border-indigo-500/50 text-indigo-400 dark:text-indigo-300' : 'border-transparent hover:bg-muted/50 text-muted-foreground'">
                                                <component :is="scheme.icon" class="h-3 w-3" />
                                                {{ scheme.name }}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </PopoverContent>
                    </Popover>

                    <!-- User Keys Switcher -->
                    <Popover v-if="currentUser">
                        <PopoverTrigger as-child>
                            <Button variant="ghost" size="icon" class="h-8 w-8 rounded-lg group">
                                <Book class="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary" />
                            </Button>
                        </PopoverTrigger>
                        <PopoverContent align="end"
                            class="w-72 p-4 rounded-[1.5rem] border-border/20 shadow-2xl backdrop-blur-xl bg-card/90">
                            <div class="space-y-4">
                                <h4 class="font-black text-xs uppercase tracking-widest text-muted-foreground/60 px-2">
                                    {{ t('tools.webNav.myBookmarks') }}</h4>

                                <div class="grid gap-2 max-h-[200px] overflow-y-auto custom-scrollbar">
                                    <button v-for="k in userKeys" :key="k.key" @click="$emit('select-user-key', k.key)"
                                        class="flex items-center justify-between p-2.5 rounded-xl transition-all border border-transparent group/item"
                                        :class="activeUserKey === k.key ? 'bg-primary/10 border-primary/20 text-primary' : 'hover:bg-muted/50'">
                                        <div class="flex items-center gap-3 min-w-0">
                                            <div class="w-1.5 h-1.5 rounded-full"
                                                :class="activeUserKey === k.key ? 'bg-primary' : 'bg-muted-foreground/30'" />
                                            <span class="text-xs font-bold truncate">{{ k.name }}</span>
                                        </div>
                                        <Button v-if="userKeys && userKeys.length > 1 && k.key !== currentUser.dataKey"
                                            variant="ghost" size="icon"
                                            class="h-6 w-6 opacity-0 group-hover/item:opacity-100 -mr-1 hover:bg-destructive/10 hover:text-destructive"
                                            @click.stop="$emit('delete-user-key', k.key)">
                                            <Trash2 class="h-3 w-3" />
                                        </Button>
                                    </button>
                                </div>

                                <div class="pt-2 border-t border-border/10 flex gap-2">
                                    <Input v-model="newKeyName" :placeholder="t('tools.webNav.newCollectionName')"
                                        @keyup.enter="createNewKey"
                                        class="h-9 text-xs rounded-lg bg-muted/40 border-none" />
                                    <Button size="icon" class="h-9 w-9 rounded-lg shrink-0" @click="createNewKey">
                                        <Plus class="h-4 w-4" />
                                    </Button>
                                </div>
                            </div>
                        </PopoverContent>
                    </Popover>

                    <!-- Key Popover -->
                    <Popover>
                        <PopoverTrigger as-child>
                            <Button variant="ghost" size="icon" class="h-8 w-8 rounded-lg group">
                                <Key class="h-3.5 w-3.5 text-muted-foreground group-hover:text-primary" />
                            </Button>
                        </PopoverTrigger>
                        <PopoverContent align="end"
                            class="w-80 p-5 rounded-[2rem] border-border/20 shadow-2xl backdrop-blur-xl bg-card/90">
                            <div class="grid gap-6">
                                <div class="space-y-1">
                                    <h4 class="font-black tracking-tight text-sm uppercase">{{
                                        t('tools.webNav.switchKey') }}</h4>
                                    <p class="text-[10px] text-muted-foreground leading-snug">{{
                                        t('tools.webNav.enterCustomKey') }}</p>
                                </div>
                                <div class="flex gap-2">
                                    <Input v-model="keyInput" :placeholder="t('tools.webNav.enterCustomKey')"
                                        @keyup.enter="$emit('switch-key', keyInput)"
                                        class="h-11 rounded-xl bg-muted/40 border-none px-4 text-sm focus-visible:ring-primary/20" />
                                    <Button size="icon" @click="$emit('switch-key', keyInput)"
                                        class="rounded-xl h-11 w-11 shadow-lg shrink-0">{{ t('tools.webNav.ok')
                                        }}</Button>
                                </div>
                                <div class="grid gap-2 border-t border-border/10 pt-4">
                                    <Button variant="outline" size="sm"
                                        class="w-full h-10 rounded-xl font-bold text-[11px] uppercase tracking-wider"
                                        @click="$emit('bulk-export')">
                                        <Download class="mr-2 h-3.5 w-3.5" /> {{
                                            t('tools.webNav.bulkAction') || 'Bulk Actions' }}
                                    </Button>
                                    <Button v-if="isManageMode" variant="ghost" size="sm"
                                        class="w-full h-10 rounded-xl font-bold text-[11px] uppercase tracking-wider text-destructive hover:bg-destructive/10"
                                        @click="$emit('clear-all')">
                                        <Trash2 class="mr-2 h-3.5 w-3.5" /> {{ t('tools.webNav.clearAll') }}
                                    </Button>
                                </div>
                            </div>
                        </PopoverContent>
                    </Popover>
                </div>
            </div>
        </div>
    </header>
</template>



<script setup lang="ts">
import { ref, watch, onMounted, nextTick, onUnmounted, computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
} from '@/components/ui/select'
import {
    Sun, Moon, Settings, Plus, User, LogIn, LogOut, UserPlus,
    Layers, Key, Download, Trash2, Book, Shield, Copy, LayoutGrid, List
} from 'lucide-vue-next'

import type { WebNavTheme } from '@/config/webNav/webNavTheme'

const { t } = useI18n()

interface Props {
    currentKey: string
    currentUser: any
    currentTime: { date: string; time: string; day: string }
    searchQuery: string
    isDark: boolean
    isManageMode: boolean
    layoutScheme: string
    dayScheme: string
    nightScheme: string
    schemes: WebNavTheme[]
    userKeys?: Array<{ name: string; key: string; readOnlyKey?: string }>
    activeUserKey?: string
    isReadOnly?: boolean
    manageTab?: 'drag' | 'data'
}

const props = defineProps<Props>()

const handleCopyReadOnly = () => {
    emit('copy-read-only')
}

const keyInput = ref('')
const newKeyName = ref('')
const searchQuery = ref(props.searchQuery)
const searchInputRef = ref<any>(null)

const createNewKey = () => {
    if (!newKeyName.value.trim()) return
    emit('create-user-key', newKeyName.value.trim())
    newKeyName.value = ''
}
// --- Search Engine Logic ---
const searchEngines = [
    {
        id: 'google',
        name: 'Google',
        url: 'https://www.google.com/search?q=',
        icon: '<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-1-13h2v6h-2zm0 8h2v2h-2z" transform="scale(0.04)"/><path d="M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .533 5.347.533 12S5.867 24 12.48 24c3.52 0 6.1-1.16 8.16-3.293 2.093-2.093 2.733-5.027 2.733-7.4 0-.733-.067-1.44-.187-2.133H12.48z"/>'
    },
    {
        id: 'baidu',
        name: 'Baidu',
        url: 'https://www.baidu.com/s?wd=',
        icon: '<path d="M11.96 1.706a5.867 5.867 0 0 0-.256-1.152L11.533 0h1.195c.768 0 1.259.085 1.472.256.213.15.352.427.416.832l.096.544a8.68 8.68 0 0 1-.501 2.336 6.13 6.13 0 0 1-1.035 2.08 7.6 7.6 0 0 1-1.632 1.632c-.672.48-1.557.885-2.656 1.216a.434.434 0 0 1-.299.01.597.597 0 0 1-.245-.16l-3.328-3.328a.56.56 0 0 1-.16-.363c0-.128.053-.235.16-.32l.48-.384c.32-.235.619-.352.896-.352a1.2 1.2 0 0 1 .683.245l1.045.757.299-1.387a6.953 6.953 0 0 1 1.259-2.848 5.764 5.764 0 0 1 2.677-1.728zM4.322 8.683c.405 0 .8.085 1.184.256.384.15.683.405.896.768l2.976 5.568.128.224c.085.15.128.299.128.448 0 .427-.203.736-.608.928l-.352.16c-.15.064-.299.096-.448.096-.32 0-.619-.117-.896-.352l-2.08-1.76-2.016 1.248a1.234 1.234 0 0 1-.683.192c-.299 0-.555-.107-.768-.32a.965.965 0 0 1-.32-.736c0-.128.021-.267.064-.416l1.248-3.936a2.26 2.26 0 0 1 1.547-1.568zm11.755.352c.235 0 .459.032.672.096 1.621.469 2.805 1.344 3.552 2.624.512.832.768 2.005.768 3.52 0 2.453-.789 4.384-2.368 5.792-1.579 1.408-3.563 2.112-5.952 2.112-1.92 0-3.648-.501-5.184-1.504L6.178 22.993a1.05 1.05 0 0 1-.544.16.89.89 0 0 1-.64-.256.89.89 0 0 1-.256-.64c0-.128.021-.256.064-.384l1.376-4.576a8.948 8.948 0 0 1-1.376-4.608c0-1.856.715-3.392 2.144-4.608 1.429-1.216 3.232-1.824 5.408-1.824 1.301 0 2.528.224 3.68.672zm-2.091 10.347c1.365 0 2.421-.363 3.168-1.088.747-.725 1.12-1.867 1.12-3.424 0-1.152-.352-2.101-1.056-2.848-.704-.747-1.749-1.12-3.136-1.12-2.347 0-3.52 1.323-3.52 3.968 0 1.515.363 2.688 1.088 3.52.725.832 1.515 1.248 2.368 1.248l-.053-.245z"/>'
    },
    {
        id: 'bing',
        name: 'Bing',
        url: 'https://www.bing.com/search?q=',
        icon: '<path d="M12.96 4.332v12.446l-5.65 2.809V4.689l5.65-.357zm2.4 1.77L21.46 10l-6.1 2.3v-6.2zM4.77 2.332l9.7 1.9 8.2 5.3-8.2 2.9V23L2.27 16V5l2.5-2.668z"/>'
    },
    {
        id: 'yahoo',
        name: 'Yahoo',
        url: 'https://search.yahoo.com/search?p=',
        icon: '<path d="M16 4l-4 9-4-9H5l5 11v6h4v-6l5-11h-3z"/>'
    },
    {
        id: 'duckduckgo',
        name: 'DDG',
        url: 'https://duckduckgo.com/?q=',
        icon: '<path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm6.2 13.9a5.3 5.3 0 0 1-3.6 1.4 5.9 5.9 0 0 1-3.7-1.8 17.5 17.5 0 0 1-3.8.4 5.4 5.4 0 0 1-1.3-.2c-1.3-.4-1.6-1.6-.8-2.6.2-.2 1.9-2.1 3-2.5-.2-.8-.4-1.6-.4-1.8a2.9 2.9 0 0 1 .4-1.8 1.9 1.9 0 0 1 1.7-.8c2.1.2 2.7 2.8 2.9 3.5.6.8 1.5 1.2 2.3.9h.2c.4 0 .7.1 1 .3.4.3.5.7.5 1.2a6 6 0 0 1-1.6 3.2 5.5 5.5 0 0 1 3.2.6z"/>'
    },
    {
        id: 'yandex',
        name: 'Yandex',
        url: 'https://yandex.com/search/?text=',
        icon: '<path d="M11 20H6v-2h2.2c1.3 0 1.8-.7 1.4-1.9L4 2h3l3.4 9.4L13.8 2H17l-4.7 13.3c-.5 1.5 0 2.2 1.3 2.2H14v2h-3v-.5z"/>'
    },
]

const selectedEngine = ref('google')

const currentSearchIcon = computed(() => {
    const engine = searchEngines.find(e => e.id === selectedEngine.value)
    return engine?.icon || searchEngines[0]?.icon || ''
})

const focusInput = () => {
    nextTick(() => {
        if (searchInputRef.value) {
            // Try standard focus
            if (typeof searchInputRef.value.focus === 'function') {
                searchInputRef.value.focus()
            } else if (searchInputRef.value?.$el?.querySelector) {
                // If it's a wrapper, find input
                const input = searchInputRef.value.$el.querySelector('input') || searchInputRef.value.$el
                input?.focus()
            } else if (searchInputRef.value instanceof HTMLElement) {
                searchInputRef.value.focus()
            }
        }
    })
}

const handleVisibilityChange = () => {
    if (document.visibilityState === 'visible') {
        focusInput()
    }
}

onMounted(() => {
    const saved = localStorage.getItem('webnav_search_engine')
    if (saved && searchEngines.some(e => e.id === saved)) {
        selectedEngine.value = saved
    }

    focusInput()
    document.addEventListener('visibilitychange', handleVisibilityChange)
    window.addEventListener('focus', focusInput)
})

onUnmounted(() => {
    document.removeEventListener('visibilitychange', handleVisibilityChange)
    window.removeEventListener('focus', focusInput)
})

watch(selectedEngine, (val) => {
    localStorage.setItem('webnav_search_engine', val)
})

const handleSearch = () => {
    if (!searchQuery.value) return
    const engine = searchEngines.find(e => e.id === selectedEngine.value)
    if (engine) {
        window.open(engine.url + encodeURIComponent(searchQuery.value), '_blank')
    }
}

watch(() => props.searchQuery, (newVal) => {
    searchQuery.value = newVal
})

const emit = defineEmits<{
    'update:searchQuery': [value: string]
    'toggle-theme': []
    'toggle-manage': []
    'open-modal': []
    'open-auth': [mode: string]
    'logout': []
    'update:layoutScheme': [value: string]
    'update:dayScheme': [value: string]
    'update:nightScheme': [value: string]
    'switch-key': [key: string]
    'bulk-export': []
    'clear-all': []
    'create-user-key': [name: string]
    'select-user-key': [key: string]
    'delete-user-key': [key: string]
    'update:manage-tab': [value: 'drag' | 'data']
    'copy-read-only': []
}>()

watch(searchQuery, (newVal) => {
    emit('update:searchQuery', newVal)
})
</script>
