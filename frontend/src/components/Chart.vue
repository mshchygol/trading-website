<script setup>
import { ref, watch } from "vue";
import * as d3 from 'd3';

const props = defineProps(['data'])
const chartContainer = ref(null);
let svg, x, y, width, height;


watch(() => props.data, (newValue, oldValue) => {
    console.log('Chart data prop updated')
    if (!svg && newValue?.length > 0) {
        drawChart()
    } else if (svg) {
        updateChart()
    }
});

function drawChart() {
  const margin = { top: 20, right: 30, bottom: 40, left: 40 };
  width = 800 - margin.left - margin.right;
  height = 400 - margin.top - margin.bottom;

  svg = d3
    .select(chartContainer.value)
    .append("svg")
    .attr("width", width + margin.left + margin.right)
    .attr("height", height + margin.top + margin.bottom)
    .append("g")
    .attr("transform", `translate(${margin.left},${margin.top})`);

  // scales
  x = d3
    .scaleBand()
    .domain(props.data.map((d) => d.name))
    .range([0, width])
    .padding(0.1);

  y = d3
    .scaleLinear()
    .domain([0, d3.max(props.data, (d) => d.value)])
    .nice()
    .range([height, 0]);

  // bars group
  svg.append("g").attr("class", "bars");

  // axes
  svg.append("g").attr("class", "x-axis").attr("transform", `translate(0,${height})`).call(d3.axisBottom(x));
  svg.append("g").attr("class", "y-axis").call(d3.axisLeft(y));

  // draw initial bars
  updateChart();
}

function updateChart() {
  // update scales
  x.domain(props.data.map((d) => d.name));
  y.domain([0, d3.max(props.data, (d) => d.value)]).nice();

  const bars = svg
    .select(".bars")
    .selectAll(".bar")
    .data(props.data, (d) => d.name);

  // ENTER
  bars
    .enter()
    .append("rect")
    .attr("class", "bar")
    .attr("x", (d) => x(d.name))
    .attr("width", x.bandwidth())
    .attr("fill", "steelblue")
    .attr("y", y(0))
    .attr("height", 0)
    .merge(bars) // ENTER + UPDATE
    .transition()
    .duration(500)
    .attr("x", (d) => x(d.name))
    .attr("y", (d) => y(d.value))
    .attr("width", x.bandwidth())
    .attr("height", (d) => height - y(d.value));

  // EXIT
  bars.exit().remove();

  // update axes
  svg.select(".x-axis").transition().duration(200).call(d3.axisBottom(x));
  svg.select(".y-axis").transition().duration(200).call(d3.axisLeft(y));
}

</script>

<template>
  <div ref="chartContainer" class="w-full h-[400px]"></div>
  <div>{{ props.message?.data?.timestamp }}</div>
  <!-- <div>{{ data }}</div> -->
</template>

<style scoped>
    .bar {
        fill: steelblue;
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