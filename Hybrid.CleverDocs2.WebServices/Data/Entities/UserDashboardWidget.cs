using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    /// <summary>
    /// User dashboard widget configuration for customizable dashboards
    /// Supports drag-and-drop positioning and role-based visibility
    /// </summary>
    [Table("UserDashboardWidgets")]
    public class UserDashboardWidget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User who owns this widget configuration
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Company context for multi-tenant isolation
        /// </summary>
        [Required]
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Widget type identifier (StatCard, Chart, Table, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string WidgetType { get; set; } = string.Empty;

        /// <summary>
        /// Unique widget identifier within the dashboard
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string WidgetId { get; set; } = string.Empty;

        /// <summary>
        /// Widget display title
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Widget configuration as JSON
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string Configuration { get; set; } = "{}";

        /// <summary>
        /// Grid position X coordinate
        /// </summary>
        public int PositionX { get; set; } = 0;

        /// <summary>
        /// Grid position Y coordinate
        /// </summary>
        public int PositionY { get; set; } = 0;

        /// <summary>
        /// Widget width in grid units
        /// </summary>
        public int Width { get; set; } = 1;

        /// <summary>
        /// Widget height in grid units
        /// </summary>
        public int Height { get; set; } = 1;

        /// <summary>
        /// Display order for widgets in same position
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Whether widget is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether widget is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Minimum role required to view this widget (1=Admin, 2=Company, 3=User)
        /// </summary>
        public int MinimumRole { get; set; } = 3;

        /// <summary>
        /// Widget refresh interval in seconds (0 = no auto-refresh)
        /// </summary>
        public int RefreshInterval { get; set; } = 0;

        /// <summary>
        /// Widget theme/color scheme
        /// </summary>
        [MaxLength(20)]
        public string Theme { get; set; } = "primary";

        /// <summary>
        /// Custom CSS classes
        /// </summary>
        [MaxLength(500)]
        public string? CssClasses { get; set; }

        /// <summary>
        /// Widget data source URL or endpoint
        /// </summary>
        [MaxLength(500)]
        public string? DataSource { get; set; }

        /// <summary>
        /// Cache TTL for widget data in seconds
        /// </summary>
        public int CacheTtl { get; set; } = 300; // 5 minutes default

        /// <summary>
        /// Whether widget supports export functionality
        /// </summary>
        public bool SupportsExport { get; set; } = false;

        /// <summary>
        /// Whether widget supports click interactions
        /// </summary>
        public bool SupportsClick { get; set; } = false;

        /// <summary>
        /// Click action URL or JavaScript function
        /// </summary>
        [MaxLength(500)]
        public string? ClickAction { get; set; }

        /// <summary>
        /// Widget description for accessibility
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this widget
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User who last updated this widget
        /// </summary>
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Company Company { get; set; } = null!;
    }

    /// <summary>
    /// Widget template for creating new widgets
    /// </summary>
    [Table("WidgetTemplates")]
    public class WidgetTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Template name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Widget type identifier
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string WidgetType { get; set; } = string.Empty;

        /// <summary>
        /// Template description
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Default configuration as JSON
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string DefaultConfiguration { get; set; } = "{}";

        /// <summary>
        /// Default width in grid units
        /// </summary>
        public int DefaultWidth { get; set; } = 1;

        /// <summary>
        /// Default height in grid units
        /// </summary>
        public int DefaultHeight { get; set; } = 1;

        /// <summary>
        /// Minimum role required (1=Admin, 2=Company, 3=User)
        /// </summary>
        public int MinimumRole { get; set; } = 3;

        /// <summary>
        /// Template category
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; } = "General";

        /// <summary>
        /// Template icon
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "widgets";

        /// <summary>
        /// Whether template is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
