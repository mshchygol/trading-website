<script setup lang="ts">
import * as d3 from "d3";

const props = defineProps<{ data: ChartDataItem[] }>();

const chartContainer = ref<HTMLDivElement | null>(null);
const entriesToShow = ref<number>(100);

const CHART_HEIGHT = 400;
const CHART_MARGIN = 40;
const TOOLTIP_OFFSET = {
    left: 10,
    top: 20
};

let svg: d3.Selection<SVGGElement, unknown, null, undefined>;
let x: d3.ScaleLinear<number, number>;
let y: d3.ScaleLinear<number, number>;
let width: number;
let height: number;
let tooltip: d3.Selection<HTMLDivElement, unknown, null, undefined>;

onMounted(() => {
    drawChart();
});

watch(
    () => [props.data, entriesToShow.value],
    () => {
        if (!svg && props.data?.length > 0) {
            drawChart();
        } else if (svg) {
            updateChart();
        }
    },
    { deep: true }
);

function getAggregatedData(data: ChartDataItem[]): ChartDataItem[] {
    if (!data || data.length === 0) return [];

    const parsed = data.map((d) => ({
        name: Number(d.name),
        value: Number(d.value) || 0,
    }));

    const midIndex = Math.floor(parsed.length / 2);
    let bids = parsed.slice(0, midIndex).reverse();
    let asks = parsed.slice(midIndex);

    // apply limit (both bids and asks)
    const limit = entriesToShow.value;
    if (limit > 0 && limit < midIndex) {
        bids = bids.slice(0, limit);
        asks = asks.slice(0, limit);
    }

    // recompute cumulative values
    let bidSum = 0;
    const cumBids = bids.map((d) => ({
        name: d.name,
        value: (bidSum += d.value),
    })).reverse();

    let askSum = 0;
    const cumAsks = asks.map((d) => ({
        name: d.name,
        value: (askSum += d.value),
    }));

    return [...cumBids, ...cumAsks];
}

function drawChart() {
    const margin = { top: CHART_MARGIN, right: CHART_MARGIN, bottom: CHART_MARGIN, left: CHART_MARGIN };
    width = document.documentElement.clientWidth - margin.left - margin.right;
    height = CHART_HEIGHT - margin.top - margin.bottom;

    svg = d3
        .select(chartContainer.value)
        .append("svg")
        .attr("width", width)
        .attr("height", height + margin.top + margin.bottom)
        .append("g")
        .attr("transform", `translate(${margin.left},${margin.top})`);

    // scales
    x = d3
        .scaleLinear()
        .domain(d3.extent(props.data, (d) => d.name) as [number, number])
        .range([0, width]);

    y = d3
        .scaleLinear()
        .domain([0, d3.max(props.data, (d) => d.value) ?? 0])
        .nice()
        .range([height, 0]);

    // bars group
    svg.append("g").attr("class", "bars");

    // axes
    svg
        .append("g")
        .attr("class", "x-axis")
        .attr("transform", `translate(0,${height})`)
        .call(d3.axisBottom(x).ticks(10).tickFormat(d3.format(".2f")));
    svg.append("g").attr("class", "y-axis").call(d3.axisLeft(y));

    // tooltip div
    tooltip = d3
        .select(chartContainer.value)
        .append("div")
        .style("position", "absolute")
        .style("background", "rgba(0,0,0,0.7)")
        .style("color", "#fff")
        .style("padding", "4px 8px")
        .style("border-radius", "4px")
        .style("font-size", "12px")
        .style("pointer-events", "none")
        .style("opacity", 0);

    // draw initial bars
    updateChart();
}

function updateChart() {
    const aggregated = getAggregatedData(props.data);

    // update scales
    x.domain(d3.extent(aggregated, (d) => d.name) as [number, number]);
    y.domain([0, d3.max(aggregated, (d) => d.value) ?? 0]).nice();

    const bars = svg
        .select(".bars")
        .selectAll<SVGRectElement, ChartDataItem>(".bar")
        .data(aggregated, (d) => d.name.toString());

    const midIndex = Math.floor(aggregated.length / 2);
    const barWidth = Math.max(1, width / aggregated.length);

    // ENTER
    bars
        .enter()
        .append("rect")
        .attr("class", "bar")
        .attr("x", (d) => x(d.name))
        .attr("width", barWidth)
        .attr("fill", (_, i) => (i < midIndex ? "green" : "red"))
        .attr("y", y(0))
        .attr("height", 0)
        .on("mouseover", function (event, d) {
        tooltip
            .style("opacity", 1)
            .html(`Price: ${d.name}<br/>Cumulative: ${d.value} BTC`);
        })
        .on("mousemove", function (event) {
        tooltip
            .style("left", event.layerX + TOOLTIP_OFFSET.left + "px")
            .style("top", event.layerY - TOOLTIP_OFFSET.top + "px");
        })
        .on("mouseout", function () {
        tooltip.style("opacity", 0);
        })
        // ENTER + UPDATE
        .merge(bars as any)
        .transition()
        .duration(200)
        .attr("x", (d) => x(d.name))
        .attr("y", (d) => y(d.value))
        .attr("width", barWidth)
        .attr("height", (d) => height - y(d.value))
        .attr("fill", (_, i) => (i < midIndex ? "green" : "red"));

    // EXIT
    bars.exit().remove();

    // update axes
    svg.select(".x-axis").transition().duration(200).call(d3.axisBottom(x) as any);
    svg.select(".y-axis").transition().duration(200).call(d3.axisLeft(y) as any);
}
</script>

<template>
    <div>
        <div class="flex items-center gap-2 my-2">
            <label for="entries" class="text-lg">Entries to show:</label>
            <select id="entries" v-model="entriesToShow" class="border rounded px-2 py-1">
                <option :value="10">10</option>
                <option :value="20">20</option>
                <option :value="30">30</option>
                <option :value="40">40</option>
                <option :value="60">60</option>
                <option :value="80">80</option>
                <option :value="100">All</option>
            </select>
        </div>
        <div ref="chartContainer" class="w-full h-[400px] relative"></div>
    </div>
</template>

<style scoped>
    .bar {
        cursor: pointer;
    }
    .x-axis path,
    .x-axis line,
    .y-axis path,
    .y-axis line {
        shape-rendering: crispEdges;
    }
    .x-axis text,
    .y-axis text {
        font-size: 12px;
    }
</style>
