import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import path from "path";
import { componentTagger } from "lovable-tagger";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  server: {
    host: "::",
    port: 8080,
    hmr: {
      overlay: false,
    },
    proxy: {
      "/Auth": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Users": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Restaurant": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Dishes": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Images": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Audios": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Audio": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/Language": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
      "/uploads": {
        target: "http://localhost:5044",
        changeOrigin: true,
      },
    },
  },
  plugins: [react(), mode === "development" && componentTagger()].filter(
    Boolean,
  ),
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
}));
