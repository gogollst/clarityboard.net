import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

// German
import deCommon from './locales/de/common.json';
import deNavigation from './locales/de/navigation.json';
import deAuth from './locales/de/auth.json';
import deValidation from './locales/de/validation.json';
import deDashboard from './locales/de/dashboard.json';
import deCashflow from './locales/de/cashflow.json';
import deScenarios from './locales/de/scenarios.json';
import deDocuments from './locales/de/documents.json';
import deBudget from './locales/de/budget.json';
import deAssets from './locales/de/assets.json';
import deDatev from './locales/de/datev.json';
import deSettings from './locales/de/settings.json';
import deAdmin from './locales/de/admin.json';
import deAi from './locales/de/ai.json';
import deHr from './locales/de/hr.json';
import deAccounting from './locales/de/accounting.json';
import deExecutive from './locales/de/executive.json';

// English
import enCommon from './locales/en/common.json';
import enNavigation from './locales/en/navigation.json';
import enAuth from './locales/en/auth.json';
import enValidation from './locales/en/validation.json';
import enDashboard from './locales/en/dashboard.json';
import enCashflow from './locales/en/cashflow.json';
import enScenarios from './locales/en/scenarios.json';
import enDocuments from './locales/en/documents.json';
import enBudget from './locales/en/budget.json';
import enAssets from './locales/en/assets.json';
import enDatev from './locales/en/datev.json';
import enSettings from './locales/en/settings.json';
import enAdmin from './locales/en/admin.json';
import enAi from './locales/en/ai.json';
import enHr from './locales/en/hr.json';
import enAccounting from './locales/en/accounting.json';
import enExecutive from './locales/en/executive.json';

// Russian
import ruCommon from './locales/ru/common.json';
import ruNavigation from './locales/ru/navigation.json';
import ruAuth from './locales/ru/auth.json';
import ruValidation from './locales/ru/validation.json';
import ruDashboard from './locales/ru/dashboard.json';
import ruCashflow from './locales/ru/cashflow.json';
import ruScenarios from './locales/ru/scenarios.json';
import ruDocuments from './locales/ru/documents.json';
import ruBudget from './locales/ru/budget.json';
import ruAssets from './locales/ru/assets.json';
import ruDatev from './locales/ru/datev.json';
import ruSettings from './locales/ru/settings.json';
import ruAdmin from './locales/ru/admin.json';
import ruAi from './locales/ru/ai.json';
import ruHr from './locales/ru/hr.json';
import ruAccounting from './locales/ru/accounting.json';
import ruExecutive from './locales/ru/executive.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      de: {
        common: deCommon,
        navigation: deNavigation,
        auth: deAuth,
        validation: deValidation,
        dashboard: deDashboard,
        cashflow: deCashflow,
        scenarios: deScenarios,
        documents: deDocuments,
        budget: deBudget,
        assets: deAssets,
        datev: deDatev,
        settings: deSettings,
        admin: deAdmin,
        ai: deAi,
        hr: deHr,
        accounting: deAccounting,
        executive: deExecutive,
      },
      en: {
        common: enCommon,
        navigation: enNavigation,
        auth: enAuth,
        validation: enValidation,
        dashboard: enDashboard,
        cashflow: enCashflow,
        scenarios: enScenarios,
        documents: enDocuments,
        budget: enBudget,
        assets: enAssets,
        datev: enDatev,
        settings: enSettings,
        admin: enAdmin,
        ai: enAi,
        hr: enHr,
        accounting: enAccounting,
        executive: enExecutive,
      },
      ru: {
        common: ruCommon,
        navigation: ruNavigation,
        auth: ruAuth,
        validation: ruValidation,
        dashboard: ruDashboard,
        cashflow: ruCashflow,
        scenarios: ruScenarios,
        documents: ruDocuments,
        budget: ruBudget,
        assets: ruAssets,
        datev: ruDatev,
        settings: ruSettings,
        admin: ruAdmin,
        ai: ruAi,
        hr: ruHr,
        accounting: ruAccounting,
        executive: ruExecutive,
      },
    },
    lng: 'de',
    fallbackLng: 'de',
    supportedLngs: ['de', 'en', 'ru'],
    defaultNS: 'common',
    ns: ['common', 'navigation', 'auth', 'validation', 'dashboard', 'cashflow', 'scenarios', 'documents', 'budget', 'assets', 'datev', 'settings', 'admin', 'ai', 'hr', 'accounting', 'executive'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'clarityboard_language',
    },
  });

export default i18n;
