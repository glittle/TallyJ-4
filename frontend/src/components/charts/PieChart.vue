<template>
  <Pie :data="chartData" :options="chartOptions" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Pie } from 'vue-chartjs';
import {
  Chart as ChartJS,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  type ChartData,
  type ChartOptions
} from 'chart.js';

ChartJS.register(
  ArcElement,
  Title,
  Tooltip,
  Legend
);

interface Props {
  labels: string[];
  data: number[];
  title?: string;
  showLegend?: boolean;
  backgroundColor?: string[];
}

const props = withDefaults(defineProps<Props>(), {
  title: undefined,
  showLegend: true,
  backgroundColor: undefined
});

const defaultColors = [
  'rgba(59, 130, 246, 0.8)',
  'rgba(239, 68, 68, 0.8)',
  'rgba(34, 197, 94, 0.8)',
  'rgba(245, 158, 11, 0.8)',
  'rgba(168, 85, 247, 0.8)',
  'rgba(236, 72, 153, 0.8)',
  'rgba(20, 184, 166, 0.8)',
  'rgba(251, 146, 60, 0.8)',
];

const chartData = computed<ChartData<'pie'>>(() => ({
  labels: props.labels,
  datasets: [{
    data: props.data,
    backgroundColor: props.backgroundColor || defaultColors.slice(0, props.data.length),
    borderColor: (props.backgroundColor || defaultColors.slice(0, props.data.length)).map(color => 
      color.replace('0.8', '1')
    ),
    borderWidth: 2
  }]
}));

const chartOptions = computed<ChartOptions<'pie'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: props.showLegend,
      position: 'right' as const,
    },
    title: {
      display: !!props.title,
      text: props.title,
    },
  }
}));
</script>
