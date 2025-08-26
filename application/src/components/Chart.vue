<script setup>
import { onMounted, ref, watch } from "vue";
import * as d3 from "d3";
import { formatMoney } from "@/utils";

const props = defineProps(["data"]);
const chartContainer = ref(null);
let svg, x, y, width, height, tooltip;

onMounted(() => {
    drawChart();
});

watch(
    () => props.data,
    (newValue) => {
        if (!svg && newValue?.length > 0) {
            drawChart();
        } else if (svg) {
            updateChart();
        }
    }
);

function drawChart() {
    const margin = { top: 20, right: 40, bottom: 40, left: 40 };
        width = document.documentElement.clientWidth - margin.left - margin.right;
        height = 400 - margin.top - margin.bottom;

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
        .domain(d3.extent(props.data, (d) => +d.name)) // min → max price
        .range([0, width]);

    y = d3
        .scaleLinear()
        .domain([0, d3.max(props.data, (d) => d.value)])
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
    // update scales
    x.domain(d3.extent(props.data, (d) => +d.name));
    y.domain([0, d3.max(props.data, (d) => d.value)]).nice();

    const bars = svg
        .select(".bars")
        .selectAll(".bar")
        .data(props.data, (d) => d.name);

    const midIndex = Math.floor(props.data.length / 2);
    const barWidth = Math.max(1, width / props.data.length);

    // ENTER
    bars
        .enter()
        .append("rect")
        .attr("class", "bar")
        .attr("x", (d) => x(d.name))
        .attr("width", barWidth)
        .attr("fill", (d, i) => (i < midIndex ? "green" : "red"))
        .attr("y", y(0))
        .attr("height", 0)
        // tooltip events
        .on("mouseover", function (event, d) {
            tooltip.style("opacity", 1).html(
                `Price: ${formatMoney(d.name)}<br/>Amount: ${d.value} BTC`
        );
        })
        .on("mousemove", function (event) {
            tooltip
                .style("left", event.layerX + 10 + "px")
                .style("top", event.layerY - 20 + "px");
        })
        .on("mouseout", function () {
            tooltip.style("opacity", 0);
        })
        .merge(bars) // ENTER + UPDATE
        .transition()
        .duration(200)
        .attr("x", (d) => x(d.name))
        .attr("y", (d) => y(d.value))
        .attr("width", barWidth)
        .attr("height", (d) => height - y(d.value))
        .attr("fill", (d, i) => (i < midIndex ? "green" : "red"));

    // EXIT
    bars.exit().remove();

    // update axes
    svg.select(".x-axis").transition().duration(200).call(d3.axisBottom(x));
    svg.select(".y-axis").transition().duration(200).call(d3.axisLeft(y));
}
</script>

<template>
    <div ref="chartContainer" class="w-full h-[400px] relative"></div>
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
