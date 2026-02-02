<template>
  <Line :data="chartData" :options="chartOptions" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Line } from 'vue-chartjs';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  type ChartData,
  type ChartOptions
} from 'chart.js';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

interface Props {
  labels: string[];
  datasets: Array<{
    label: string;
    data: number[];
    borderColor?: string;
    backgroundColor?: string;
    fill?: boolean;
    tension?: number;
  }>;
  title?: string;
  showLegend?: boolean;
  showGrid?: boolean;
  smooth?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  title: undefined,
  showLegend: true,
  showGrid: true,
  smooth: true
});

const defaultColors = [
  'rgb(59, 130, 246)',
  'rgb(239, 68, 68)',
  'rgb(34, 197, 94)',
  'rgb(245, 158, 11)',
  'rgb(168, 85, 247)',
  'rgb(236, 72, 153)',
];

const chartData = computed<ChartData<'line'>>(() => ({
  labels: props.labels,
  datasets: props.datasets.map((dataset, index) => ({
    label: dataset.label,
    data: dataset.data,
    borderColor: dataset.borderColor || defaultColors[index % defaultColors.length],
    backgroundColor: dataset.backgroundColor || `${defaultColors[index % defaultColors.length]}33`,
    fill: dataset.fill ?? false,
    tension: dataset.tension ?? (props.smooth ? 0.4 : 0)
  }))
}));

const chartOptions = computed<ChartOptions<'line'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: props.showLegend,
      position: 'top' as const,
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
        display: props.showGrid
      }
    },
    x: {
      grid: {
        display: props.showGrid
      }
    }
  }
}));
</script>
