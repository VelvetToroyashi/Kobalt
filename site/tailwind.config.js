/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./*.html", "./css/*.css"],
  theme: {
    fontFamily: {
      "league": ['League\\ Spartan', 'sans-serif'],
      "helvetica": ['Helvetica', 'Arial', 'sans-serif']
    },
    extend: {
      backgroundImage: {
        'hero-pattern': "url('/img/hero.svg')"
      }
    }
  },
  plugins: [],
}

