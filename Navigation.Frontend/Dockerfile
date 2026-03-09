# ==========================================
# 第一阶段：构建环境 (Builder)
# 优化：使用 lts-alpine 版本，体积小且稳定
# ==========================================
FROM node:lts-alpine AS builder

WORKDIR /workspace

# 设置国内源并安装 pnpm
RUN npm config set registry https://registry.npmmirror.com && \
    npm install -g pnpm

# 利用 Docker 缓存层：先仅复制依赖描述文档
COPY package.json pnpm-lock.yaml ./
RUN pnpm install

# 复制所有源码
COPY . .

# ---------------------------------------------------
# 【关键修改 1】执行 SSG 构建
# 必须使用 build:ssg，否则只会生成空壳 SPA
# ---------------------------------------------------
RUN npm run build:ssg

# ==========================================
# 第二阶段：生产环境 (Nginx)
# ==========================================
FROM nginx:alpine

# 1. 复制构建产物
# 注意：根据你的日志，vite-ssg 输出目录是 docs
COPY --from=builder /workspace/docs /usr/share/nginx/html

# 2. 写入 Nginx 配置
# 我们直接在 Dockerfile 里写入配置，避免维护额外的 .conf 文档
RUN echo 'server { \
    listen 80; \
    server_name localhost; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    # ========================================== \
    # 【性能优化】 \
    # ========================================== \
    # 开启高效文档传输 (生产环境容器内无挂载卷问题，强烈建议开启) \
    sendfile on; \
    tcp_nopush on; \
    \
    # 开启 Gzip 压缩 \
    gzip on; \
    gzip_min_length 1k; \
    gzip_comp_level 5; \
    gzip_types text/plain application/javascript application/x-javascript text/css application/xml text/javascript; \
    \
    # ========================================== \
    # 【SSG 内核路由策略】 \
    # ========================================== \
    location / { \
        # 尝试顺序： \
        # 1. 精确文档 ($uri) -> 比如 style.css \
        # 2. 目录 ($uri/) -> 比如访问 /folder/ 找 index.html \
        # 3. HTML后缀 ($uri.html) -> 内核！访问 /tools 自动隐形映射到 /tools.html \
        # 4. 首页兜底 (/index.html) -> 处理 404 或纯前端动态路由 \
        try_files $uri $uri/ $uri.html /index.html; \
    } \
    \
    # 静态资源缓存策略 (JS/CSS/图片) \
    location ~ .*\.(gif|jpg|jpeg|png|bmp|swf|ico|svg|js|css)$ { \
        expires 30d; \
        access_log off; \
    } \
}' > /etc/nginx/conf.d/default.conf

# 暴露端口
EXPOSE 80

# 启动 Nginx
CMD ["nginx", "-g", "daemon off;"]