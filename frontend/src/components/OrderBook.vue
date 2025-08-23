<script setup>
import { ref, watch } from 'vue';
import { useWebSocket } from '../composable/useWebSocket';
import Chart from './Chart.vue';

const { message, isConnected, send, error } = useWebSocket('ws://localhost:5263/ws/orderbook');
const data = ref([]);
const buyAmount = ref(null);
const quote = ref(null);
let throttling = 0;

function sendAmount() {
    send(JSON.stringify({"buyAmount": buyAmount.value}));
}

watch(message, (newValue) => {
    throttling++;
    if (throttling % 2 === 0) {
        data.value = newValue.bids.reverse().concat(newValue.asks).map(item => ({
            name: item[0],
            value: item[1]
        }))

        if (buyAmount.value && newValue.quote) {
            quote.value = {
                formattedAmount: new Intl.NumberFormat('de-DE', {
                    style: 'currency',
                    currency: 'EUR'
                }).format(newValue.quote.eurCost),
                btcAmount: newValue.quote.btcAmount
            };
        }   
    }
});
</script>

<template>
    <div>
        <p v-if="isConnected">Connected to WebSocket</p>
        <p v-else>Disconnected from WebSocket</p>
        <div>
            <input type="number" v-model="buyAmount"></input>
            <button @click="sendAmount">Show the price</button>
        </div>
        <p v-if="quote">Average price for {{ quote.btcAmount }} BTC would be: {{ quote.formattedAmount }}</p>
        <Chart :data="data" />
        <p v-if="!isConnected && !error">Connecting...</p>
        <p v-if="error">Got error: {{ error.type }}</p>
    </div>
</template>

<style scoped></style>
