/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// The bundle is copied into the API's wwwroot and served from the site root
// (A.6), so the base path is '/'.
//
// In development, Vite serves the front and hands /api to the API started by
// `dotnet run` (port 8080, launchSettings.json). The Host header is kept
// (changeOrigin: false) on purpose: the API refuses a write whose Origin is
// not its own Host (MEN-010), and the browser's Origin is Vite's address.
// Keeping Vite's Host makes both match, exactly as in production, where one
// server serves both.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
  },
  server: {
    proxy: {
      '/api': { target: 'http://localhost:8080', changeOrigin: false },
    },
  },

  // DEC-118 - Vitest reads this same file. Node, not a simulated browser:
  // what is tested is the pure modules (routing, errors, requests, the
  // project list, capacities), which need no DOM; the screens are tried in
  // a real browser instead. Tests sit next to their module, as *.test.ts.
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
});
