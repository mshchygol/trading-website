<script setup lang="ts">
const route = useRoute();

const linkClass = (path: string) => {
  return route.path === path
    ? "bg-green-700 text-white rounded-t-lg"
    : ""
};
</script>

<template>
    <main class="p-12">
        <h1 class="text-3xl my-4">Unorthodox trading website</h1>
        <nav class="border-b-2 border-green-700 mb-4 text-lg">
            <NuxtLink 
                to="/" 
                class="p-2 inline-block" 
                :class="linkClass('/')"
            >
                Order book
            </NuxtLink>
            <NuxtLink 
                to="/audit-log" 
                class="ml-4 p-2 inline-block" 
                :class="linkClass('/audit-log')"
            >
                Audit log
            </NuxtLink>
        </nav>
        <NuxtPage />
    </main>
</template>