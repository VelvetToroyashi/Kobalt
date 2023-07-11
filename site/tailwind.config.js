/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./*.html", "./css/*.css"],
  theme: {
    fontFamily: {
      "league": ['League\\ Spartan', 'sans-serif'],
      "helvetica": ['Helvetica', 'Arial', 'sans-serif'],
      "roboto": ['Roboto', 'sans-serif'],
      "roboto-mono": ['Roboto', 'Source\\ Code\\ Pro', 'monospace']
    },

    extend: {
      backgroundImage: {
        'hero-pattern': "url('/img/hero.svg')"
      }
    }
  },
  plugins: [],
}

