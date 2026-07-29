import { useTranslation } from "react-i18next";
import { Select } from "./ui/controls";

export function LanguageSelector() {
  const { t, i18n } = useTranslation("common");
  return (
    <div data-tour="language">
      <Select label={t("language.label")} value={i18n.language === "it" ? "it" : "en"} onValueChange={(value) => { void i18n.changeLanguage(value); }} options={[{ value: "it", label: t("language.it") }, { value: "en", label: t("language.en") }]} />
    </div>
  );
}
