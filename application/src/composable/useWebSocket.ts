import { ref, onMounted, onUnmounted, type Ref } from 'vue';

export function useWebSocket<T = unknown>(url: string) {
    const socket: Ref<WebSocket | null> = ref(null);
    const message: Ref<T | null> = ref(null);
    const isConnected = ref(false);
    const error: Ref<Event | Error | null> = ref(null);

    const connect = () => {
        socket.value = new WebSocket(url);

        socket.value.onopen = () => {
            isConnected.value = true;
            error.value = null;
            console.log('WebSocket connected');
        };

        socket.value.onmessage = (event: MessageEvent) => {
            try {
                message.value = JSON.parse(event?.data) as T;
            } catch (err) {
                console.error('Failed to parse WebSocket message:', err);
                error.value = err instanceof Error ? err : new Error(String(err));
            }
        };

        socket.value.onerror = (err: Event) => {
            error.value = err;
            console.error('WebSocket error:', err);
        };

        socket.value.onclose = () => {
            isConnected.value = false;
            console.log('WebSocket disconnected');
        };
    };

    const send = (msg: string | ArrayBufferLike | Blob | ArrayBufferView) => {
        if (socket.value && isConnected.value) {
            socket.value.send(msg);
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
        connect,
    };
}
