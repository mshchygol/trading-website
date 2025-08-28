export function useFetchUrl<T = unknown>(
    apiUrl: string,
    options: RequestInit = {}
) {
    const data: Ref<T | null> = ref(null);
    const isLoading = ref(false);
    const error: Ref<Error | null> = ref(null);

    const execute = async (
        url: string = apiUrl,
        fetchOptions: RequestInit = options
    ): Promise<void> => {
        isLoading.value = true;
        error.value = null;

        try {
            const response = await fetch(url, fetchOptions);
            if (!response.ok) {
                throw new Error(`Error ${response.status}: ${response.statusText}`);
            }
            data.value = (await response.json()) as T;
        } catch (err) {
            error.value = err instanceof Error ? err : new Error(String(err));
        } finally {
            isLoading.value = false;
        }
    };

    execute();

    return { data, isLoading, error, execute };
}
