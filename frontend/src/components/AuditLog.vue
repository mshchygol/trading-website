<script setup>
import { useFetch } from '@/composable/useFetch';
import Chart from './Chart.vue';
import { computed, ref } from 'vue';

const chartData = ref([]);
const { data, isLoading, error } = useFetch('http://localhost:5263/auditlog');
const auditlog = computed(() => {
    return data?.value 
    ? data.value.map((item, index) => ({snapshot: item.snapshot, timestamp: formatDate(new Date(item.timestamp)), index})).reverse() 
    : [];
})
let selectedSnapshotIndex = null;

function selectSnapshot({snapshot, index}) {
    const data = JSON.parse(snapshot).data;
    selectedSnapshotIndex = index;
    chartData.value = data.bids.reverse().concat(data.asks).map(item => ({
        name: item[0],
        value: item[1]
    }))
}

function formatDate(date) {
    const pad = (num, size = 2) => String(num).padStart(size, '0');

    const day = pad(date.getDate());
    const month = pad(date.getMonth() + 1);
    const year = date.getFullYear();

    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());
    const seconds = pad(date.getSeconds());
    const milliseconds = pad(date.getMilliseconds(), 3);

    return `${day}.${month}.${year} ${hours}:${minutes}:${seconds}:${milliseconds}`;
}

</script>

<template>
    <div>
        <p class="text-lg">Welcome to audit log, please select a snapshot to review:</p>
        <Chart :data="chartData"/>
        <p v-if="isLoading">Loading....</p>
        <p v-if="error">Got error! {{ error }}</p>
        <ul>
            <li v-for="value in auditlog" 
                class="p-2 transition-all cursor-pointer hover:bg-gray-700 hover:text-white" 
                :class="{'bg-gray-200': value.index === selectedSnapshotIndex}"
                @click="selectSnapshot(value)"
            >
                Snapshot saved on: {{ value.timestamp }}
            </li>
        </ul>
    </div>
</template>

<style scoped></style>
