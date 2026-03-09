import { ViteSSG } from 'vite-ssg'
import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import './style.css'
import App from './App.vue'
import { routes } from './router'


export const createApp = ViteSSG(
    App,
    { routes, base: import.meta.env.BASE_URL },
    ({ app }) => {
        const pinia = createPinia()
        if (!import.meta.env.SSR) {
            pinia.use(piniaPluginPersistedstate)
        }
        app.use(pinia)
    }
)
