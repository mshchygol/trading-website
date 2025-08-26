<script setup lang="ts"></script>

<template>
    <main class="p-12">
        <h1 class="text-3xl my-4">Unorthodox trading website</h1>
        <nav class="border-b-2 border-green-700 mb-4 text-lg">
            <RouterLink class="p-2 inline-block" to="/">Order book</RouterLink>
            <RouterLink to="/audit-log" class="ml-4 p-2 inline-block">Audit log</RouterLink>
        </nav>
        <RouterView />
    </main>
</template>

<style>
@import "tailwindcss";
</style>
