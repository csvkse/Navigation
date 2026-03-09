新增导航主题指南

现在添加一个新主题非常清晰，只需以下三步：

1. 注册主题配置
   在
   src/config/webNav/webNavTheme.ts
   中添加新主题的元数据。

typescript
// 引入图标 (可选)
import { Sparkles } from 'lucide-vue-next'
export const WEBNAV_THEMES: WebNavTheme[] = [
// ... 其他主题
{
id: 'new-theme', // [!code focus] 唯一 ID
name: 'New Theme', // [!code focus] 显示名称
icon: Sparkles, // [!code focus] 图标
containerClass: 'theme-new', // [!code focus] 必须：用于 CSS 作用域的类名
hasBackgroundEffect: true // [!code focus] 是否有自定义背景组件
}
] 2. 定义主题样式
在
src/config/webNav/webNav.css
中添加该主题的配色方案。需分别为日间模式和夜间模式定义 CSS 变量。

css
/_ --- New Theme --- _/
/_ 日间模式 _/
:root:has([class*="theme-new"]) {
--background: oklch(0.95 0.05 200); /_ 浅蓝背景 _/
--foreground: oklch(0.2 0.05 200);
--primary: oklch(0.6 0.15 200); /_ 主色调 _/
--muted: oklch(0.9 0.05 200);
--border: oklch(0.8 0.05 200);
/_ 可选：覆盖字体大小 _/
--app-name-size: 12px;
}
/_ 夜间模式 _/
.dark:has([class*="theme-new"]) {
--background: oklch(0.1 0.05 200); /_ 深蓝背景 _/
--foreground: oklch(0.9 0.05 200);
--primary: oklch(0.7 0.15 200);
--muted: oklch(0.2 0.05 200);
--border: oklch(0.3 0.05 200);

    /* 可选：覆盖书签容器背景 */
    --bookmark-card-bg: oklch(0.15 0.05 200 / 0.8);

} 3. 实现背景特效 (可选)
如果 hasBackgroundEffect 为 true，在
src/views/Pages/Tools/WebNav/WebNavBackground.vue
中添加对应的模板。

vue
<template v-else-if="themeId === 'new-theme'">
<!-- 使用 CSS 变量以支持日/夜自动切换 -->
<div class="absolute inset-0 bg-background"></div>
<!-- 您的自定义背景元素/动画 -->
<div class="absolute inset-0 bg-gradient-to-tr from-primary/10 to-transparent"></div>
</template>
完成这三步后，新主题就会自动出现在设置面板中，并支持完整的日/夜切换和云端同步功能。
