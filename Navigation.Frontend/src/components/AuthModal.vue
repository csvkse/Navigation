<template>
    <Dialog :open="open" @update:open="$emit('update:open', $event)">
        <DialogContent class="sm:max-w-[400px] z-[100]">
            <DialogHeader>
                <DialogTitle>{{ mode === 'profile' ? t('tools.webNav.auth.profile') : (mode === 'login' ?
                    t('tools.webNav.auth.login') : t('tools.webNav.auth.register')) }}</DialogTitle>
            </DialogHeader>

            <!-- Login / Register Mode -->
            <div v-if="mode !== 'profile'" class="space-y-4 py-4">
                <div class="grid gap-2">
                    <Label>{{ t('tools.webNav.auth.username') }}</Label>
                    <Input v-model="form.username" :placeholder="t('tools.webNav.auth.username')" />
                </div>

                <div class="grid gap-2">
                    <Label>{{ t('tools.webNav.auth.password') }}</Label>
                    <Input type="password" v-model="form.password" :placeholder="t('tools.webNav.auth.password')" />
                </div>
            </div>

            <!-- Profile Mode (Tabbed) -->
            <div v-else class="py-2">
                <!-- Custom Tabs -->
                <div class="flex items-center gap-1 bg-muted/50 p-1 rounded-lg mb-4">
                    <button v-for="tab in tabs" :key="tab.value" @click="activeTab = tab.value"
                        :class="['flex-1 px-3 py-1.5 text-sm font-medium rounded-md transition-all',
                            activeTab === tab.value ? 'bg-background shadow-sm text-foreground' : 'text-muted-foreground hover:text-foreground']">
                        {{ tab.label }}
                    </button>
                </div>

                <div class="space-y-4">
                    <!-- Username Tab -->
                    <div v-if="activeTab === 'username'" class="space-y-4">
                        <div class="grid gap-2">
                            <Label>{{ t('tools.webNav.auth.username') }}</Label>
                            <Input v-model="form.username" />
                            <p class="text-xs text-muted-foreground">{{ t('tools.webNav.auth.usernameTip') ||
                                'Uniqueidentifier for your collection' }}</p>
                        </div>
                    </div>

                    <!-- Password Tab -->
                    <div v-else class="space-y-4">
                        <div class="grid gap-2">
                            <Label>{{ t('tools.webNav.auth.newPassword') }}</Label>
                            <Input type="password" v-model="form.password"
                                :placeholder="t('tools.webNav.auth.newPassword')" />
                        </div>
                        <div class="grid gap-2">
                            <Label>{{ t('tools.webNav.auth.confirmPassword') || 'Confirm Password' }}</Label>
                            <Input type="password" v-model="confirmPassword"
                                :placeholder="t('tools.webNav.auth.confirmPassword') || 'Confirm Password'" />
                        </div>
                    </div>

                    <!-- Common Old Password Field -->
                    <div class="grid gap-2 pt-2 border-t border-border/50">
                        <Label>{{ t('tools.webNav.auth.oldPassword') || 'Current Password' }} <span
                                class="text-destructive">*</span></Label>
                        <Input type="password" v-model="form.oldPassword"
                            :placeholder="t('tools.webNav.auth.oldPassword') || 'Required to save changes'" />
                    </div>
                </div>
            </div>

            <DialogFooter class="flex flex-col sm:flex-row gap-2">
                <Button v-if="mode === 'login'" @click="$emit('login')" :disabled="loading" class="w-full">
                    <Loader2 v-if="loading" class="mr-2 h-4 w-4 animate-spin" />
                    {{ t('tools.webNav.auth.login') }}
                </Button>
                <Button v-else-if="mode === 'register'" @click="$emit('register')" :disabled="loading" class="w-full">
                    <Loader2 v-if="loading" class="mr-2 h-4 w-4 animate-spin" />
                    {{ t('tools.webNav.auth.register') }}
                </Button>
                <Button v-else @click="handleUpdate" :disabled="loading" class="w-full">
                    <Loader2 v-if="loading" class="mr-2 h-4 w-4 animate-spin" />
                    {{ t('common.save') }}
                </Button>
            </DialogFooter>

            <div v-if="mode !== 'profile'" class="text-center text-xs text-muted-foreground mt-2">
                <button v-if="mode === 'login'" @click="$emit('update:mode', 'register')"
                    class="hover:underline text-primary">{{
                        t('tools.webNav.auth.register') }}</button>
                <button v-else @click="$emit('update:mode', 'login')" class="hover:underline text-primary">{{
                    t('tools.webNav.auth.login') }}</button>
            </div>
        </DialogContent>
    </Dialog>
</template>



<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useI18n } from '@/composables/useI18n'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Loader2 } from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const { t } = useI18n()

interface AuthForm {
    username: string
    password: string
    oldPassword?: string
}

interface Props {
    open: boolean
    mode: 'login' | 'register' | 'profile'
    form: AuthForm
    loading: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
    'update:open': [value: boolean]
    'update:mode': [value: 'login' | 'register' | 'profile']
    'login': []
    'register': []
    'update-profile': []
}>()

const activeTab = ref('username')
const confirmPassword = ref('')

const tabs = computed(() => [
    { value: 'username', label: t('tools.webNav.auth.username') || 'Profile' },
    { value: 'password', label: t('tools.webNav.auth.password') || 'Security' }
])

// Reset tab and fields when modal opens
watch(() => props.open, (val) => {
    if (val) {
        activeTab.value = 'username'
        confirmPassword.value = ''
    }
})

const handleUpdate = () => {
    if (activeTab.value === 'username') {
        // Clearing password so backend doesn't try to update it
        props.form.password = ''
    } else {
        // Password Tab Validation
        if (!props.form.password) {
            toast.error(t('tools.webNav.auth.passwordEmpty') || 'New password is required')
            return
        }
        if (props.form.password !== confirmPassword.value) {
            toast.error(t('tools.webNav.auth.passwordMismatch') || 'Passwords do not match')
            return
        }
    }

    emit('update-profile')
}
</script>
