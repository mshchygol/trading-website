<script setup>
import { computed, ref, watch } from 'vue';
import { useWebSocket } from '../composable/useWebSocket';
import Chart from './Chart.vue';
import { formatMoney } from '@/utils';

const { message, isConnected, send, error } = useWebSocket('ws://localhost:5263/ws/orderbook');
const data = ref([]);
const buyAmount = ref(null);
const quote = ref(null);
let throttling = 0;
const buttonDisabled = computed(() => {
    return !buyAmount.value || buyAmount.value <= 0
})

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
                formattedAmount: formatMoney(newValue.quote.eurCost),
                btcAmount: newValue.quote.btcAmount,
                success: newValue.quote.success
            };
        }   
    }
});
</script>

<template>
    <div>
        <h2 v-if="isConnected" class="text-lg">
            Connected to WebSocket - live data from 
            <strong>wss://ws.bitstamp.net</strong>
            <span v-if="!isConnected && !error || isConnected && data.length === 0" class="ml-4 text-red-500">Connecting...</span>
        </h2>
        <h2 v-else class="text-lg">Disconnected from WebSocket</h2>
        <div class="my-4">
            <label for="amount" class="block text-lg mb-2">Check the price for desired amount:</label>
            <input 
                type="number" 
                id="amount" v-model="buyAmount" 
                placeholder="Your amount" 
                class="border border-gray-300 rounded-md px-3 py-2 text-gray-900 placeholder-gray-400 focus:outline-none focus:ring focus:ring-blue-500 focus:border-blue-500">
            </input>
            <button 
                type="button" 
                :disabled="buttonDisabled" 
                class="ml-4 bg-green-700 hover:bg-green-600 text-white font-bold py-2 px-4 rounded disabled:opacity-75 disabled:bg-gray-400" 
                @click="sendAmount">
                Show the price
            </button>
            <p v-if="quote?.success" class="text-lg pt-2">
                Average price for <strong>{{ quote.btcAmount }}</strong> BTC would be: <strong>{{ quote.formattedAmount }}</strong>
            </p>
        </div>
        <Chart :data="data" />
        <p v-if="error">Got error: {{ error.type }}</p>
    </div>
</template>

<style scoped></style>
