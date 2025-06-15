using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hybrid.CleverDocs.WebUI.Helpers
{
    /// <summary>
    /// Helper methods for navigation and active state management
    /// </summary>
    public static class NavigationHelper
    {
        /// <summary>
        /// Returns CSS class for active navigation item
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="action">Action name</param>
        /// <param name="controller">Controller name (optional)</param>
        /// <param name="activeClass">CSS class to apply when active</param>
        /// <returns>CSS class string</returns>
        public static string IsActive(this IHtmlHelper htmlHelper, string action, string controller = null, string activeClass = "active")
        {
            var currentController = htmlHelper.ViewContext.RouteData.Values["Controller"]?.ToString();
            var currentAction = htmlHelper.ViewContext.RouteData.Values["Action"]?.ToString();

            // If controller is not specified, use current controller
            if (string.IsNullOrEmpty(controller))
            {
                controller = currentController;
            }

            // Check if current route matches
            var isActive = string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase);

            return isActive ? activeClass : string.Empty;
        }

        /// <summary>
        /// Returns CSS class for active navigation item with custom active class
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="action">Action name</param>
        /// <param name="activeClass">CSS class to apply when active</param>
        /// <returns>CSS class string</returns>
        public static string IsActive(this IHtmlHelper htmlHelper, string action, string activeClass)
        {
            return htmlHelper.IsActive(action, null, activeClass);
        }

        /// <summary>
        /// Checks if current route matches any of the specified actions
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="actions">Array of action names</param>
        /// <param name="controller">Controller name (optional)</param>
        /// <param name="activeClass">CSS class to apply when active</param>
        /// <returns>CSS class string</returns>
        public static string IsActiveAny(this IHtmlHelper htmlHelper, string[] actions, string controller = null, string activeClass = "active")
        {
            var currentController = htmlHelper.ViewContext.RouteData.Values["Controller"]?.ToString();
            var currentAction = htmlHelper.ViewContext.RouteData.Values["Action"]?.ToString();

            // If controller is not specified, use current controller
            if (string.IsNullOrEmpty(controller))
            {
                controller = currentController;
            }

            // Check if current route matches any of the actions
            var isActive = actions.Any(action => 
                string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase);

            return isActive ? activeClass : string.Empty;
        }

        /// <summary>
        /// Gets the current user role for CSS class application
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <returns>Role string for CSS class</returns>
        public static string GetUserRoleClass(this IHtmlHelper htmlHelper)
        {
            var user = htmlHelper.ViewContext.HttpContext.User;
            
            if (user.IsInRole("Admin"))
                return "admin";
            else if (user.IsInRole("Company"))
                return "company";
            else if (user.IsInRole("User"))
                return "user";
            
            return "guest";
        }

        /// <summary>
        /// Checks if user has any of the specified roles
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="roles">Array of role names</param>
        /// <returns>True if user has any of the roles</returns>
        public static bool HasAnyRole(this IHtmlHelper htmlHelper, params string[] roles)
        {
            var user = htmlHelper.ViewContext.HttpContext.User;
            return roles.Any(role => user.IsInRole(role));
        }

        /// <summary>
        /// Gets the page title based on current route
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <returns>Page title string</returns>
        public static string GetPageTitle(this IHtmlHelper htmlHelper)
        {
            var controller = htmlHelper.ViewContext.RouteData.Values["Controller"]?.ToString();
            var action = htmlHelper.ViewContext.RouteData.Values["Action"]?.ToString();

            return $"{controller} - {action}";
        }

        /// <summary>
        /// Generates breadcrumb navigation
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <returns>Breadcrumb HTML string</returns>
        public static string GetBreadcrumb(this IHtmlHelper htmlHelper)
        {
            var controller = htmlHelper.ViewContext.RouteData.Values["Controller"]?.ToString();
            var action = htmlHelper.ViewContext.RouteData.Values["Action"]?.ToString();

            var breadcrumb = new List<string>();
            
            // Add home
            breadcrumb.Add("<a href='/'>Home</a>");
            
            // Add controller if not home
            if (!string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase))
            {
                breadcrumb.Add($"<a href='/{controller}'>{controller}</a>");
            }
            
            // Add action if not index
            if (!string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
            {
                breadcrumb.Add($"<span>{action}</span>");
            }

            return string.Join(" / ", breadcrumb);
        }
    }
}
