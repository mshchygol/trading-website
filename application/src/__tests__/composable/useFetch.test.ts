import { useFetch } from "@/composable/useFetch";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { nextTick } from "vue";

describe("useFetch composable", () => {
    beforeEach(() => {
        vi.restoreAllMocks();
    });

    it("fetches data successfully on initialization", async () => {
        const mockData = { message: "hello" };

        vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
            ok: true,
            json: async () => mockData,
        } as Response);

        const { data, isLoading, error } = useFetch<typeof mockData>(
            "https://api.test/success"
        );

        // wait for composable to finish initial execute
        await nextTick();
        await Promise.resolve(); // let async finish

        expect(isLoading.value).toBe(false);
        expect(error.value).toBeNull();
        expect(data.value).toEqual(mockData);
    });

    it("sets error when fetch response is not ok", async () => {
        vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
            ok: false,
            status: 404,
            statusText: "Not Found",
        } as Response);

        const { data, error, isLoading } = useFetch("https://api.test/404");

        await nextTick();
        await Promise.resolve();

        expect(isLoading.value).toBe(false);
        expect(data.value).toBeNull();
        expect(error.value).toBeInstanceOf(Error);
        expect(error.value?.message).toBe("Error 404: Not Found");
    });

    it("sets error when fetch throws", async () => {
        vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("Network error"));

        const { error, isLoading, data } = useFetch("https://api.test/fail");

        await nextTick();
        await Promise.resolve();

        expect(isLoading.value).toBe(false);
        expect(data.value).toBeNull();
        expect(error.value).toBeInstanceOf(Error);
        expect(error.value?.message).toBe("Network error");
    });

    it("can execute fetch manually with new URL", async () => {
        const mockData1 = { id: 1 };
        const mockData2 = { id: 2 };

        const fetchMock = vi
            .spyOn(globalThis, "fetch")
            .mockResolvedValueOnce({
                ok: true,
                json: async () => mockData1,
            } as Response)
            .mockResolvedValueOnce({
                ok: true,
                json: async () => mockData2,
            } as Response);

        const { data, execute } = useFetch<typeof mockData1>(
            "https://api.test/initial"
        );

        await nextTick();
        await Promise.resolve();

        expect(data.value).toEqual(mockData1);

        await execute("https://api.test/second");
        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(data.value).toEqual(mockData2);
    });
});
