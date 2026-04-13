<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions,
} from "chart.js";

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
);

interface Props {
  labels: string[];
  datasets: Array<{
    label: string;
    data: number[];
    backgroundColor?: string | string[];
    borderColor?: string | string[];
    borderWidth?: number;
  }>;
  title?: string;
  showLegend?: boolean;
  showGrid?: boolean;
  horizontal?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  title: undefined,
  showLegend: true,
  showGrid: true,
  horizontal: false,
});

const defaultColors = [
  "rgba(59, 130, 246, 0.8)",
  "rgba(239, 68, 68, 0.8)",
  "rgba(34, 197, 94, 0.8)",
  "rgba(245, 158, 11, 0.8)",
  "rgba(168, 85, 247, 0.8)",
  "rgba(236, 72, 153, 0.8)",
];

const chartData = computed<ChartData<"bar">>(() => ({
  labels: props.labels,
  datasets: props.datasets.map((dataset, index) => ({
    label: dataset.label,
    data: dataset.data,
    backgroundColor:
      dataset.backgroundColor || defaultColors[index % defaultColors.length],
    borderColor:
      dataset.borderColor ||
      defaultColors[index % defaultColors.length]!.replace("0.8", "1"),
    borderWidth: dataset.borderWidth ?? 1,
  })),
}));

const chartOptions = computed<ChartOptions<"bar">>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: props.horizontal ? "y" : "x",
  plugins: {
    legend: {
      display: props.showLegend,
      position: "top" as const,
    },
    title: {
      display: !!props.title,
      text: props.title,
    },
  },
  scales: {
    y: {
      beginAtZero: true,
      grid: {
        display: props.showGrid,
      },
    },
    x: {
      grid: {
        display: props.showGrid,
      },
    },
  },
}));
</script>

<template>
  <Bar :data="chartData" :options="chartOptions" />
</template>
