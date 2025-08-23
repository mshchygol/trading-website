<script setup>
import { ref, watch } from 'vue';
import { useWebSocket } from '../composable/useWebSocket';
import Chart from './Chart.vue';

const { message, isConnected, send, close, connect } = useWebSocket('ws://localhost:5263/ws/orderbook');
const data = ref([]);
let throttling = 0;

function sendAmount() {
    send(JSON.stringify({"buyAmount": Math.random()}))
}

watch(message, (newValue) => {
    throttling++;
    if (throttling % 2 === 0) {
        data.value = newValue.bids.reverse().concat(newValue.asks).map(item => ({
            name: item[0],
            value: item[1]
        }))
    }
});
</script>

<template>
    <div>
        <p v-if="isConnected">Connected to WebSocket</p>
        <p v-else>Disconnected from WebSocket</p>
        <button @click="connect">open socket connection</button>
        <button @click="close">close socket connection</button>
        <button @click="sendAmount">send amount</button>
        <Chart :data="data" />
    </div>
</template>

<style scoped></style>
