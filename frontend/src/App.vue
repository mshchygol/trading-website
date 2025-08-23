<script setup></script>

<template>
    <p>Unorthodox trading website</p>
    <nav>
        <RouterLink to="/">Go to Home</RouterLink>
        <RouterLink to="/audit-log">Go to audit log</RouterLink>
    </nav>
    <main>
        <RouterView />
    </main>
</template>

<style scoped></style>
