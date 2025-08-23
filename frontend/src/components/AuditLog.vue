<script setup>
import { useFetch } from '@/composable/useFetch';
import Chart from './Chart.vue';
import { computed, ref } from 'vue';

const chartData = ref([]);
const { data, isLoading, error } = useFetch('http://localhost:5263/auditlog');
const auditlog = computed(() => {
    return data?.value ? data.value.map((item) => ({snapshot: item.snapshot, timestamp: item.timestamp})).reverse() : [];
})

console.log('what ????', auditlog.value);

function selectSnapshot(snapshot) {
    const data = JSON.parse(snapshot).data;
    chartData.value = data.bids.reverse().concat(data.asks).map(item => ({
        name: item[0],
        value: item[1]
    }))
}

</script>

<template>
    <div>
        <p>audit log</p>
        <Chart :data="chartData"/>
        <ul>
            <li v-for="value in auditlog" @click="selectSnapshot(value.snapshot)">Snapshot taken on: {{ value.timestamp }}</li>
        </ul>
    </div>
</template>

<style scoped></style>
