import type { RouteRecordRaw } from 'vue-router'

export const routes: RouteRecordRaw[] = [
  { path: '/:key?', name: 'web-nav', component: () => import('@/views/Pages/Tools/WebNav.vue') },
  { path: '/tools/web-nav/:key?', name: 'web-nav-key', component: () => import('@/views/Pages/Tools/WebNav.vue') },
]

// No top-level router instance to avoid SSR errors during ViteSSG build
// ViteSSG will create the router using the exported routes below
