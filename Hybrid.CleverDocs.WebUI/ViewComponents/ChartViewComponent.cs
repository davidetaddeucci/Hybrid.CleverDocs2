using Microsoft.AspNetCore.Mvc;

namespace Hybrid.CleverDocs.WebUI.ViewComponents
{
    /// <summary>
    /// Chart View Component for displaying Chart.js charts
    /// Compatible with Material Dashboard theme and MVC architecture
    /// </summary>
    public class ChartViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ChartModel model)
        {
            return View(model);
        }
    }

    /// <summary>
    /// Model for Chart component
    /// </summary>
    public class ChartModel
    {
        /// <summary>
        /// Chart title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Chart type: line, bar, pie, doughnut, area
        /// </summary>
        public string Type { get; set; } = "line";

        /// <summary>
        /// API endpoint to fetch chart data
        /// </summary>
        public string DataUrl { get; set; } = string.Empty;

        /// <summary>
        /// Chart height in pixels
        /// </summary>
        public int Height { get; set; } = 300;

        /// <summary>
        /// Chart width (responsive by default)
        /// </summary>
        public string Width { get; set; } = "100%";

        /// <summary>
        /// Card color theme: primary, success, warning, danger, info, dark
        /// </summary>
        public string Color { get; set; } = "primary";

        /// <summary>
        /// Show loading spinner
        /// </summary>
        public bool ShowLoading { get; set; } = true;

        /// <summary>
        /// Show export buttons
        /// </summary>
        public bool ShowExport { get; set; } = false;

        /// <summary>
        /// Additional CSS classes
        /// </summary>
        public string? CssClass { get; set; }

        /// <summary>
        /// Chart container ID (auto-generated if not provided)
        /// </summary>
        public string? ChartId { get; set; }

        /// <summary>
        /// Refresh interval in seconds (0 = no auto-refresh)
        /// </summary>
        public int RefreshInterval { get; set; } = 0;

        /// <summary>
        /// Show chart legend
        /// </summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// Chart description for accessibility
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Custom chart options (JSON string)
        /// </summary>
        public string? CustomOptions { get; set; }

        /// <summary>
        /// Error message to display if chart fails to load
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Chart size: sm, md, lg, xl
        /// </summary>
        public string Size { get; set; } = "md";

        /// <summary>
        /// Enable click events
        /// </summary>
        public bool EnableClick { get; set; } = false;

        /// <summary>
        /// Click handler JavaScript function name
        /// </summary>
        public string? ClickHandler { get; set; }

        /// <summary>
        /// Chart animation duration in milliseconds
        /// </summary>
        public int AnimationDuration { get; set; } = 1000;

        /// <summary>
        /// Show chart toolbar
        /// </summary>
        public bool ShowToolbar { get; set; } = false;

        /// <summary>
        /// Chart theme: light, dark, auto
        /// </summary>
        public string Theme { get; set; } = "auto";
    }
}
