import { ref } from 'vue'

export function useFetch(apiUrl, options = {}) {
    const data = ref(null)
    const isLoading = ref(false)
    const error = ref(null)

    const execute = async (url = apiUrl, fetchOptions = options) => {
        isLoading.value = true
        error.value = null
        try {
            const response = await fetch(url, fetchOptions)
            if (!response.ok) {
                throw new Error(`Error ${response.status}: ${response.statusText}`)
            }
            data.value = await response.json()
        } catch (err) {
            error.value = err
        } finally {
            isLoading.value = false
        }
    }

    execute()

    return { data, isLoading, error, execute }
}
