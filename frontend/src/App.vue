<script setup>
import { ref, watch } from 'vue';
import Chart from './components/Chart.vue';
import { useWebSocket } from './composable/useWebSocket';

const { message, isConnected, send, close, connect } = useWebSocket('ws://localhost:5263/ws/orderbook');
const data = ref([]);
let throttling = 0;

async function getAuditLog() {
    try {
        const response = await fetch('http://localhost:5263/auditlog');
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        console.log(data);
    } catch (error) {
        console.error('Fetch error:', error);
    }
}

watch(message, (newValue) => {
    if (newValue.event === 'data') {
        throttling++;
        if (throttling % 2 === 0) {
            data.value = newValue.data.bids.reverse().concat(newValue.data.asks).map(item => ({
                name: item[0],
                value: item[1]
            }))
        }
    }
});
</script>

<template>
    <div>
        <p>Unorthodox trading website</p>
        <p v-if="isConnected">Connected to WebSocket</p>
        <p v-else>Disconnected from WebSocket</p>
        <button @click="connect">open socket connection</button>
        <button @click="close">close socket connection</button>
        <button @click="getAuditLog">get audit log</button>
        <Chart :data="data" />
    </div>
</template>

<style scoped></style>
