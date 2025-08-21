<script setup>
import { useWebSocket } from './composable/useWebSocket';

const { message, isConnected, send } = useWebSocket('wss://ws.bitstamp.net');

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
</script>

<template>
    <div>
        <p v-if="isConnected">Connected to WebSocket</p>
        <p v-else>Disconnected from WebSocket</p>
        <button @click="sendSubscribeMessage">sendSubscribeMessage</button>
        <button @click="sendUnsubscribeMessage">sendUnsubscribeMessage</button>
        <div >{{ message }}</div>
    </div>
</template>

<style scoped></style>
