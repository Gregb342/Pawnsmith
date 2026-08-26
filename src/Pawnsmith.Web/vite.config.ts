import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// The bundle is copied into the API's wwwroot and served from the site root
// (A.6), so the base path is '/'.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
  },
});
