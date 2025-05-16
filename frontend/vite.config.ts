import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [tailwindcss(), sveltekit()],
  // See https://vike.dev/broken-npm-package
  ssr: {
    noExternal: ['emoji-mart', "@emoji-mart/data"]
  }
});
