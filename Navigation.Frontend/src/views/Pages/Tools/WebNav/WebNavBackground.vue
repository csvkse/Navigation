<template>
    <div class="fixed inset-0 pointer-events-none -z-10 overflow-hidden transition-colors duration-500 bg-background">

        <!-- 1. Premium Glass Theme -->
        <template v-if="themeId === 'premium'">
            <!-- Dynamic Gradient Background -->
            <div class="absolute inset-0 bg-gradient-to-br from-background via-background/95 to-primary/5"></div>
            <!-- Organic Moving Blobs -->
            <div
                class="absolute -top-[10%] -left-[10%] w-[50%] h-[50%] bg-primary/20 blur-[120px] rounded-full animate-pulse opacity-60 dark:opacity-40">
            </div>
            <div class="absolute top-[20%] -right-[5%] w-[40%] h-[40%] bg-purple-500/15 blur-[100px] rounded-full dark:opacity-30"
                style="animation: blob-float 20s infinite alternate"></div>
            <div class="absolute -bottom-[10%] left-[20%] w-[45%] h-[45%] bg-blue-500/10 blur-[120px] rounded-full dark:opacity-20"
                style="animation: blob-float 25s infinite alternate-reverse"></div>
            <!-- Surface Grain -->
            <div
                class="absolute inset-0 opacity-[0.03] dark:opacity-[0.05] pointer-events-none bg-[url('https://grainy-gradients.vercel.app/noise.svg')]">
            </div>
        </template>

        <!-- 2. Minimal (Ultra Pure) -->
        <template v-else-if="themeId === 'minimal'">
            <div class="absolute inset-0 bg-background"></div>
        </template>

        <!-- 3. Cyberpunk -->
        <template v-else-if="themeId === 'cyber'">
            <!-- Custom Dark Background for Night Mode via CSS Var 'background' -->
            <div class="absolute inset-0 bg-background"></div>
            <!-- Grid Pattern using CSS Variable for Border -->
            <div
                class="absolute inset-0 bg-[linear-gradient(to_right,var(--border)_1px,transparent_1px),linear-gradient(to_bottom,var(--border)_1px,transparent_1px)] bg-[size:32px_32px] opacity-20">
            </div>
            <!-- Scanning Line -->
            <div
                class="absolute inset-0 bg-gradient-to-b from-transparent via-primary/5 to-transparent animate-scan z-0 pointer-events-none h-full w-full">
            </div>
            <!-- Bottom Glow -->
            <div class="absolute bottom-0 left-0 right-0 h-64 bg-gradient-to-t from-primary/10 to-transparent"></div>
        </template>

        <!-- 4. Neo-Retro -->
        <template v-else-if="themeId === 'retro'">
            <div class="absolute inset-0 bg-background"></div>
            <!-- Paper/Noise Texture -->
            <div
                class="absolute inset-0 opacity-[0.08] bg-[url('https://www.transparenttextures.com/patterns/cream-paper.png')] mix-blend-multiply dark:mix-blend-overlay">
            </div>
            <!-- Vignette -->
            <div
                class="absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_0%,var(--background)_100%)] opacity-80">
            </div>
        </template>

        <!-- 5. Zen Forest -->
        <template v-else-if="themeId === 'forest'">
            <div class="absolute inset-0 bg-gradient-to-b from-background to-background/90"></div>
            <!-- Pattern -->
            <div
                class="absolute top-0 left-0 w-full h-full opacity-[0.03] bg-[url('https://www.transparenttextures.com/patterns/cubes.png')]">
            </div>
            <!-- Soft Green Glows -->
            <div class="absolute -bottom-20 -right-20 w-96 h-96 bg-primary/10 blur-[100px] rounded-full"></div>
            <div class="absolute top-20 left-10 w-64 h-64 bg-emerald-500/10 blur-[80px] rounded-full"></div>
        </template>

        <!-- 6. Neon Nights -->
        <template v-else-if="themeId === 'neon'">
            <div class="absolute inset-0 bg-background"></div>
            <!-- Dot Matrix using CSS Border Var -->
            <div
                class="absolute inset-0 bg-[radial-gradient(var(--border)_1px,transparent_1px)] bg-[size:20px_20px] opacity-20">
            </div>
            <!-- Glowing Orbs -->
            <div
                class="absolute top-1/4 left-1/4 w-72 h-72 bg-primary/20 blur-[120px] rounded-full mix-blend-screen animate-pulse">
            </div>
            <div class="absolute bottom-1/4 right-1/4 w-72 h-72 bg-secondary/20 blur-[120px] rounded-full mix-blend-screen animate-pulse"
                style="animation-delay: 1s;"></div>
        </template>

        <!-- 7. Midnight Slate -->
        <template v-else-if="themeId === 'midnight-slate'">
            <div class="absolute inset-0 bg-[#222831]"></div>
            <div
                class="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-transparent via-white/10 to-transparent opacity-50">
            </div>
        </template>

    </div>
</template>

<script setup lang="ts">
defineProps<{
    themeId: string
}>()
</script>

<style scoped>
@keyframes blob-float {
    0% {
        transform: translate(0, 0) rotate(0deg);
    }

    100% {
        transform: translate(30px, 50px) rotate(15deg);
    }
}

@keyframes scan {
    0% {
        transform: translateY(-100%);
    }

    100% {
        transform: translateY(100%);
    }
}

.animate-scan {
    animation: scan 8s linear infinite;
}
</style>
