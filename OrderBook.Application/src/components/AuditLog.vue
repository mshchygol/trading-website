<script setup>
import { useFetch } from '@/composable/useFetch';
import Chart from './Chart.vue';
import { computed, ref } from 'vue';
import { formatDate } from '@/utils';
// import { meta } from 'eslint-plugin-vue';

const chartData = ref([]);
const { data, isLoading, error } = useFetch(`${import.meta.env.VITE_API_URL}/auditlog`);
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

</script>

<template>
    <div>
        <p class="text-lg">Welcome to audit log, please select a snapshot to review:</p>
        <Chart :data="chartData"/>
        <p v-if="isLoading">Loading....</p>
        <p v-if="error">Got error! {{ error }}</p>
        <ul>
            <li v-for="value in auditlog" 
                :key="auditlog.index"
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
