import { useTranslation } from "react-i18next";
import { useTheme, type Theme } from "./ThemeProvider";

export function ThemeSelector() {
  const { t } = useTranslation("common");
  const { theme, setTheme } = useTheme();
  return (
    <label className="control" data-tour="theme">
      <span>{t("theme.label")}</span>
      <select value={theme} onChange={(event) => setTheme(event.target.value as Theme)}>
        <option value="light">{t("theme.light")}</option>
        <option value="dark">{t("theme.dark")}</option>
        <option value="system">{t("theme.system")}</option>
      </select>
    </label>
  );
}
