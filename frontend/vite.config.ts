import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [tailwindcss(), sveltekit({ adapter: adapter(), preprocess: vitePreprocess(), alias: { $lib: 'src/lib' } })],
  // See https://vike.dev/broken-npm-package
  ssr: {
    noExternal: ['emoji-mart', "@emoji-mart/data"]
  }
});
