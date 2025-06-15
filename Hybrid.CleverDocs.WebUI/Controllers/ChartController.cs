using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs.WebUI.Helpers.Charts;
using Hybrid.CleverDocs.WebUI.Services;

namespace Hybrid.CleverDocs.WebUI.Controllers
{
    /// <summary>
    /// Controller for providing chart data endpoints
    /// Supports admin, company, and user role-based charts
    /// </summary>
    [Authorize]
    [Route("api/charts")]
    public class ChartController : ControllerBase
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ChartController> _logger;

        public ChartController(IApiService apiService, ILogger<ChartController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Get user growth chart data (Admin only)
        /// </summary>
        [HttpGet("user-growth")]
        [Authorize(Roles = "1")] // Admin only
        public async Task<IActionResult> GetUserGrowthChart()
        {
            try
            {
                // Generate sample data - replace with real API calls
                var labels = GetLast12Months();
                var userData = await GenerateUserGrowthData();

                var dataset = new ChartDataset
                {
                    Label = "New Users",
                    Data = userData,
                    BorderColor = "#e91e63",
                    BackgroundColor = "rgba(233, 30, 99, 0.1)",
                    Fill = true,
                    Tension = 0.4
                };

                var chartConfig = ChartDataHelper.CreateLineChart(
                    labels,
                    new[] { dataset }
                );

                return Ok(ChartDataHelper.ToJson(chartConfig));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user growth chart");
                return StatusCode(500, "Error generating chart data");
            }
        }

        /// <summary>
        /// Get document types distribution (Admin/Company)
        /// </summary>
        [HttpGet("document-types")]
        [Authorize(Roles = "1,2")] // Admin and Company
        public async Task<IActionResult> GetDocumentTypesChart()
        {
            try
            {
                var labels = new[] { "PDF", "Word", "Excel", "PowerPoint", "Text", "Other" };
                var data = await GenerateDocumentTypesData();
                var colors = new[]
                {
                    "#e91e63", "#2196f3", "#4caf50", 
                    "#ff9800", "#9c27b0", "#607d8b"
                };

                var chartConfig = ChartDataHelper.CreateDoughnutChart(
                    labels,
                    data,
                    colors
                );

                return Ok(ChartDataHelper.ToJson(chartConfig));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document types chart");
                return StatusCode(500, "Error generating chart data");
            }
        }

        /// <summary>
        /// Get company activity chart (Admin only)
        /// </summary>
        [HttpGet("company-activity")]
        [Authorize(Roles = "1")] // Admin only
        public async Task<IActionResult> GetCompanyActivityChart()
        {
            try
            {
                var labels = GetLast7Days();
                var datasets = await GenerateCompanyActivityData();

                var chartConfig = ChartDataHelper.CreateBarChart(
                    labels,
                    datasets
                );

                return Ok(ChartDataHelper.ToJson(chartConfig));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating company activity chart");
                return StatusCode(500, "Error generating chart data");
            }
        }

        /// <summary>
        /// Get user activity chart (All roles)
        /// </summary>
        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivityChart()
        {
            try
            {
                var labels = GetLast30Days();
                var activityData = await GenerateUserActivityData();

                var dataset = new ChartDataset
                {
                    Label = "Daily Activity",
                    Data = activityData,
                    BorderColor = "#4caf50",
                    BackgroundColor = "rgba(76, 175, 80, 0.1)",
                    Fill = true,
                    Tension = 0.3
                };

                var chartConfig = ChartDataHelper.CreateLineChart(
                    labels,
                    new[] { dataset }
                );

                return Ok(ChartDataHelper.ToJson(chartConfig));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user activity chart");
                return StatusCode(500, "Error generating chart data");
            }
        }

        /// <summary>
        /// Get storage usage chart (Admin/Company)
        /// </summary>
        [HttpGet("storage-usage")]
        [Authorize(Roles = "1,2")] // Admin and Company
        public async Task<IActionResult> GetStorageUsageChart()
        {
            try
            {
                var labels = new[] { "Used", "Available" };
                var data = await GenerateStorageUsageData();
                var colors = new[] { "#f44336", "#e0e0e0" };

                var chartConfig = ChartDataHelper.CreatePieChart(
                    labels,
                    data,
                    colors
                );

                return Ok(ChartDataHelper.ToJson(chartConfig));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating storage usage chart");
                return StatusCode(500, "Error generating chart data");
            }
        }

        #region Private Helper Methods

        private string[] GetLast12Months()
        {
            var months = new List<string>();
            for (int i = 11; i >= 0; i--)
            {
                months.Add(DateTime.Now.AddMonths(-i).ToString("MMM yyyy"));
            }
            return months.ToArray();
        }

        private string[] GetLast7Days()
        {
            var days = new List<string>();
            for (int i = 6; i >= 0; i--)
            {
                days.Add(DateTime.Now.AddDays(-i).ToString("ddd"));
            }
            return days.ToArray();
        }

        private string[] GetLast30Days()
        {
            var days = new List<string>();
            for (int i = 29; i >= 0; i--)
            {
                days.Add(DateTime.Now.AddDays(-i).ToString("MM/dd"));
            }
            return days.ToArray();
        }

        private async Task<double[]> GenerateUserGrowthData()
        {
            // TODO: Replace with real API calls
            var random = new Random();
            return Enumerable.Range(0, 12)
                .Select(_ => random.NextDouble() * 50 + 10)
                .ToArray();
        }

        private async Task<double[]> GenerateDocumentTypesData()
        {
            // TODO: Replace with real API calls
            return new double[] { 45, 25, 15, 8, 5, 2 };
        }

        private async Task<ChartDataset[]> GenerateCompanyActivityData()
        {
            // TODO: Replace with real API calls
            var random = new Random();
            
            return new[]
            {
                new ChartDataset
                {
                    Label = "Logins",
                    Data = Enumerable.Range(0, 7).Select(_ => random.NextDouble() * 100).ToArray(),
                    BackgroundColor = "#e91e63",
                    BorderColor = "#e91e63"
                },
                new ChartDataset
                {
                    Label = "Documents",
                    Data = Enumerable.Range(0, 7).Select(_ => random.NextDouble() * 50).ToArray(),
                    BackgroundColor = "#2196f3",
                    BorderColor = "#2196f3"
                }
            };
        }

        private async Task<double[]> GenerateUserActivityData()
        {
            // TODO: Replace with real API calls
            var random = new Random();
            return Enumerable.Range(0, 30)
                .Select(_ => random.NextDouble() * 20 + 5)
                .ToArray();
        }

        private async Task<double[]> GenerateStorageUsageData()
        {
            // TODO: Replace with real API calls
            return new double[] { 65, 35 }; // 65% used, 35% available
        }

        #endregion
    }
}
