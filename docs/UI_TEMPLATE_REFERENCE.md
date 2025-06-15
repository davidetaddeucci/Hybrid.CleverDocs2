# UI Template Reference - Hybrid.CleverDocs2

## 🎨 **SIDEBAR TEMPLATE DEFINITIVO - NON MODIFICARE**

### **Status**: ✅ PRODUCTION READY - LOCKED TEMPLATE
**Data Finalizzazione**: 2025-01-15
**Versione**: 2.0.0 FINAL - WITH ADVANCED DASHBOARD COMPONENTS

---

## 🔒 **REGOLA CRITICA**
**QUESTO TEMPLATE NON DEVE ESSERE MODIFICATO** senza esplicita richiesta dell'utente.  
Qualsiasi modifica deve essere approvata e documentata.

---

## 🎯 **SPECIFICHE TEMPLATE**

### **Colori Definitivi**
```css
/* Sfondo Sidebar */
background: linear-gradient(195deg, #ffffff 0%, #f8f9fa 100%);

/* Testo e Icone */
color: #344767; /* Testo scuro per ottima leggibilità */

/* Hover States */
background-color: rgba(52, 71, 103, 0.1); /* Grigio chiaro */

/* Bordi e Separatori */
border-color: rgba(52, 71, 103, 0.1); /* Grigio molto chiaro */
```

### **Layout e Posizionamento**
```css
/* Sidebar Container */
.sidenav {
  position: fixed;
  width: 250px;
  height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: flex-start; /* CRITICO: Menu in alto */
}

/* Header Limitato */
.sidenav-header {
  max-height: 80px;
  flex: 0 0 auto;
}

/* Search Container Limitato */
.md-ext-search-container {
  max-height: 60px;
  flex: 0 0 auto;
}

/* Menu Posizionato in Alto */
.navbar-collapse {
  flex: 0 0 auto;
  order: 1;
  position: relative;
  top: 0;
}
```

### **Caratteristiche Funzionali**
- ✅ **Menu Immediatamente Visibile**: Nessuno scroll richiesto
- ✅ **Testo Leggibile**: Contrasto ottimale con sfondo chiaro
- ✅ **Multi-level Menu**: Funzionalità espandibile mantenuta
- ✅ **Responsive**: Comportamento mobile preservato
- ✅ **Performance**: Layout ottimizzato per velocità

---

## 📁 **File Coinvolti**

### **CSS Principale**
- `Hybrid.CleverDocs.WebUI/wwwroot/css/material-dashboard-extensions.css`

### **HTML Template**
- `Hybrid.CleverDocs.WebUI/Views/Shared/_Layout.cshtml`
- `Hybrid.CleverDocs.WebUI/Views/Shared/_AdminNavigation.cshtml`
- `Hybrid.CleverDocs.WebUI/Views/Shared/_CompanyNavigation.cshtml`
- `Hybrid.CleverDocs.WebUI/Views/Shared/_UserNavigation.cshtml`

---

## 🔧 **CORREZIONI APPLICATE**

### **1. Posizionamento Menu**
- **Problema**: Menu posizionato in fondo alla sidebar
- **Soluzione**: Flexbox con `justify-content: flex-start`
- **CSS**: Forzatura posizione top con `order` e `flex: 0 0 auto`

### **2. Visibilità Testo**
- **Problema**: Testo chiaro su sfondo scuro illeggibile
- **Soluzione**: Inversione colori - sfondo chiaro, testo scuro
- **Colore**: `#344767` per testo e icone

### **3. Limitazione Altezze**
- **Problema**: Header e search che spingevano il menu in basso
- **Soluzione**: `max-height` su header (80px) e search (60px)
- **Risultato**: Spazio ottimizzato per il menu

---

## 🎨 **DESIGN SYSTEM**

### **Palette Colori**
```
Sfondo Principale: #ffffff → #f8f9fa (gradiente)
Testo Primario: #344767 (scuro)
Hover Background: rgba(52, 71, 103, 0.1)
Bordi: rgba(52, 71, 103, 0.1)
Active State: rgba(52, 71, 103, 0.15)
```

### **Tipografia**
```
Font Size Menu: 0.875rem
Font Weight: 400 (normale), 600 (attivo)
Line Height: 1.2
Icon Size: 1rem
Icon Opacity: 0.7
```

### **Spaziature**
```
Padding Menu Item: 0.75rem 1rem
Header Padding: 0.75rem 1rem 0.5rem 1rem
Search Padding: 0.5rem 1rem
Margin: 0 (tutti gli elementi)
```

---

## ⚠️ **AVVERTENZE**

### **NON MODIFICARE**
1. **Colori di sfondo** della sidebar
2. **Colori del testo** e delle icone
3. **Posizionamento flexbox** del menu
4. **Altezze massime** di header e search
5. **Ordine degli elementi** CSS

### **MODIFICHE CONSENTITE** (solo su richiesta esplicita)
1. Aggiunta di nuovi menu items
2. Modifica delle icone
3. Aggiunta di funzionalità JavaScript
4. Ottimizzazioni performance

---

## 🧪 **TEST DI VERIFICA**

### **Checklist Visiva**
- [ ] Sidebar con sfondo bianco/grigio chiarissimo
- [ ] Testo scuro (#344767) ben leggibile
- [ ] Menu visibile immediatamente senza scroll
- [ ] Header limitato a ~80px di altezza
- [ ] Search box limitato a ~60px di altezza
- [ ] Hover effects funzionanti
- [ ] Multi-level menu espandibile

### **Test Funzionali**
- [ ] Click su menu items funzionante
- [ ] Espansione sottomenu System Management
- [ ] Espansione sottomenu Content Management
- [ ] Responsive behavior su mobile
- [ ] Toggle sidebar funzionante

---

## 📞 **SUPPORTO**

Per qualsiasi modifica a questo template:
1. **Richiesta esplicita** dell'utente necessaria
2. **Backup** del CSS attuale prima delle modifiche
3. **Test completo** dopo ogni modifica
4. **Aggiornamento** di questa documentazione

---

---

## 🚀 **ADVANCED DASHBOARD COMPONENTS - v2.0.0**

### **Modern StatCard Components**
- **Animated Counters**: Smooth number animations with easing
- **Trend Indicators**: Up/down arrows with percentage changes
- **Color Variants**: 6 color themes (primary, success, warning, danger, info, dark)
- **Loading States**: Spinner animations during data loading
- **Click Actions**: Optional navigation links
- **Responsive**: Mobile-optimized layouts

### **Chart.js Integration**
- **Chart Types**: Line, Bar, Pie, Doughnut charts supported
- **Export Functionality**: PNG and PDF export capabilities
- **Auto-refresh**: Configurable refresh intervals
- **Theme Support**: Light/dark theme adaptation
- **Error Handling**: Graceful fallback for failed chart loads
- **Performance**: Optimized rendering with requestAnimationFrame

### **Drag-and-Drop Widget System**
- **SortableJS Integration**: Smooth drag-and-drop reordering
- **Edit Mode**: Visual editing with controls and indicators
- **Widget Templates**: Extensible template system
- **User Preferences**: Database-persisted dashboard layouts
- **Multi-tenant**: Role-based widget visibility
- **Responsive Grid**: Bootstrap-based responsive layout

### **Database Schema**
```sql
-- Widget configuration table
UserDashboardWidgets (
  Id, UserId, CompanyId, WidgetType, WidgetId, Title,
  Configuration (JSONB), PositionX, PositionY, Width, Height,
  IsVisible, IsEnabled, MinimumRole, RefreshInterval, Theme
)

-- Widget templates table
WidgetTemplates (
  Id, Name, WidgetType, Description, DefaultConfiguration (JSONB),
  DefaultWidth, DefaultHeight, MinimumRole, Category, Icon
)
```

### **API Endpoints**
- `GET /api/widgets/dashboard` - Get user's widget configuration
- `POST /api/widgets/dashboard` - Save widget configuration
- `GET /api/widgets/templates` - Get available widget templates
- `PUT /api/widgets/{id}/position` - Update widget position
- `PUT /api/widgets/{id}/visibility` - Toggle widget visibility
- `GET /api/charts/*` - Chart data endpoints with role-based access

### **JavaScript Framework**
- **DashboardWidgets.js**: Main widget management system
- **chart-manager.js**: Chart.js integration and management
- **material-dashboard-extensions.js**: Enhanced sidebar functionality
- **SortableJS**: External library for drag-and-drop

### **CSS Framework**
- **StatCard Styles**: Modern card designs with hover effects
- **Chart Styles**: Responsive chart containers with loading states
- **Widget Styles**: Drag-and-drop visual indicators
- **Responsive**: Mobile-first responsive design

---

**© 2025 Hybrid Research - Template UI Locked v2.0.0 WITH ADVANCED DASHBOARD**
