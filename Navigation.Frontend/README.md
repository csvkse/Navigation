项目介绍与 WebNav 功能更新说明：

1. 项目介绍
项目名称：WebTool (WebNav) 这是一个基于 Vue 3 + Vite 构建的现代化个人导航与工具仪表盘项目。

技术栈：Vue 3 (Script Setup), TypeScript, Tailwind CSS, Shadcn Vue (UI组件), Lucide Icons。
核心理念：极简、高性能（0延迟渲染）、高颜值（Premium/Cyberpunk 风格）。
架构：前端静态部署 + FastDB 后端 API (支持数据同步与鉴权)。
2. WebNav.vue 页面功能
WebNav 是该项目的核心“起始页”功能，集成了书签管理与应用启动器。

双模式布局：
Apps Grid：顶部展示常用的应用/工具入口。
Bookmarks：下方瀑布流展示分类书签。
管理模式 (Manage Mode)：支持拖拽排序 (Drag & Drop)、右键编辑、批量导入/导出。
多用户/多配置：支持用户登录 (Auth)，支持创建多套书签配置 (Keys) 并快速切换。
实时同步与缓存：本地 LocalStorage 瞬时加载 + 后端无感同步。
视觉方案：内置多套 UI 主题（玻璃拟态、赛博朋克、极简风格等）。
3. 新增功能：日夜独立 UI Scheme 配置
根据您的要求，已完成UI Scheme (界面样式) 的重构。现在“白天模式”和“黑夜模式”可以分别独立配置不同的 UI 主题，系统会根据当前的日夜状态自动切换。

功能亮点
独立配置：你可以设置“白天使用 Minimal 极简白”，而“晚上使用 Cyberpunk 赛博黑”。
自动切换：点击 Header 上的日夜切换按钮（或随系统变化），主题会自动变更为设定好的对应 Styles。
配置解耦：所有主题样式定义已抽离至 
src/config/webNavTheme.ts
，新增样式无需修改页面逻辑代码。
使用介绍
打开设置面板：点击右上角的 Layers (图层图标)。
分别选择主题：
Day Theme (日间)：选择一个适合亮色的主题（如 Premium Glass 或 Ultra Pure）。
Night Theme (夜间)：选择一个适合暗色的主题（如 Cyberpunk 或 Neon Nights）。
完成：以后切换日夜模式时，WebNav 将自动呈现你预设的这两种不同外观。
代码扩展指南 (新增样式)
如果您未来想增加新的风格（配色或字体），只需修改以下两个文件：

1. 注册新主题 在 
src/config/webNavTheme.ts
 的 WEBNAV_THEMES 数组中添加配置：

typescript
{
    id: 'my-new-style',       // 唯一ID
    name: 'My Custom Style',  // 显示名称
    icon: Zap,                // 图标 (从 lucide-vue-next 导入)
    containerClass: 'font-serif bg-slate-50', // 全局样式类 (Tailwind)
    hasBackgroundEffect: true // 是否开启自定义背景特效组件
}
2. (可选) 添加专属背景特效 如果 hasBackgroundEffect 为 true，请在 
src/views/Pages/Tools/WebNav/WebNavBackground.vue
 中添加对应的背景代码：

vue
<template v-else-if="themeId === 'my-new-style'">
    <!-- 在这里写你的 CSS/Div 背景动画 -->
    <div class="absolute inset-0 bg-red-500/10"></div>
</template>
已为您预置了 Forest (森林) 和 Neon (霓虹) 两个新主题作为扩展示例。




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
        id: 'new-theme',          // [!code focus] 唯一 ID
        name: 'New Theme',        // [!code focus] 显示名称
        icon: Sparkles,           // [!code focus] 图标
        containerClass: 'theme-new', // [!code focus] 必须：用于 CSS 作用域的类名
        hasBackgroundEffect: true // [!code focus] 是否有自定义背景组件
    }
]
2. 定义主题样式
在 
src/config/webNav/webNav.css
 中添加该主题的配色方案。需分别为日间模式和夜间模式定义 CSS 变量。

css
/* --- New Theme --- */
/* 日间模式 */
:root:has([class*="theme-new"]) {
    --background: oklch(0.95 0.05 200); /* 浅蓝背景 */
    --foreground: oklch(0.2 0.05 200);
    --primary: oklch(0.6 0.15 200);     /* 主色调 */
    --muted: oklch(0.9 0.05 200);
    --border: oklch(0.8 0.05 200);
    /* 可选：覆盖字体大小 */
    --app-name-size: 12px;
}
/* 夜间模式 */
.dark:has([class*="theme-new"]) {
    --background: oklch(0.1 0.05 200);  /* 深蓝背景 */
    --foreground: oklch(0.9 0.05 200);
    --primary: oklch(0.7 0.15 200);
    --muted: oklch(0.2 0.05 200);
    --border: oklch(0.3 0.05 200);
    
    /* 可选：覆盖书签容器背景 */
    --bookmark-card-bg: oklch(0.15 0.05 200 / 0.8);
}
3. 实现背景特效 (可选)
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