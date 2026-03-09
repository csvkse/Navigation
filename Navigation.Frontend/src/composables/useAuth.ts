import { ref } from 'vue'
import { toast } from 'vue-sonner'
import CryptoJS from 'crypto-js'

export function useAuth(t: (key: string) => string) {
    const BASE_URL = import.meta.env.VITE_API_BASE_URL || ''
    const STATIC_SALT = 'webnav_an_v1_920930589'

    // State
    const currentUser = ref<any>(null)
    const auth = ref({
        open: false,
        mode: 'login' as 'login' | 'register' | 'profile',
        loading: false,
        form: { username: '', password: '', oldPassword: '' }
    })

    // Helpers
    const getAuthKey = (username: string) => CryptoJS.SHA256(`auth:${username.toLowerCase()}:${STATIC_SALT}`).toString()
    const hashPassword = (password: string) => CryptoJS.SHA256(`pwd:${password}:${STATIC_SALT}`).toString()
    const generateSecureKey = (username: string) => {
        const random = Math.random().toString(36).substring(2) + Math.random().toString(36).substring(2)
        const timestamp = Date.now().toString()
        return CryptoJS.SHA256(`${username}:${STATIC_SALT}:${random}:${timestamp}`).toString()
    }

    const safeParse = (content: any): any => {
        if (!content) return {}
        if (typeof content !== 'string') return content
        try {
            let parsed = JSON.parse(content)
            let depth = 0
            while (typeof parsed === 'string' && depth < 3) {
                parsed = JSON.parse(parsed)
                depth++
            }
            return parsed
        } catch (e) {
            return { _raw: content, _error: true }
        }
    }

    // Actions
    const openAuthModal = (mode: string = 'login') => {
        auth.value.mode = mode as any

        // Reset form for profile mode to ensure checks work correctly
        if (mode === 'profile' && currentUser.value) {
            auth.value.form = {
                username: currentUser.value.username,
                password: '',
                oldPassword: ''
            }
        } else if (mode !== 'profile') {
            auth.value.form = { username: '', password: '', oldPassword: '' }
        }

        auth.value.open = true
    }

    const handleLogin = async (onSuccess: () => void) => {
        if (!auth.value.form.username || !auth.value.form.password) return
        auth.value.loading = true
        try {
            const userAuthKey = getAuthKey(auth.value.form.username)
            const res = await fetch(`${BASE_URL}/FastDB?key=${userAuthKey}`)
            const results = await res.json()
            const userRecord = results[0]

            if (!userRecord) {
                toast.error(t('tools.webNav.auth.invalidAuth'))
                return
            }

            const userData = safeParse(userRecord.content)
            const inputPwdHash = hashPassword(auth.value.form.password)

            if (userData.passwordHash === inputPwdHash) {
                const sessionUser = {
                    ...userData,
                    username: userData.username,
                    dataKey: userData.dataKey,
                    dbId: userRecord.id,
                    authKey: userAuthKey,
                    passwordHash: userData.passwordHash,
                    bookmarkKeys: userData.bookmarkKeys || [{ name: 'Default', key: userData.dataKey }],
                    messageKeys: userData.messageKeys || [{ name: 'Default', key: userData.dataKey }]
                }

                // Ensure messageKeys exist if not present (migration)
                if (!sessionUser.messageKeys) {
                    sessionUser.messageKeys = [{ name: 'Default', key: sessionUser.dataKey }]
                }

                // Ensure readOnlyKey exists for all bookmark keys (migration)
                // Now using server-side Pseudo-Bookmark Anchor
                let keysUpdated = false
                if (sessionUser.bookmarkKeys && Array.isArray(sessionUser.bookmarkKeys)) {
                    for (const k of sessionUser.bookmarkKeys) {
                        if (!k.readOnlyKey) {
                            // Fetch all items to find anchor
                            try {
                                const kRes = await fetch(`${BASE_URL}/FastDB?key=u_${k.key}`)
                                const kData = await kRes.json()
                                const existingAnchor = kData.find((item: any) => {
                                    const c = safeParse(item.content)
                                    return c.type === 'READ_ONLY_ANCHOR'
                                })

                                if (existingAnchor) {
                                    k.readOnlyKey = existingAnchor.id
                                } else {
                                    // Create new anchor
                                    const anchorContent = {
                                        name: 'System Read-Only Anchor',
                                        url: 'about:readonly',
                                        type: 'READ_ONLY_ANCHOR',
                                        isApp: false,
                                        description: 'DO NOT DELETE. This record enables Read-Only access.'
                                    }
                                    const createRes = await fetch(`${BASE_URL}/FastDB?key=u_${k.key}`, {
                                        method: 'POST',
                                        headers: { 'Content-Type': 'application/json' },
                                        body: JSON.stringify(anchorContent)
                                    })
                                    const createJson = await createRes.json()
                                    k.readOnlyKey = createJson.id
                                }
                                keysUpdated = true
                            } catch (e) {
                                console.error('Failed to ensure anchor for key', k.key, e)
                            }
                        }
                    }
                }

                if (keysUpdated) {
                    await fetch(`${BASE_URL}/FastDB/${userRecord.id}?key=${userAuthKey}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(sessionUser)
                    })
                }

                currentUser.value = sessionUser

                // Set default active keys
                // activeUserKey.value = sessionUser.dataKey

                localStorage.setItem('webnav_user', JSON.stringify(sessionUser))
                toast.success(t('tools.webNav.auth.login') + ' Success')
                auth.value.open = false
                onSuccess()
            } else {
                toast.error(t('tools.webNav.auth.invalidAuth'))
            }
        } catch (e) {
            toast.error('Auth server error')
        } finally {
            auth.value.loading = false
        }
    }

    const handleRegister = async () => {
        if (!auth.value.form.username || !auth.value.form.password) return
        auth.value.loading = true
        try {
            const userAuthKey = getAuthKey(auth.value.form.username)
            const checkRes = await fetch(`${BASE_URL}/FastDB?key=${userAuthKey}`)
            const existing = await checkRes.json()
            if (existing && existing.length > 0) {
                toast.error(t('tools.webNav.auth.userExists'))
                return
            }

            const dataKey = generateSecureKey(auth.value.form.username)

            // Create Read-Only Anchor immediately
            let readOnlyKey = ''
            try {
                const anchorContent = {
                    name: 'System Read-Only Anchor',
                    url: 'about:readonly',
                    type: 'READ_ONLY_ANCHOR',
                    isApp: false,
                    description: 'DO NOT DELETE. This record enables Read-Only access.'
                }
                const createRes = await fetch(`${BASE_URL}/FastDB?key=u_${dataKey}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(anchorContent)
                })
                const createJson = await createRes.json()
                readOnlyKey = createJson.id
            } catch (e) {
                console.error('Failed to create anchor during reg', e)
            }

            // Construct new user with all supported features keys
            const newUser = {
                username: auth.value.form.username,
                passwordHash: hashPassword(auth.value.form.password),
                dataKey,
                bookmarkKeys: [{ name: 'Default', key: dataKey, readOnlyKey: readOnlyKey || '' }],
                messageKeys: [{ name: 'Default', key: dataKey }]
            }

            await fetch(`${BASE_URL}/FastDB?key=${userAuthKey}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newUser)
            })

            toast.success(t('tools.webNav.auth.regSuccess'))
            auth.value.mode = 'login'
        } catch (e) {
            toast.error('Registration failed')
        } finally {
            auth.value.loading = false
        }
    }

    const handleUpdateProfile = async (onSuccess: () => Promise<void>) => {
        if (!currentUser.value) return
        auth.value.loading = true

        try {
            // 1. Verify old password
            if (!auth.value.form.oldPassword) {
                toast.error(t('tools.webNav.auth.oldPasswordRequired') || 'Old password required')
                auth.value.loading = false
                return
            }

            const oldPwdHash = hashPassword(auth.value.form.oldPassword)
            if (oldPwdHash !== currentUser.value.passwordHash) {
                toast.error(t('tools.webNav.auth.oldPasswordIncorrect') || 'Old password incorrect')
                auth.value.loading = false
                return
            }



            const usernameChanged = auth.value.form.username.toLowerCase() !== currentUser.value.username.toLowerCase()

            // 2. Check Username Uniqueness
            if (usernameChanged) {
                const newAuthKeyCheck = getAuthKey(auth.value.form.username)
                const checkRes = await fetch(`${BASE_URL}/FastDB?key=${newAuthKeyCheck}`)
                const checkData = await checkRes.json()
                if (checkData && checkData.length > 0) {
                    toast.error(t('tools.webNav.auth.userExists') || 'User already exists')
                    auth.value.loading = false
                    return
                }
            }

            const oldAuthKey = currentUser.value.authKey || getAuthKey(currentUser.value.username)
            const newAuthKey = getAuthKey(auth.value.form.username)

            // 3. Fix Key Rotation: DO NOT generate new data key, keep the old one to preserve bookmarks
            const newDataKey = currentUser.value.dataKey

            const updated: any = {
                ...currentUser.value,
                username: auth.value.form.username,
                passwordHash: auth.value.form.password ? hashPassword(auth.value.form.password) : currentUser.value.passwordHash,
                dataKey: newDataKey,
            }

            // Update or Create new Auth Record
            if (usernameChanged) {
                await fetch(`${BASE_URL}/FastDB/${currentUser.value.dbId}?key=${oldAuthKey}`, { method: 'DELETE' })
                const createRes = await fetch(`${BASE_URL}/FastDB?key=${newAuthKey}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(updated)
                })
                const createData = await createRes.json()
                updated.dbId = createData.id
            } else {
                await fetch(`${BASE_URL}/FastDB/${currentUser.value.dbId}?key=${oldAuthKey}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(updated)
                })
                updated.dbId = currentUser.value.dbId
            }

            updated.authKey = newAuthKey
            currentUser.value = updated
            localStorage.setItem('webnav_user', JSON.stringify(updated))
            toast.success(t('tools.webNav.auth.updateSuccess'))
            auth.value.open = false

            if (onSuccess) await onSuccess()

        } catch (e) {
            toast.error('Update failed')
        } finally {
            auth.value.loading = false
        }
    }

    const handleLogout = () => {
        currentUser.value = null
        localStorage.removeItem('webnav_user')
        toast.info(t('tools.webNav.auth.logout'))
    }

    // Init from LocalStorage
    const initAuth = () => {
        if (typeof window !== 'undefined') {
            try {
                const storedUser = localStorage.getItem('webnav_user')
                if (storedUser && storedUser !== 'null') {
                    currentUser.value = JSON.parse(storedUser)
                }
            } catch (e) { }
        }
    }

    return {
        currentUser,
        auth,
        getAuthKey,
        openAuthModal,
        handleLogin,
        handleRegister,
        handleUpdateProfile,
        handleLogout,
        initAuth
    }
}
