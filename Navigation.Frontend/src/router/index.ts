import type { RouteRecordRaw } from 'vue-router'

export const routes: RouteRecordRaw[] = [
  // 将路径调整为根路径 /:key?，代替原来的 /tools/web-nav/:key?
  { 
    path: '/:key?', 
    name: 'web-nav-home', 
    component: () => import('@/views/Pages/Tools/WebNav.vue') 
  },
]

// No top-level router instance to avoid SSR errors during ViteSSG build
// ViteSSG will create the router using the exported routes below