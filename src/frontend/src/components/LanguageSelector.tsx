import { useTranslation } from "react-i18next";

export function LanguageSelector() {
  const { t, i18n } = useTranslation("common");
  return (
    <label className="control" data-tour="language">
      <span>{t("language.label")}</span>
      <select value={i18n.language} onChange={(event) => { void i18n.changeLanguage(event.target.value); }}>
        <option value="it">{t("language.it")}</option>
        <option value="en">{t("language.en")}</option>
      </select>
    </label>
  );
}
