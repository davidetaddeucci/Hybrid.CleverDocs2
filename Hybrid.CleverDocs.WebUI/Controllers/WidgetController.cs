using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Services;
using System.Text.Json;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    /// <summary>
    /// Controller for managing dashboard widgets
    /// Supports drag-and-drop positioning and user preferences
    /// </summary>
    [Authorize]
    [Route("api/widgets")]
    public class WidgetController : ControllerBase
    {
        private readonly IApiService _apiService;
        private readonly ILogger<WidgetController> _logger;

        public WidgetController(IApiService apiService, ILogger<WidgetController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's dashboard widget configuration
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardWidgets()
        {
            try
            {
                // For now, return default widget configuration
                // TODO: Implement API call to get user's saved widgets
                var defaultWidgets = GetDefaultWidgetConfiguration();
                return Ok(defaultWidgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard widgets");
                return StatusCode(500, "Error loading dashboard widgets");
            }
        }

        /// <summary>
        /// Save user's dashboard widget configuration
        /// </summary>
        [HttpPost("dashboard")]
        public async Task<IActionResult> SaveDashboardWidgets([FromBody] WidgetConfiguration[] widgets)
        {
            try
            {
                // TODO: Implement API call to save user's widgets
                _logger.LogInformation($"Saving {widgets.Length} widgets for user");
                
                // For now, just return success
                return Ok(new { message = "Widgets saved successfully", count = widgets.Length });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving dashboard widgets");
                return StatusCode(500, "Error saving dashboard widgets");
            }
        }

        /// <summary>
        /// Get available widget templates
        /// </summary>
        [HttpGet("templates")]
        public async Task<IActionResult> GetWidgetTemplates()
        {
            try
            {
                var templates = GetAvailableTemplates();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting widget templates");
                return StatusCode(500, "Error loading widget templates");
            }
        }

        /// <summary>
        /// Update widget position
        /// </summary>
        [HttpPut("{widgetId}/position")]
        public async Task<IActionResult> UpdateWidgetPosition(string widgetId, [FromBody] WidgetPosition position)
        {
            try
            {
                // TODO: Implement API call to update widget position
                _logger.LogInformation($"Updating position for widget {widgetId}: X={position.X}, Y={position.Y}");
                
                return Ok(new { message = "Widget position updated", widgetId, position });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating widget position");
                return StatusCode(500, "Error updating widget position");
            }
        }

        /// <summary>
        /// Toggle widget visibility
        /// </summary>
        [HttpPut("{widgetId}/visibility")]
        public async Task<IActionResult> ToggleWidgetVisibility(string widgetId, [FromBody] WidgetVisibility visibility)
        {
            try
            {
                // TODO: Implement API call to toggle widget visibility
                _logger.LogInformation($"Toggling visibility for widget {widgetId}: {visibility.IsVisible}");
                
                return Ok(new { message = "Widget visibility updated", widgetId, visibility.IsVisible });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating widget visibility");
                return StatusCode(500, "Error updating widget visibility");
            }
        }

        #region Private Helper Methods

        private WidgetConfiguration[] GetDefaultWidgetConfiguration()
        {
            return new[]
            {
                new WidgetConfiguration
                {
                    Id = "stat-companies",
                    Type = "StatCard",
                    Title = "Total Companies",
                    X = 0, Y = 0, Width = 3, Height = 1,
                    Configuration = JsonSerializer.Serialize(new
                    {
                        dataUrl = "/api/admin/companies/count",
                        icon = "business",
                        color = "primary",
                        trend = "+12.5%"
                    }),
                    IsVisible = true
                },
                new WidgetConfiguration
                {
                    Id = "stat-users",
                    Type = "StatCard", 
                    Title = "Total Users",
                    X = 3, Y = 0, Width = 3, Height = 1,
                    Configuration = JsonSerializer.Serialize(new
                    {
                        dataUrl = "/api/admin/users/count",
                        icon = "people",
                        color = "info",
                        trend = "+8.2%"
                    }),
                    IsVisible = true
                },
                new WidgetConfiguration
                {
                    Id = "chart-user-growth",
                    Type = "Chart",
                    Title = "User Growth",
                    X = 0, Y = 1, Width = 8, Height = 2,
                    Configuration = JsonSerializer.Serialize(new
                    {
                        chartType = "line",
                        dataUrl = "/api/charts/user-growth",
                        showExport = true
                    }),
                    IsVisible = true
                },
                new WidgetConfiguration
                {
                    Id = "chart-document-types",
                    Type = "Chart",
                    Title = "Document Types",
                    X = 8, Y = 1, Width = 4, Height = 2,
                    Configuration = JsonSerializer.Serialize(new
                    {
                        chartType = "doughnut",
                        dataUrl = "/api/charts/document-types"
                    }),
                    IsVisible = true
                }
            };
        }

        private WidgetTemplate[] GetAvailableTemplates()
        {
            return new[]
            {
                new WidgetTemplate
                {
                    Id = "stat-card",
                    Name = "Statistic Card",
                    Type = "StatCard",
                    Category = "Statistics",
                    Icon = "analytics",
                    Description = "Display key metrics with trend indicators",
                    DefaultWidth = 3,
                    DefaultHeight = 1,
                    MinimumRole = 3
                },
                new WidgetTemplate
                {
                    Id = "line-chart",
                    Name = "Line Chart",
                    Type = "Chart",
                    Category = "Charts",
                    Icon = "show_chart",
                    Description = "Display trends over time",
                    DefaultWidth = 6,
                    DefaultHeight = 2,
                    MinimumRole = 3
                },
                new WidgetTemplate
                {
                    Id = "bar-chart",
                    Name = "Bar Chart",
                    Type = "Chart",
                    Category = "Charts",
                    Icon = "bar_chart",
                    Description = "Compare values across categories",
                    DefaultWidth = 6,
                    DefaultHeight = 2,
                    MinimumRole = 3
                },
                new WidgetTemplate
                {
                    Id = "pie-chart",
                    Name = "Pie Chart",
                    Type = "Chart",
                    Category = "Charts",
                    Icon = "pie_chart",
                    Description = "Show proportional data",
                    DefaultWidth = 4,
                    DefaultHeight = 2,
                    MinimumRole = 3
                }
            };
        }

        #endregion
    }

    #region DTOs

    public class WidgetConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Configuration { get; set; } = "{}";
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; }
    }

    public class WidgetPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class WidgetVisibility
    {
        public bool IsVisible { get; set; }
    }

    public class WidgetTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DefaultWidth { get; set; }
        public int DefaultHeight { get; set; }
        public int MinimumRole { get; set; }
    }

    #endregion
}
