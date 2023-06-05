const defaultTheme = require("tailwindcss/defaultTheme");
const colors = require("tailwindcss/colors");

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./index.html", "./webapp/**/*.{js,jsx,ts,tsx}"],
  theme: {
    fontFamily: {
      sans: ["Inter var", ...defaultTheme.fontFamily.sans],
      title: ["Cal Sans", ...defaultTheme.fontFamily.sans],
    },
    colors: {
      transparent: "transparent",
      current: "currentColor",
      default: "var(--apta-default)",
      inverted: "var(--apta-inverted)",
      muted: "var(--apta-muted)",
      subtle: "var(--apta-subtle)",
      emphasis: "var(--apta-emphasis)",
      chartsecondary: "var(--apta-chart-secondary)",
      error: "var(--apta-error)",
      warning: "var(--apta-warning)",
      success: "var(--apta-success)",

      black: colors.black,
      white: colors.white,
      primary: {
        ...colors.blue,
        DEFAULT: colors.blue[600],
      },
    },
    textColor: {
      white: colors.white,
      default: "var(--apta-text-default)",
      inverted: "var(--apta-inverted)",
      subtle: "var(--apta-text-subtle)",
      warning: "var(--apta-warning)",
      success: "var(--apta-success)",
      error: "var(--apta-error)",
      primary: {
        ...colors.blue,
        DEFAULT: colors.blue[600],
      },
    },
    borderColor: {
      default: "var(--apta-border-default)",
      success: "var(--apta-success)",
      warning: "var(--apta-warning)",
      error: "var(--apta-error)",
      primary: {
        ...colors.blue,
        DEFAULT: colors.blue[600],
      },
    },
    extend: {
      typography: {
        invert: {
          css: {
            "--tw-prose-pre-bg": "var(--apta-muted)",
          },
        },
      },
    },
  },
  plugins: [require("@tailwindcss/forms"), require("@tailwindcss/typography")],
};
