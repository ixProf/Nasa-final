/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        space: {
          950: '#030712',
          900: '#070d1e',
          850: '#0b152d',
          800: '#0f1d3e',
          700: '#172a54',
          cyan: '#00d2ff',
          neon: '#00f2fe',
          emerald: '#10b981',
          danger: '#ef4444',
          warning: '#f59e0b',
        },
      },
      fontFamily: {
        sans: ['var(--font-primary)', 'Cairo', 'Plus Jakarta Sans', 'system-ui', 'sans-serif'],
        arabic: ['Cairo', 'system-ui', 'sans-serif'],
        english: ['Plus Jakarta Sans', 'system-ui', 'sans-serif'],
        mono: ['var(--font-primary)', 'Plus Jakarta Sans', 'Cairo', 'sans-serif'],
      },
      boxShadow: {
        'glass': '0 8px 32px 0 rgba(0, 0, 0, 0.37)',
        'glass-glow': '0 0 25px rgba(0, 210, 255, 0.25), inset 0 0 15px rgba(0, 210, 255, 0.1)',
        'card-glow': '0 10px 30px -10px rgba(0, 150, 255, 0.3)',
        'green-glow': '0 0 20px rgba(16, 185, 129, 0.3)',
        'danger-glow': '0 0 20px rgba(239, 68, 68, 0.4)',
      },
      backdropBlur: {
        'xs': '2px',
        '2xl': '24px',
        '3xl': '40px',
      },
      animation: {
        'pulse-subtle': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'heartbeat': 'heartbeat 1.4s ease-in-out infinite',
        'float': 'float 6s ease-in-out infinite',
      },
      keyframes: {
        heartbeat: {
          '0%, 100%': { transform: 'scale(1)' },
          '14%': { transform: 'scale(1.12)' },
          '28%': { transform: 'scale(1)' },
          '42%': { transform: 'scale(1.08)' },
          '70%': { transform: 'scale(1)' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-6px)' },
        },
      },
    },
  },
  plugins: [],
}
