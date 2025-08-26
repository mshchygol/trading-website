<script setup lang="ts">
import { useFetch } from '@/composable/useFetch'
import Chart from './Chart.vue'
import { computed, ref } from 'vue'
import { formatDate } from '@/utils'
import type { AuditLogItem, ChartDataItem, SnapshotData, SnapshotValue } from '@/interfaces'

const chartData = ref<ChartDataItem[]>([])

const { data, isLoading, error } = useFetch<AuditLogItem[]>(
    `${import.meta.env.VITE_API_URL}/auditlog`
)

const auditlog = computed<SnapshotValue[]>(() => {
    return data?.value
        ? data.value
            .map((item, index) => ({
                snapshot: item.snapshot,
                timestamp: formatDate(new Date(item.timestamp)),
                index,
            }))
            .reverse()
        : []
})

const selectedSnapshotIndex = ref<number | null>(null)

function selectSnapshot({ snapshot, index }: SnapshotValue) {
    if (!snapshot) return
    const parsed: SnapshotData = JSON.parse(snapshot)
    selectedSnapshotIndex.value = index
    chartData.value = parsed.data.bids
        .reverse()
        .concat(parsed.data.asks)
        .map((item) => ({
            name: Number(item[0]),
            value: Number(item[1])
        }))
}
</script>

<template>
    <div>
        <p class="text-lg">Welcome to audit log, you can select a snapshot to review</p>
        <Chart :data="chartData" />
        <p v-if="isLoading">Loading....</p>
        <p v-if="error">Got error! {{ error }}</p>
        <p v-if="!isLoading && chartData.length === 0" class="my-4">No snapshots yet...</p>
        <ul>
            <li
                v-for="value in auditlog"
                :key="value.index"
                class="p-2 transition-all cursor-pointer hover:bg-gray-700 hover:text-white border-b-1"
                :class="{ 'bg-gray-200': value.index === selectedSnapshotIndex }"
                @click="selectSnapshot(value)"
            >
                Snapshot saved on: {{ value.timestamp }}
            </li>
        </ul>
    </div>
</template>

<style scoped></style>
