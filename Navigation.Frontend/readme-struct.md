# 项目结构与框架说明

## 1. 技术栈概览

本项目基于 **Vue 3** + **TypeScript** + **Vite** 构建，采用了现代化的前端技术栈。

### 核心框架 & 构建工具

- **Vue 3**: 渐进式 JavaScript 框架 (v3.5+)
- **TypeScript**: 强类型语言支持
- **Vite**: 下一代前端构建工具 (v6.0+)
- **Vite SSG**: 服务端静态生成 (Static Site Generation)，用于 SEO 优化

### UI & 样式

- **Tailwind CSS (v4)**: 原子化 CSS 框架
- **Reka UI**: 无头 (Headless) UI 组件库
- **Lucide Vue Next**: 现代化图标库
- **Tw Animate CSS**: Tailwind 动画扩展

### 状态管理 & 路由

- **Pinia**: Vue 的专属状态管理库
- **Vue Router**: 路由管理器

### 工具库

- **VueUse**: Vue 组合式 API 工具集
- **Crypto-js**: 加密标准算法库
- **Pako**: Zlib 压缩库
- **Diff Match Patch**: 文本差异对比

## 2. 项目结构

```
e:\WorkProject\Vue\Project\WebTool\
├── .github/              # GitHub Actions 配置
├── .vscode/              # VS Code 编辑器配置
├── docs/                 # 构建输出目录 (GitHub Pages)
├── public/               # 静态资源目录
├── src/                  # 源代码目录
│   ├── assets/           # 静态资源 (图片、样式等)
│   ├── components/       # 公共组件
│   ├── composables/      # 组合式函数 (Hooks)
│   ├── config/           # 配置文件
│   ├── lib/              # 第三方库封装或核心逻辑
│   ├── locales/          # 国际化语言包
│   ├── router/           # 路由配置
│   ├── stores/           # Pinia 状态仓库
│   ├── types/            # TypeScript 类型定义
│   ├── utils/            # 工具函数
│   ├── views/            # 页面视图
│   ├── App.vue           # 根组件
│   ├── main.ts           # 入口文件
│   └── style.css         # 全局样式
├── .env                  # 环境变量
├── package.json          # 项目配置与依赖
├── readme-struct.md      # 项目结构说明 (本文档)
├── tsconfig.json         # TypeScript 配置
└── vite.config.ts        # Vite 配置
```

## 3. 脚手架安装与使用

本项目使用 `pnpm` 作为包管理器。

### 环境准备

请确保已安装 Node.js (推荐 LTS 版本) 和 pnpm。

```bash
# 安装 pnpm (如果你还没有安装)
npm install -g pnpm
```

### 依赖安装

```bash
pnpm install
```

### 开发启动

```bash
# 启动开发服务器
pnpm dev
# 默认端口: 25121
```

### 项目构建

```bash
# 构建生产版本 (SSG)
pnpm build:ssg

# 预览构建产物
pnpm preview
```

### 其他命令

```bash
# 仅构建 (不含 SSG)
pnpm build
```
