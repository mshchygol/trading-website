import { ref, onMounted, onUnmounted } from 'vue';

export function useWebSocket(url) {
    const socket = ref(null);
    const message = ref({});
    const isConnected = ref(false);
    const error = ref(null);

    const connect = () => {
        socket.value = new WebSocket(url);

        socket.value.onopen = () => {
            isConnected.value = true;
            error.value = null;
            console.log('WebSocket connected');
        };

        socket.value.onmessage = (event) => {
            message.value = JSON.parse(event.data);
        };

        socket.value.onerror = (err) => {
            error.value = err;
            console.error('WebSocket error:', err);
        };

        socket.value.onclose = () => {
            isConnected.value = false;
            console.log('WebSocket disconnected');
        };
    };

    const send = (message) => {
        if (socket.value && isConnected.value) {
            socket.value.send(message);
        } else {
            console.warn('WebSocket not connected. Cannot send message.');
        }
    };

    const close = () => {
        if (socket.value) {
            socket.value.close();
        }
    };

    onMounted(connect);
    onUnmounted(close);

    return {
        socket,
        message,
        isConnected,
        error,
        send,
        close,
    };
}