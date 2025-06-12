# 📋 Guida al Template Material Design 3 Dashboard - Creative Tim

## 🎯 **OVERVIEW**

Questa guida fornisce le linee guida per mantenere la compatibilità con il template originale **Material Dashboard 3** di Creative Tim, evitando di creare classi CSS personalizzate che potrebbero rompere il design system.

**Template Originale**: [Material Dashboard 3 by Creative Tim](https://github.com/creativetimofficial/material-dashboard)
**Versione**: 3.2.0
**Licenza**: MIT

---

## 🏗️ **STRUTTURA HTML FONDAMENTALE**

### 📄 **Layout Base**

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- Fonts obbligatori -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:300,400,500,600,700,900&display=swap" rel="stylesheet">
    
    <!-- Material Icons -->
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@24,400,0,0" />
    
    <!-- CSS Template -->
    <link href="~/css/material-dashboard.css" rel="stylesheet" />
</head>
<body class="g-sidenav-show bg-gray-100">
    <!-- Sidebar -->
    <aside class="sidenav navbar navbar-vertical navbar-expand-xs border-radius-lg fixed-start ms-2 bg-white my-2" id="sidenav-main">
        <!-- Sidebar Content -->
    </aside>
    
    <!-- Main Content -->
    <main class="main-content position-relative max-height-vh-100 h-100 border-radius-lg">
        <!-- Navbar -->
        <nav class="navbar navbar-main navbar-expand-lg px-0 mx-3 shadow-none border-radius-xl">
            <!-- Navbar Content -->
        </nav>
        
        <!-- Content Container -->
        <div class="container-fluid py-2">
            <!-- Page Content -->
        </div>
    </main>
</body>
</html>
```

---

## 🧭 **SIDEBAR (SIDENAV)**

### 🎨 **Classi Principali**

```html
<aside class="sidenav navbar navbar-vertical navbar-expand-xs border-radius-lg fixed-start ms-2 bg-white my-2" id="sidenav-main">
```

**Classi Obbligatorie**:
- `sidenav`: Classe principale della sidebar
- `navbar navbar-vertical navbar-expand-xs`: Bootstrap navbar verticale
- `border-radius-lg`: Bordi arrotondati
- `fixed-start`: Posizione fissa a sinistra
- `ms-2`: Margin start (sinistra)
- `bg-white`: Sfondo bianco
- `my-2`: Margin verticale

### 📋 **Header Sidebar**

```html
<div class="sidenav-header">
    <i class="fas fa-times p-3 cursor-pointer text-dark opacity-5 position-absolute end-0 top-0 d-none d-xl-none" aria-hidden="true" id="iconSidenav"></i>
    <a class="navbar-brand px-4 py-3 m-0" href="#">
        <img src="logo.png" class="navbar-brand-img" width="26" height="26" alt="logo">
        <span class="ms-1 text-sm text-dark">Brand Name</span>
    </a>
</div>
<hr class="horizontal dark mt-0 mb-2">
```

### 🔗 **Navigation Items**

```html
<div class="collapse navbar-collapse w-auto" id="sidenav-collapse-main">
    <ul class="navbar-nav">
        <!-- Menu Item Standard -->
        <li class="nav-item">
            <a class="nav-link text-dark" href="/dashboard">
                <i class="material-symbols-rounded opacity-5">dashboard</i>
                <span class="nav-link-text ms-1">Dashboard</span>
            </a>
        </li>
        
        <!-- Menu Item Attivo -->
        <li class="nav-item">
            <a class="nav-link active bg-gradient-dark text-white" href="/current-page">
                <i class="material-symbols-rounded opacity-5">dashboard</i>
                <span class="nav-link-text ms-1">Current Page</span>
            </a>
        </li>
        
        <!-- Sezione Header -->
        <li class="nav-item mt-3">
            <h6 class="ps-4 ms-2 text-uppercase text-xs text-dark font-weight-bolder opacity-5">Section Name</h6>
        </li>
    </ul>
</div>
```

---

## 🧭 **NAVBAR PRINCIPALE**

### 🎨 **Struttura Navbar**

```html
<nav class="navbar navbar-main navbar-expand-lg px-0 mx-3 shadow-none border-radius-xl" id="navbarBlur" data-scroll="true">
    <div class="container-fluid py-1 px-3">
        <!-- Breadcrumb -->
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb bg-transparent mb-0 pb-0 pt-1 px-0 me-sm-6 me-5">
                <li class="breadcrumb-item text-sm">
                    <a class="opacity-5 text-dark" href="javascript:;">Pages</a>
                </li>
                <li class="breadcrumb-item text-sm text-dark active" aria-current="page">Current Page</li>
            </ol>
        </nav>
        
        <!-- Navbar Items -->
        <div class="collapse navbar-collapse mt-sm-0 mt-2 me-md-0 me-sm-4" id="navbar">
            <div class="ms-md-auto pe-md-3 d-flex align-items-center">
                <!-- Search o altri elementi -->
            </div>
            <ul class="navbar-nav d-flex align-items-center justify-content-end">
                <!-- Mobile Toggle -->
                <li class="nav-item d-xl-none ps-3 d-flex align-items-center">
                    <a href="javascript:;" class="nav-link text-body p-0" id="iconNavbarSidenav">
                        <div class="sidenav-toggler-inner">
                            <i class="sidenav-toggler-line"></i>
                            <i class="sidenav-toggler-line"></i>
                            <i class="sidenav-toggler-line"></i>
                        </div>
                    </a>
                </li>
                
                <!-- User Menu -->
                <li class="nav-item d-flex align-items-center">
                    <a href="#" class="nav-link text-body font-weight-bold px-0">
                        <i class="material-symbols-rounded">account_circle</i>
                        <span class="d-sm-inline d-none">Username</span>
                    </a>
                </li>
            </ul>
        </div>
    </div>
</nav>
```

---

## 🎨 **SISTEMA DI COLORI**

### 🌈 **Colori Principali**

```css
/* Colori del Template */
--bs-primary: #e91e63;     /* Rosa Material */
--bs-secondary: #737373;   /* Grigio */
--bs-success: #4CAF50;     /* Verde */
--bs-info: #1A73E8;        /* Blu */
--bs-warning: #fb8c00;     /* Arancione */
--bs-danger: #F44335;      /* Rosso */
--bs-dark: #262626;        /* Nero */
--bs-white: #fff;          /* Bianco */
```

### 🎯 **Classi di Background**

```html
<!-- Gradienti -->
<div class="bg-gradient-primary"></div>
<div class="bg-gradient-dark"></div>
<div class="bg-gradient-success"></div>

<!-- Colori Solidi -->
<div class="bg-white"></div>
<div class="bg-gray-100"></div>
<div class="bg-dark"></div>
```

---

## 🔤 **TIPOGRAFIA**

### 📝 **Font Family**

```css
font-family: Inter, Helvetica, Arial, sans-serif;
```

### 📏 **Classi Tipografiche**

```html
<!-- Dimensioni Testo -->
<span class="text-xs">Extra Small</span>
<span class="text-sm">Small</span>
<span class="text-lg">Large</span>
<span class="text-xl">Extra Large</span>

<!-- Peso Font -->
<span class="font-weight-light">Light</span>
<span class="font-weight-normal">Normal</span>
<span class="font-weight-bold">Bold</span>
<span class="font-weight-bolder">Bolder</span>

<!-- Colori Testo -->
<span class="text-dark">Dark</span>
<span class="text-white">White</span>
<span class="text-primary">Primary</span>
<span class="text-secondary">Secondary</span>
```

---

## 🃏 **CARDS E COMPONENTI**

### 🎴 **Card Standard**

```html
<div class="card">
    <div class="card-header p-2 ps-3">
        <div class="d-flex justify-content-between">
            <div>
                <p class="text-sm mb-0 text-capitalize">Card Title</p>
                <h4 class="mb-0">Card Value</h4>
            </div>
            <div class="icon icon-md icon-shape bg-gradient-dark shadow-dark shadow text-center border-radius-lg">
                <i class="material-symbols-rounded opacity-10">dashboard</i>
            </div>
        </div>
    </div>
    <hr class="dark horizontal my-0">
    <div class="card-footer p-2 ps-3">
        <p class="mb-0 text-sm">
            <span class="text-success font-weight-bolder">+55% </span>than last week
        </p>
    </div>
</div>
```

### 🔘 **Buttons**

```html
<!-- Button Primario -->
<button class="btn bg-gradient-primary">Primary Button</button>

<!-- Button Outline -->
<button class="btn btn-outline-primary">Outline Button</button>

<!-- Button con Icona -->
<button class="btn bg-gradient-dark">
    <i class="material-symbols-rounded">add</i>
    Add Item
</button>
```

---

## 🎯 **ICONE**

### 🔣 **Material Symbols**

**Utilizzare sempre Material Symbols Rounded**:

```html
<!-- Icone Comuni -->
<i class="material-symbols-rounded">dashboard</i>
<i class="material-symbols-rounded">description</i>
<i class="material-symbols-rounded">folder</i>
<i class="material-symbols-rounded">chat</i>
<i class="material-symbols-rounded">search</i>
<i class="material-symbols-rounded">person</i>
<i class="material-symbols-rounded">settings</i>
<i class="material-symbols-rounded">logout</i>
<i class="material-symbols-rounded">business</i>
<i class="material-symbols-rounded">group</i>
<i class="material-symbols-rounded">admin_panel_settings</i>
<i class="material-symbols-rounded">monitor_heart</i>
```

### 🎨 **Classi per Icone**

```html
<!-- Opacità -->
<i class="material-symbols-rounded opacity-5">dashboard</i>
<i class="material-symbols-rounded opacity-10">dashboard</i>

<!-- Dimensioni -->
<i class="material-symbols-rounded icon-sm">dashboard</i>
<i class="material-symbols-rounded icon-md">dashboard</i>
<i class="material-symbols-rounded icon-lg">dashboard</i>
```

---

## 📱 **RESPONSIVE DESIGN**

### 📐 **Breakpoints Bootstrap**

```css
/* Extra small devices (phones, less than 576px) */
/* Small devices (landscape phones, 576px and up) */
@media (min-width: 576px) { ... }

/* Medium devices (tablets, 768px and up) */
@media (min-width: 768px) { ... }

/* Large devices (desktops, 992px and up) */
@media (min-width: 992px) { ... }

/* Extra large devices (large desktops, 1200px and up) */
@media (min-width: 1200px) { ... }
```

### 📱 **Classi Responsive**

```html
<!-- Visibilità -->
<div class="d-none d-xl-block">Visibile solo su XL+</div>
<div class="d-xl-none">Nascosto su XL+</div>

<!-- Spacing Responsive -->
<div class="p-2 p-md-3 p-lg-4">Padding responsive</div>
<div class="m-1 m-sm-2 m-md-3">Margin responsive</div>
```

---

## ⚡ **JAVASCRIPT E INTERAZIONI**

### 🔧 **Script Obbligatori**

```html
<!-- Bootstrap Bundle -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>

<!-- Material Dashboard JS -->
<script src="~/js/material-dashboard.js"></script>
```

### 🎮 **Funzionalità JavaScript**

```javascript
// Toggle Sidebar (Mobile)
document.getElementById('iconNavbarSidenav').addEventListener('click', function() {
    // Gestito automaticamente dal template
});

// Sidebar Collapse/Expand
document.getElementById('iconSidenav').addEventListener('click', function() {
    // Gestito automaticamente dal template
});
```

---

## 🚫 **COSA NON FARE**

### ❌ **Classi da Evitare**

```html
<!-- NON usare classi personalizzate che sovrascrivono il template -->
<div class="custom-sidebar">❌</div>
<div class="my-custom-nav">❌</div>
<div class="custom-main-content">❌</div>

<!-- NON modificare le classi principali -->
<aside class="my-sidebar">❌</aside>
<main class="my-main">❌</main>
```

### ❌ **CSS da Evitare**

```css
/* NON sovrascrivere le classi principali */
.sidenav { /* modifiche */ } ❌
.main-content { /* modifiche */ } ❌
.navbar-main { /* modifiche */ } ❌

/* NON usare !important */
.my-class {
    color: red !important; ❌
}
```

---

## ✅ **BEST PRACTICES**

### 🎯 **Estensioni Consentite**

```css
/* Utilizzare classi aggiuntive senza sovrascrivere */
.custom-component {
    /* Nuovi stili che non interferiscono */
}

/* Utilizzare le utility classes di Bootstrap */
.my-element {
    @extend .d-flex;
    @extend .align-items-center;
}
```

### 🔧 **Personalizzazioni Sicure**

```html
<!-- Aggiungere classi personalizzate INSIEME a quelle del template -->
<div class="card my-custom-card">
    <!-- Contenuto -->
</div>

<!-- Utilizzare data attributes per JavaScript personalizzato -->
<button class="btn bg-gradient-primary" data-custom-action="save">
    Save
</button>
```

---

## 📚 **RISORSE UTILI**

### 🔗 **Link Ufficiali**

- **Repository GitHub**: https://github.com/creativetimofficial/material-dashboard
- **Demo Live**: https://demos.creative-tim.com/material-dashboard/pages/dashboard.html
- **Documentazione**: https://demos.creative-tim.com/material-dashboard/docs/2.1/getting-started/introduction.html

### 📖 **Documentazione Bootstrap**

- **Bootstrap 5**: https://getbootstrap.com/docs/5.3/
- **Utility Classes**: https://getbootstrap.com/docs/5.3/utilities/api/
- **Components**: https://getbootstrap.com/docs/5.3/components/

### 🎨 **Material Design**

- **Material Design 3**: https://m3.material.io/
- **Material Symbols**: https://fonts.google.com/icons

---

## 🔄 **AGGIORNAMENTI TEMPLATE**

### 📅 **Versioning**

- **Versione Attuale**: 3.2.0
- **Ultimo Aggiornamento**: Verificare su GitHub
- **Compatibilità**: Bootstrap 5.3+

### 🔄 **Procedura di Aggiornamento**

1. **Backup**: Salvare le personalizzazioni
2. **Download**: Scaricare la nuova versione
3. **Merge**: Integrare le modifiche
4. **Test**: Verificare la compatibilità
5. **Deploy**: Applicare gli aggiornamenti

---

## 📝 **CHANGELOG PERSONALIZZAZIONI**

### 🗓️ **2025-06-12**

- ✅ Integrato Material Dashboard 3.2.0
- ✅ Aggiornata struttura HTML per compatibilità MD3
- ✅ Sostituito CSS personalizzato con template originale
- ✅ Implementata navigazione con Material Symbols
- ✅ Risolti problemi di layout sidebar/content

---

**⚠️ IMPORTANTE**: Seguire sempre questa guida per mantenere la compatibilità con il template originale e garantire aggiornamenti futuri senza problemi.