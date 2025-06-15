using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Helpers.Charts
{
    /// <summary>
    /// Helper class for generating Chart.js compatible data structures
    /// Supports line, bar, pie, doughnut, and area charts
    /// </summary>
    public static class ChartDataHelper
    {
        /// <summary>
        /// Create a line chart configuration
        /// </summary>
        public static object CreateLineChart(
            string[] labels,
            ChartDataset[] datasets,
            ChartOptions? options = null)
        {
            return new
            {
                type = "line",
                data = new
                {
                    labels = labels,
                    datasets = datasets.Select(d => new
                    {
                        label = d.Label,
                        data = d.Data,
                        borderColor = d.BorderColor,
                        backgroundColor = d.BackgroundColor,
                        borderWidth = d.BorderWidth,
                        fill = d.Fill,
                        tension = d.Tension
                    })
                },
                options = options ?? GetDefaultLineOptions()
            };
        }

        /// <summary>
        /// Create a bar chart configuration
        /// </summary>
        public static object CreateBarChart(
            string[] labels,
            ChartDataset[] datasets,
            ChartOptions? options = null)
        {
            return new
            {
                type = "bar",
                data = new
                {
                    labels = labels,
                    datasets = datasets.Select(d => new
                    {
                        label = d.Label,
                        data = d.Data,
                        backgroundColor = d.BackgroundColor,
                        borderColor = d.BorderColor,
                        borderWidth = d.BorderWidth
                    })
                },
                options = options ?? GetDefaultBarOptions()
            };
        }

        /// <summary>
        /// Create a pie chart configuration
        /// </summary>
        public static object CreatePieChart(
            string[] labels,
            double[] data,
            string[] colors,
            ChartOptions? options = null)
        {
            return new
            {
                type = "pie",
                data = new
                {
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            data = data,
                            backgroundColor = colors,
                            borderWidth = 2,
                            borderColor = "#ffffff"
                        }
                    }
                },
                options = options ?? GetDefaultPieOptions()
            };
        }

        /// <summary>
        /// Create a doughnut chart configuration
        /// </summary>
        public static object CreateDoughnutChart(
            string[] labels,
            double[] data,
            string[] colors,
            ChartOptions? options = null)
        {
            return new
            {
                type = "doughnut",
                data = new
                {
                    labels = labels,
                    datasets = new[]
                    {
                        new
                        {
                            data = data,
                            backgroundColor = colors,
                            borderWidth = 2,
                            borderColor = "#ffffff"
                        }
                    }
                },
                options = options ?? GetDefaultDoughnutOptions()
            };
        }

        /// <summary>
        /// Default options for line charts
        /// </summary>
        private static ChartOptions GetDefaultLineOptions()
        {
            return new ChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Scales = new
                {
                    y = new
                    {
                        beginAtZero = true,
                        grid = new { color = "rgba(0,0,0,0.1)" }
                    },
                    x = new
                    {
                        grid = new { color = "rgba(0,0,0,0.1)" }
                    }
                },
                Plugins = new
                {
                    legend = new
                    {
                        position = "top"
                    },
                    tooltip = new
                    {
                        mode = "index",
                        intersect = false
                    }
                },
                Interaction = new
                {
                    mode = "nearest",
                    axis = "x",
                    intersect = false
                }
            };
        }

        /// <summary>
        /// Default options for bar charts
        /// </summary>
        private static ChartOptions GetDefaultBarOptions()
        {
            return new ChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Scales = new
                {
                    y = new
                    {
                        beginAtZero = true,
                        grid = new { color = "rgba(0,0,0,0.1)" }
                    },
                    x = new
                    {
                        grid = new { display = false }
                    }
                },
                Plugins = new
                {
                    legend = new
                    {
                        position = "top"
                    }
                }
            };
        }

        /// <summary>
        /// Default options for pie charts
        /// </summary>
        private static ChartOptions GetDefaultPieOptions()
        {
            return new ChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Plugins = new
                {
                    legend = new
                    {
                        position = "right"
                    }
                }
            };
        }

        /// <summary>
        /// Default options for doughnut charts
        /// </summary>
        private static ChartOptions GetDefaultDoughnutOptions()
        {
            return new ChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Plugins = new
                {
                    legend = new
                    {
                        position = "bottom"
                    }
                }
            };
        }

        /// <summary>
        /// Convert object to JSON string for JavaScript
        /// </summary>
        public static string ToJson(object chartConfig)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            return JsonSerializer.Serialize(chartConfig, options);
        }
    }

    /// <summary>
    /// Chart dataset model
    /// </summary>
    public class ChartDataset
    {
        public string Label { get; set; } = string.Empty;
        public double[] Data { get; set; } = Array.Empty<double>();
        public string BorderColor { get; set; } = "#e91e63";
        public string BackgroundColor { get; set; } = "rgba(233, 30, 99, 0.1)";
        public int BorderWidth { get; set; } = 2;
        public bool Fill { get; set; } = false;
        public double Tension { get; set; } = 0.4;
    }

    /// <summary>
    /// Chart options model
    /// </summary>
    public class ChartOptions
    {
        public bool Responsive { get; set; } = true;
        public bool MaintainAspectRatio { get; set; } = false;
        public object? Scales { get; set; }
        public object? Plugins { get; set; }
        public object? Interaction { get; set; }
    }
}
