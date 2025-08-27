import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { defineComponent } from "vue";
import { useWebSocket } from "@/composable/useWebSocket";

// --- Mock WebSocket ---
class MockWebSocket {
    url: string;
    readyState = 0;
    sentMessages: any[] = [];
    onopen: (() => void) | null = null;
    onmessage: ((event: MessageEvent) => void) | null = null;
    onerror: ((event: Event) => void) | null = null;
    onclose: (() => void) | null = null;

    constructor(url: string) {
        this.url = url;
        MockWebSocket.instances.push(this);
    }

    static instances: MockWebSocket[] = [];

    send(msg: any) {
        this.sentMessages.push(msg);
    }

    close() {
        this.onclose?.();
    }

    simulateOpen() {
        this.readyState = 1;
        this.onopen?.();
    }
    simulateMessage(data: any) {
        this.onmessage?.({ data } as MessageEvent);
    }
    simulateError(err: any = new Event("error")) {
        this.onerror?.(err);
    }
    simulateClose() {
        this.readyState = 3;
        this.onclose?.();
    }
}

describe("useWebSocket", () => {
    beforeEach(() => {
        MockWebSocket.instances = [];
        vi.stubGlobal("WebSocket", MockWebSocket as any);
    });

    function mountWithComposable(url: string) {
        let api: ReturnType<typeof useWebSocket<any>>;
        const Comp = defineComponent({
            setup() {
                api = useWebSocket(url);
                return {};
            },
            template: "<div />",
        });
        mount(Comp);
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        return api!;
    }

    it("connects and sets isConnected on open", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        socket.simulateOpen();
        expect(api.isConnected.value).toBe(true);
    });

    it("receives and parses messages", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        socket.simulateOpen();
        socket.simulateMessage(JSON.stringify({ text: "hello" }));
        expect(api.message.value).toEqual({ text: "hello" });
    });

    it("handles invalid JSON messages with error", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        socket.simulateOpen();
        socket.simulateMessage("not-json");
        expect(api.error.value).toBeInstanceOf(Error);
    });

    it("captures WebSocket errors", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        const event = new Event("error");
        socket.simulateError(event);
        expect(api.error.value).toBe(event);
    });

    it("sets isConnected=false on close", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        socket.simulateOpen();
        socket.simulateClose();
        expect(api.isConnected.value).toBe(false);
    });

    it("sends message only when connected", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        api.send("hello");
        expect(socket.sentMessages).toHaveLength(0);
        socket.simulateOpen();
        api.send("hello");
        expect(socket.sentMessages).toEqual(["hello"]);
    });

    it("closes socket on close()", () => {
        const api = mountWithComposable("ws://test");
        const socket = MockWebSocket.instances[0]!;
        socket.simulateOpen();
        api.close();
        expect(api.isConnected.value).toBe(false);
    });
});
