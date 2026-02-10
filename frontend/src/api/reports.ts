import api from '../services/api';

export interface ExportRequest {
  format: 'pdf' | 'excel' | 'csv';
  filters?: Record<string, any>;
}

export interface ChartData {
  chartType: string;
  title: string;
  labels: string[];
  datasets: ChartDataset[];
  options: ChartOptions;
}

export interface ChartDataset {
  label: string;
  data: number[];
  backgroundColors?: string[];
  borderColors?: string[];
  borderWidth?: number;
}

export interface ChartOptions {
  responsive: boolean;
  plugins: ChartPlugins;
  scales: ChartScales;
}

export interface ChartPlugins {
  legend: ChartLegend;
  title: ChartTitle;
}

export interface ChartLegend {
  position: string;
  display: boolean;
}

export interface ChartTitle {
  display: boolean;
  text: string;
}

export interface ChartScales {
  x: ChartAxis;
  y: ChartAxis;
}

export interface ChartAxis {
  display: boolean;
  title: string;
}

export const reportsApi = {
  async exportReport(electionId: string, request: ExportRequest): Promise<Blob> {
    const response = await api.post<Blob>(`/report-exports/${electionId}`, request, {
      responseType: 'blob'
    });
    return response.data;
  },

  async getChartData(electionId: string, chartType: string): Promise<ChartData> {
    const response = await api.get<ChartData>(`/advanced-reports/chart/${electionId}/${chartType}`);
    return response.data;
  }
};