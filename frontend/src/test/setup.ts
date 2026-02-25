import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'

// Create global instances for testing
const testPinia = createPinia()
const testRouter = createRouter({
  history: createWebHistory(),
  routes: []
})

// Export for direct imports if needed
export { testPinia as pinia, testRouter as router }