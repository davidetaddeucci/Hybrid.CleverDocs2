using Microsoft.AspNetCore.Mvc;

namespace Hybrid.CleverDocs.WebUI.ViewComponents
{
    /// <summary>
    /// StatCard View Component for modern dashboard statistics display
    /// Compatible with Material Dashboard theme and MVC architecture
    /// </summary>
    public class StatCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(StatCardModel model)
        {
            return View(model);
        }
    }

    /// <summary>
    /// Model for StatCard component with animation and trend support
    /// </summary>
    public class StatCardModel
    {
        /// <summary>
        /// Card title (e.g., "Total Users", "Active Companies")
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Main statistic value to display
        /// </summary>
        public string Value { get; set; } = "0";

        /// <summary>
        /// Material Icons icon name (e.g., "people", "business", "description")
        /// </summary>
        public string Icon { get; set; } = "analytics";

        /// <summary>
        /// Card color theme: primary, success, warning, danger, info, dark
        /// </summary>
        public string Color { get; set; } = "primary";

        /// <summary>
        /// Trend percentage (e.g., "+12.5", "-3.2")
        /// </summary>
        public string? TrendPercentage { get; set; }

        /// <summary>
        /// Trend description (e.g., "vs last month", "since yesterday")
        /// </summary>
        public string? TrendDescription { get; set; }

        /// <summary>
        /// Trend direction: up, down, neutral
        /// </summary>
        public string TrendDirection { get; set; } = "neutral";

        /// <summary>
        /// Enable animated counter effect
        /// </summary>
        public bool AnimateCounter { get; set; } = true;

        /// <summary>
        /// Animation duration in milliseconds
        /// </summary>
        public int AnimationDuration { get; set; } = 2000;

        /// <summary>
        /// Optional click action URL
        /// </summary>
        public string? ActionUrl { get; set; }

        /// <summary>
        /// Additional CSS classes
        /// </summary>
        public string? CssClass { get; set; }

        /// <summary>
        /// Card size: sm, md, lg
        /// </summary>
        public string Size { get; set; } = "md";

        /// <summary>
        /// Show loading state
        /// </summary>
        public bool IsLoading { get; set; } = false;

        /// <summary>
        /// Error state message
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
