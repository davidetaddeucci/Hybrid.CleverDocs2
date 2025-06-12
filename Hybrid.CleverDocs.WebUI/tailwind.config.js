/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html}",
    "./Components/**/*.razor",
    "./wwwroot/**/*.html"
  ],
  theme: {
    extend: {
      // Material Design 3 Color System
      colors: {
        // Primary colors
        'md-primary': 'rgb(var(--md-sys-color-primary) / <alpha-value>)',
        'md-on-primary': 'rgb(var(--md-sys-color-on-primary) / <alpha-value>)',
        'md-primary-container': 'rgb(var(--md-sys-color-primary-container) / <alpha-value>)',
        'md-on-primary-container': 'rgb(var(--md-sys-color-on-primary-container) / <alpha-value>)',
        
        // Secondary colors
        'md-secondary': 'rgb(var(--md-sys-color-secondary) / <alpha-value>)',
        'md-on-secondary': 'rgb(var(--md-sys-color-on-secondary) / <alpha-value>)',
        'md-secondary-container': 'rgb(var(--md-sys-color-secondary-container) / <alpha-value>)',
        'md-on-secondary-container': 'rgb(var(--md-sys-color-on-secondary-container) / <alpha-value>)',
        
        // Tertiary colors
        'md-tertiary': 'rgb(var(--md-sys-color-tertiary) / <alpha-value>)',
        'md-on-tertiary': 'rgb(var(--md-sys-color-on-tertiary) / <alpha-value>)',
        'md-tertiary-container': 'rgb(var(--md-sys-color-tertiary-container) / <alpha-value>)',
        'md-on-tertiary-container': 'rgb(var(--md-sys-color-on-tertiary-container) / <alpha-value>)',
        
        // Error colors
        'md-error': 'rgb(var(--md-sys-color-error) / <alpha-value>)',
        'md-on-error': 'rgb(var(--md-sys-color-on-error) / <alpha-value>)',
        'md-error-container': 'rgb(var(--md-sys-color-error-container) / <alpha-value>)',
        'md-on-error-container': 'rgb(var(--md-sys-color-on-error-container) / <alpha-value>)',
        
        // Surface colors
        'md-surface': 'rgb(var(--md-sys-color-surface) / <alpha-value>)',
        'md-on-surface': 'rgb(var(--md-sys-color-on-surface) / <alpha-value>)',
        'md-surface-variant': 'rgb(var(--md-sys-color-surface-variant) / <alpha-value>)',
        'md-on-surface-variant': 'rgb(var(--md-sys-color-on-surface-variant) / <alpha-value>)',
        'md-surface-container': 'rgb(var(--md-sys-color-surface-container) / <alpha-value>)',
        'md-surface-container-high': 'rgb(var(--md-sys-color-surface-container-high) / <alpha-value>)',
        'md-surface-container-highest': 'rgb(var(--md-sys-color-surface-container-highest) / <alpha-value>)',
        
        // Outline colors
        'md-outline': 'rgb(var(--md-sys-color-outline) / <alpha-value>)',
        'md-outline-variant': 'rgb(var(--md-sys-color-outline-variant) / <alpha-value>)',
      },
      
      // Material Design 3 Typography
      fontFamily: {
        'md-display': ['Roboto', 'system-ui', 'sans-serif'],
        'md-headline': ['Roboto', 'system-ui', 'sans-serif'],
        'md-title': ['Roboto Medium', 'system-ui', 'sans-serif'],
        'md-body': ['Roboto', 'system-ui', 'sans-serif'],
        'md-label': ['Roboto Medium', 'system-ui', 'sans-serif'],
      },
      
      // Material Design 3 Spacing
      spacing: {
        'md-xs': '4px',
        'md-sm': '8px',
        'md-md': '12px',
        'md-lg': '16px',
        'md-xl': '24px',
        'md-2xl': '32px',
        'md-3xl': '48px',
      },
      
      // Material Design 3 Border Radius
      borderRadius: {
        'md-xs': '4px',
        'md-sm': '8px',
        'md-md': '12px',
        'md-lg': '16px',
        'md-xl': '28px',
        'md-full': '1000px',
      },
      
      // Material Design 3 Shadows
      boxShadow: {
        'md-1': '0px 1px 2px 0px rgba(0, 0, 0, 0.3), 0px 1px 3px 1px rgba(0, 0, 0, 0.15)',
        'md-2': '0px 1px 2px 0px rgba(0, 0, 0, 0.3), 0px 2px 6px 2px rgba(0, 0, 0, 0.15)',
        'md-3': '0px 1px 3px 0px rgba(0, 0, 0, 0.3), 0px 4px 8px 3px rgba(0, 0, 0, 0.15)',
        'md-4': '0px 2px 3px 0px rgba(0, 0, 0, 0.3), 0px 6px 10px 4px rgba(0, 0, 0, 0.15)',
        'md-5': '0px 4px 4px 0px rgba(0, 0, 0, 0.3), 0px 8px 12px 6px rgba(0, 0, 0, 0.15)',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}