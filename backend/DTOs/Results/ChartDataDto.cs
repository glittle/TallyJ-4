namespace TallyJ4.DTOs.Results;

/// <summary>
/// Data structure for chart visualizations
/// </summary>
public class ChartDataDto
{
    /// <summary>
    /// The type of chart to display (bar, pie, line, doughnut).
    /// </summary>
    public string ChartType { get; set; } = string.Empty; // "bar", "pie", "line", "doughnut"

    /// <summary>
    /// The title of the chart.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Labels for the chart data points.
    /// </summary>
    public List<string> Labels { get; set; } = new();

    /// <summary>
    /// Datasets containing the chart data.
    /// </summary>
    public List<ChartDatasetDto> Datasets { get; set; } = new();

    /// <summary>
    /// Chart configuration options.
    /// </summary>
    public ChartOptionsDto Options { get; set; } = new();
}

/// <summary>
/// Represents a dataset for chart visualization.
/// </summary>
public class ChartDatasetDto
{
    /// <summary>
    /// The label for this dataset.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The data values for this dataset.
    /// </summary>
    public List<decimal> Data { get; set; } = new();

    /// <summary>
    /// Background colors for the data points.
    /// </summary>
    public List<string> BackgroundColors { get; set; } = new();

    /// <summary>
    /// Border colors for the data points.
    /// </summary>
    public List<string> BorderColors { get; set; } = new();

    /// <summary>
    /// Width of the border around data points.
    /// </summary>
    public int BorderWidth { get; set; } = 1;
}

/// <summary>
/// Chart configuration options.
/// </summary>
public class ChartOptionsDto
{
    /// <summary>
    /// Whether the chart should be responsive to container size changes.
    /// </summary>
    public bool Responsive { get; set; } = true;

    /// <summary>
    /// Plugin configurations for the chart.
    /// </summary>
    public ChartPluginsDto Plugins { get; set; } = new();

    /// <summary>
    /// Scale configurations for the chart axes.
    /// </summary>
    public ChartScalesDto Scales { get; set; } = new();
}

/// <summary>
/// Plugin configurations for chart elements.
/// </summary>
public class ChartPluginsDto
{
    /// <summary>
    /// Legend configuration.
    /// </summary>
    public ChartLegendDto Legend { get; set; } = new();

    /// <summary>
    /// Title configuration.
    /// </summary>
    public ChartTitleDto Title { get; set; } = new();
}

/// <summary>
/// Legend configuration options.
/// </summary>
public class ChartLegendDto
{
    /// <summary>
    /// Position of the legend (top, bottom, left, right).
    /// </summary>
    public string Position { get; set; } = "top";

    /// <summary>
    /// Whether to display the legend.
    /// </summary>
    public bool Display { get; set; } = true;
}

/// <summary>
/// Title configuration options.
/// </summary>
public class ChartTitleDto
{
    /// <summary>
    /// Whether to display the title.
    /// </summary>
    public bool Display { get; set; } = true;

    /// <summary>
    /// The text content of the title.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Scale configurations for chart axes.
/// </summary>
public class ChartScalesDto
{
    /// <summary>
    /// X-axis configuration.
    /// </summary>
    public ChartAxisDto X { get; set; } = new();

    /// <summary>
    /// Y-axis configuration.
    /// </summary>
    public ChartAxisDto Y { get; set; } = new();
}

/// <summary>
/// Axis configuration options.
/// </summary>
public class ChartAxisDto
{
    /// <summary>
    /// Whether to display the axis.
    /// </summary>
    public bool Display { get; set; } = true;

    /// <summary>
    /// The title text for the axis.
    /// </summary>
    public string Title { get; set; } = string.Empty;
}