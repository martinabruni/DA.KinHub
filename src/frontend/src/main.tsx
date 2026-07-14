import { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter, Link, Route, Routes } from "react-router-dom";
import "./styles.css";

const translations = {
  it: {
    home: "Home",
    projects: "Progetti",
    settings: "Impostazioni",
    about: "Versione",
    releases: "Note di rilascio",
    help: "Guida della pagina",
    welcome: "Benvenuto in KinHub",
    description: "Un accesso semplice ai servizi della famiglia.",
    language: "Lingua",
    theme: "Tema",
    notFound: "Pagina non trovata",
  },
  en: {
    home: "Home",
    projects: "Projects",
    settings: "Settings",
    about: "Version",
    releases: "Release notes",
    help: "Page guide",
    welcome: "Welcome to KinHub",
    description: "Simple access to family services.",
    language: "Language",
    theme: "Theme",
    notFound: "Page not found",
  },
} as const;

type Locale = "it" | "en";
type TranslationKey = keyof (typeof translations)["it"];

const routes: ReadonlyArray<readonly [string, TranslationKey]> = [
  ["/", "home"],
  ["/projects", "projects"],
  ["/settings", "settings"],
  ["/about", "about"],
  ["/releases", "releases"],
];

function App() {
  const [locale, setLocale] = useState<Locale>(
    (localStorage.getItem("locale") as Locale) || "it",
  );
  const [theme, setTheme] = useState(localStorage.getItem("theme") || "system");
  const currentTranslations = translations[locale];

  useEffect(() => {
    localStorage.setItem("locale", locale);
    localStorage.setItem("theme", theme);
    document.documentElement.dataset.theme = theme;
  }, [locale, theme]);

  return (
    <>
      <header>
        <strong>KinHub</strong>
        <nav>
          {routes.map(([path, key]) => (
            <Link key={path} to={path}>
              {currentTranslations[key]}
            </Link>
          ))}
        </nav>
        <select
          aria-label={currentTranslations.language}
          value={locale}
          onChange={(event) => setLocale(event.target.value as Locale)}
        >
          <option value="it">IT</option>
          <option value="en">EN</option>
        </select>
        <select
          aria-label={currentTranslations.theme}
          value={theme}
          onChange={(event) => setTheme(event.target.value)}
        >
          <option value="system">System</option>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
        </select>
      </header>
      <main>
        <Routes>
          {routes.map(([path, key]) => (
            <Route
              key={path}
              path={path}
              element={
                <Page
                  title={currentTranslations[key]}
                  help={currentTranslations.help}
                  locale={locale}
                />
              }
            />
          ))}
          <Route
            path="*"
            element={
              <Page
                title={currentTranslations.notFound}
                help={currentTranslations.help}
                locale={locale}
              />
            }
          />
        </Routes>
      </main>
    </>
  );
}

function Page({
  title,
  help,
  locale,
}: {
  title: string;
  help: string;
  locale: Locale;
}) {
  const [open, setOpen] = useState(false);
  const isItalian = locale === "it";

  return (
    <section>
      <h1>{title}</h1>
      <div className="help">
        <button onClick={() => setOpen(!open)} aria-expanded={open}>
          {help}
        </button>
        {open && (
          <p>
            {isItalian
              ? "Questa pagina spiega le azioni disponibili, i prerequisiti e i limiti. Consulta la guida completa per maggiori dettagli."
              : "This page explains available actions, prerequisites and limitations. Consult the full guide for details."}
          </p>
        )}
      </div>
      {title === translations[locale].home && (
        <p>
          {translations[locale].welcome}. {translations[locale].description}
        </p>
      )}
    </section>
  );
}

createRoot(document.getElementById("root")!).render(
  <BrowserRouter>
    <App />
  </BrowserRouter>,
);
