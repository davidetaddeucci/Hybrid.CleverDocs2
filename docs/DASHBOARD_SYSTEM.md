# Advanced Dashboard System - Hybrid.CleverDocs2

## 🎯 **OVERVIEW**

The Advanced Dashboard System provides a modern, customizable dashboard experience with drag-and-drop widgets, animated components, and interactive charts. Built with ASP.NET Core MVC and Material Dashboard theme integration.

---

## 🏗️ **ARCHITECTURE**

### **Technology Stack**
- **Backend**: ASP.NET Core MVC 9.0
- **Frontend**: Vanilla JavaScript (NO Blazor)
- **Database**: PostgreSQL with Entity Framework Core
- **Charts**: Chart.js 4.4.0
- **Drag-and-Drop**: SortableJS 1.15.0
- **UI Framework**: Material Dashboard + Bootstrap 5
- **Authentication**: JWT-based multi-tenant

### **Component Architecture**
```
Dashboard System
├── StatCard Components (Animated counters with trends)
├── Chart Components (Chart.js integration)
├── Widget Framework (Drag-and-drop customization)
├── Database Layer (PostgreSQL persistence)
└── API Layer (RESTful widget management)
```

---

## 📊 **DASHBOARD TYPES**

### **1. Overview Dashboard**
**URL**: `/AdminDashboard/Index`
- **Purpose**: Enhanced traditional dashboard with modern components
- **Features**: StatCards with animations, interactive charts, real-time data
- **Target**: Users who prefer fixed layouts with rich visualizations

### **2. Customizable Dashboard**
**URL**: `/AdminDashboard/Customizable`
- **Purpose**: Fully customizable drag-and-drop dashboard
- **Features**: Widget reordering, personalized layouts, edit mode
- **Target**: Power users who want personalized dashboard experiences

---

## 🧩 **COMPONENT SYSTEM**

### **StatCard Component**
**Location**: `Views/Shared/Components/StatCard/`

**Features**:
- Animated counter with easing effects
- Trend indicators (up/down arrows with percentages)
- 6 color variants (primary, success, warning, danger, info, dark)
- Loading states with spinners
- Click actions for navigation
- Responsive design

**Usage**:
```csharp
@await Component.InvokeAsync("StatCard", new StatCardModel
{
    Title = "Total Users",
    Value = "1,234",
    Icon = "people",
    Color = "primary",
    TrendPercentage = "+12.5",
    TrendDirection = "up",
    AnimateCounter = true
})
```

### **Chart Component**
**Location**: `Views/Shared/Components/Chart/`

**Features**:
- Chart.js integration (line, bar, pie, doughnut)
- Export functionality (PNG/PDF)
- Auto-refresh capabilities
- Loading and error states
- Theme adaptation (light/dark)
- Click event handling

**Usage**:
```csharp
@await Component.InvokeAsync("Chart", new ChartModel
{
    Title = "User Growth",
    Type = "line",
    DataUrl = "/api/charts/user-growth",
    Height = 350,
    ShowExport = true,
    RefreshInterval = 300
})
```

---

## 🎛️ **WIDGET FRAMEWORK**

### **Database Schema**

#### **UserDashboardWidgets Table**
```sql
CREATE TABLE "UserDashboardWidgets" (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "WidgetType" varchar(50) NOT NULL,
    "WidgetId" varchar(100) NOT NULL,
    "Title" varchar(200) NOT NULL,
    "Configuration" jsonb NOT NULL,
    "PositionX" integer NOT NULL,
    "PositionY" integer NOT NULL,
    "Width" integer NOT NULL,
    "Height" integer NOT NULL,
    "Order" integer NOT NULL,
    "IsVisible" boolean NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "MinimumRole" integer NOT NULL,
    "RefreshInterval" integer NOT NULL,
    "Theme" varchar(20) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id"),
    FOREIGN KEY ("CompanyId") REFERENCES "Companies"("Id")
);
```

#### **WidgetTemplates Table**
```sql
CREATE TABLE "WidgetTemplates" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(100) NOT NULL,
    "WidgetType" varchar(50) NOT NULL,
    "Description" varchar(500),
    "DefaultConfiguration" jsonb NOT NULL,
    "DefaultWidth" integer NOT NULL,
    "DefaultHeight" integer NOT NULL,
    "MinimumRole" integer NOT NULL,
    "Category" varchar(50) NOT NULL,
    "Icon" varchar(50) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Order" integer NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);
```

### **Widget Configuration JSON**
```json
{
  "dataUrl": "/api/admin/users/count",
  "icon": "people",
  "color": "primary",
  "trend": "+8.2%",
  "chartType": "line",
  "showExport": true,
  "refreshInterval": 300
}
```

---

## 🔌 **API ENDPOINTS**

### **Widget Management**
- `GET /api/widgets/dashboard` - Get user's widget configuration
- `POST /api/widgets/dashboard` - Save widget configuration
- `GET /api/widgets/templates` - Get available widget templates
- `PUT /api/widgets/{id}/position` - Update widget position
- `PUT /api/widgets/{id}/visibility` - Toggle widget visibility

### **Chart Data**
- `GET /api/charts/user-growth` - User growth chart data (Admin only)
- `GET /api/charts/document-types` - Document types distribution (Admin/Company)
- `GET /api/charts/company-activity` - Company activity chart (Admin only)
- `GET /api/charts/user-activity` - User activity chart (All roles)
- `GET /api/charts/storage-usage` - Storage usage chart (Admin/Company)

---

## 🎨 **STYLING SYSTEM**

### **CSS Architecture**
```
material-dashboard-extensions.css
├── StatCard Styles (.stat-card, .stat-counter, etc.)
├── Chart Styles (.chart-card, .chart-container, etc.)
├── Widget Styles (.dashboard-widget, .widget-controls, etc.)
└── Responsive Styles (@media queries)
```

### **Color System**
```css
/* StatCard Color Variants */
.bg-gradient-primary { background: linear-gradient(195deg, #e91e63 0%, #ad1457 100%); }
.bg-gradient-success { background: linear-gradient(195deg, #4caf50 0%, #2e7d32 100%); }
.bg-gradient-warning { background: linear-gradient(195deg, #ff9800 0%, #f57c00 100%); }
.bg-gradient-danger { background: linear-gradient(195deg, #f44336 0%, #c62828 100%); }
.bg-gradient-info { background: linear-gradient(195deg, #2196f3 0%, #1565c0 100%); }
.bg-gradient-dark { background: linear-gradient(195deg, #424242 0%, #212121 100%); }
```

---

## 🚀 **PERFORMANCE OPTIMIZATION**

### **Loading Strategy**
- **Parallel API Calls**: Dashboard data loaded concurrently
- **Caching**: Redis-based caching with configurable TTL
- **Lazy Loading**: Charts loaded on-demand
- **Animation Optimization**: requestAnimationFrame for smooth animations

### **Performance Targets**
- **Dashboard Load Time**: < 2 seconds
- **Chart Rendering**: < 500ms
- **Widget Reordering**: < 100ms response time
- **API Response**: < 200ms average

---

## 🔒 **SECURITY & MULTI-TENANCY**

### **Role-Based Access**
- **Admin (Role 1)**: Access to all widgets and system-wide charts
- **Company (Role 2)**: Access to company-specific widgets and data
- **User (Role 3)**: Access to user-specific widgets only

### **Data Isolation**
- **Company Isolation**: All widget data filtered by CompanyId
- **User Preferences**: Widget configurations tied to specific users
- **API Security**: JWT-based authentication on all endpoints

---

## 🧪 **TESTING GUIDE**

### **Manual Testing**
1. **Login**: Use test credentials `info@hybrid.it / Florealia2025!`
2. **Overview Dashboard**: Navigate to Dashboard > Overview
3. **Customizable Dashboard**: Navigate to Dashboard > Customizable
4. **Edit Mode**: Click "Edit Layout" to enable drag-and-drop
5. **Add Widgets**: Click "Add Widget" to browse templates
6. **Reorder**: Drag widgets to new positions
7. **Save**: Click "Save Changes" to persist layout

### **API Testing**
```bash
# Get widget configuration
curl -H "Authorization: Bearer {token}" http://localhost:5252/api/widgets/dashboard

# Get chart data
curl -H "Authorization: Bearer {token}" http://localhost:5252/api/charts/user-growth
```

---

## 📋 **MAINTENANCE**

### **Adding New Widget Types**
1. Create widget template in database
2. Add rendering logic in `dashboard-widgets.js`
3. Create API endpoint for data
4. Add CSS styles if needed

### **Adding New Chart Types**
1. Extend `ChartDataHelper.cs` with new chart type
2. Add endpoint in `ChartController.cs`
3. Update chart component template
4. Test with different data sets

---

**© 2025 Hybrid Research - Advanced Dashboard System Documentation**
