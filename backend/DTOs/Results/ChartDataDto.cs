namespace TallyJ4.DTOs.Results;

/// <summary>
/// Data structure for chart visualizations
/// </summary>
public class ChartDataDto
{
    public string ChartType { get; set; } = string.Empty; // "bar", "pie", "line", "doughnut"
    public string Title { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public List<ChartDatasetDto> Datasets { get; set; } = new();
    public ChartOptionsDto Options { get; set; } = new();
}

public class ChartDatasetDto
{
    public string Label { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
    public List<string> BackgroundColors { get; set; } = new();
    public List<string> BorderColors { get; set; } = new();
    public int BorderWidth { get; set; } = 1;
}

public class ChartOptionsDto
{
    public bool Responsive { get; set; } = true;
    public ChartPluginsDto Plugins { get; set; } = new();
    public ChartScalesDto Scales { get; set; } = new();
}

public class ChartPluginsDto
{
    public ChartLegendDto Legend { get; set; } = new();
    public ChartTitleDto Title { get; set; } = new();
}

public class ChartLegendDto
{
    public string Position { get; set; } = "top";
    public bool Display { get; set; } = true;
}

public class ChartTitleDto
{
    public bool Display { get; set; } = true;
    public string Text { get; set; } = string.Empty;
}

public class ChartScalesDto
{
    public ChartAxisDto X { get; set; } = new();
    public ChartAxisDto Y { get; set; } = new();
}

public class ChartAxisDto
{
    public bool Display { get; set; } = true;
    public string Title { get; set; } = string.Empty;
}