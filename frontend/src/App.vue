<script setup>
import { ref, watch } from 'vue';
import Chart from './components/Chart.vue';
import { useWebSocket } from './composable/useWebSocket';

const { message, isConnected, send } = useWebSocket('wss://ws.bitstamp.net');
const data = ref([]);
let throttling = 0;

const sendSubscribeMessage = () => {
    send(JSON.stringify({
        "event": "bts:subscribe",
        "data": {
            "channel": "order_book_btceur"
        }
    }));
};

const sendUnsubscribeMessage = () => {
    send(JSON.stringify({
        "event": "bts:unsubscribe",
        "data": {
            "channel": "order_book_btceur"
        }
    }));
};

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
        <button @click="sendSubscribeMessage">sendSubscribeMessage</button>
        <button @click="sendUnsubscribeMessage">sendUnsubscribeMessage</button>
        <Chart :data="data" />
    </div>
</template>

<style scoped></style>
