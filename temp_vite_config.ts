import { defineConfig } from \"vite\"; 
import vue from \"@vitejs/plugin-vue\";  
import VueI18nPlugin from \"@intlify/unplugin-vue-i18n/vite\";  
import { fileURLToPath, URL } from \"node:url\";  
  
// https://vite.dev/config/  
export default defineConfig({  
  resolve: {  
    alias: {  
      \"@\": fileURLToPath(new URL(\"./src\", import.meta.url)),  
    },  
  },  
  server: {  
    port: 8095,  
  },  
  plugins: [  
    vue(),  
    VueI18nPlugin({  
      include: [fileURLToPath(new URL(\"./src/locales/**/*.json\", import.meta.url))],  
    }),  
  ],  
}); 
