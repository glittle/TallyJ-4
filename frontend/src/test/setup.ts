import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import { createI18n } from 'vue-i18n'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

// Create global instances for testing
const testPinia = createPinia()
const testRouter = createRouter({
  history: createWebHistory(),
  routes: []
})
const testI18n = createI18n({
  locale: 'en',
  messages: {
    en: {
      nav: {
        dashboard: 'Dashboard',
        elections: 'Elections',
        profile: 'Profile'
      },
      common: {
        english: 'English',
        french: 'French'
      },
      error: {
        somethingWentWrong: 'Something went wrong',
        pageError: 'There was an error loading this page.',
        tryAgain: 'Try Again',
        goHome: 'Go Home',
        errorDetails: 'Error Details'
      },
      auth: {
        logout: 'Logout',
        logoutSuccess: 'Logged out successfully'
      }
    },
    fr: {
      nav: {
        dashboard: 'Tableau de bord',
        elections: 'Élections',
        profile: 'Profil'
      },
      common: {
        english: 'Anglais',
        french: 'Français'
      },
      error: {
        somethingWentWrong: 'Quelque chose s\'est mal passé',
        pageError: 'Une erreur s\'est produite lors du chargement de cette page.',
        tryAgain: 'Réessayer',
        goHome: 'Accueil',
        errorDetails: 'Détails de l\'erreur'
      },
      auth: {
        logout: 'Déconnexion',
        logoutSuccess: 'Déconnexion réussie'
      }
    }
  }
})

// Export for direct imports if needed
export { testPinia as pinia, testRouter as router, testI18n as i18n }