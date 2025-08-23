<script setup></script>

<template>
    <h1>Unorthodox trading website</h1>
    <nav>
        <RouterLink to="/">Go to Home</RouterLink>
        <RouterLink to="/audit-log">Go to audit log</RouterLink>
    </nav>
    <main>
        <RouterView />
    </main>
</template>

<style scoped></style>
