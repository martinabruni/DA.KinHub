import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import commonEn from "./locales/en/common.json";
import helpEn from "./locales/en/help.json";
import pagesEn from "./locales/en/pages.json";
import tutorialEn from "./locales/en/tutorial.json";
import commonIt from "./locales/it/common.json";
import helpIt from "./locales/it/help.json";
import pagesIt from "./locales/it/pages.json";
import tutorialIt from "./locales/it/tutorial.json";

const supported = ["it", "en"] as const;
const stored = localStorage.getItem("kinhub.locale");
const browser = navigator.language.split("-")[0];
const initial = supported.includes(stored as (typeof supported)[number])
  ? stored!
  : supported.includes(browser as (typeof supported)[number]) ? browser : "it";

void i18n.use(initReactI18next).init({
  lng: initial,
  fallbackLng: "en",
  supportedLngs: supported,
  defaultNS: "common",
  ns: ["common", "pages", "help", "tutorial"],
  resources: {
    it: { common: commonIt, pages: pagesIt, help: helpIt, tutorial: tutorialIt },
    en: { common: commonEn, pages: pagesEn, help: helpEn, tutorial: tutorialEn }
  },
  interpolation: { escapeValue: true },
  saveMissing: import.meta.env.DEV,
  missingKeyHandler: (_languages, namespace, key) => {
    if (import.meta.env.DEV) console.warn(`Missing i18n key: ${namespace}:${key}`);
  }
});

i18n.on("languageChanged", (language) => {
  localStorage.setItem("kinhub.locale", language);
  document.documentElement.lang = language;
});

document.documentElement.lang = initial;

export const formatDate = (value: string | Date, locale = i18n.language) =>
  new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
export const formatNumber = (value: number, locale = i18n.language) => new Intl.NumberFormat(locale).format(value);
export const formatPercent = (value: number, locale = i18n.language) => new Intl.NumberFormat(locale, { style: "percent" }).format(value);

export default i18n;
