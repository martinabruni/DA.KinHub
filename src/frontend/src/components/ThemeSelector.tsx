import { useTranslation } from "react-i18next";
import { useTheme, type Theme } from "./ThemeProvider";
import { Select } from "./ui/controls";

export function ThemeSelector() {
  const { t } = useTranslation("common");
  const { theme, setTheme } = useTheme();
  return (
    <div data-tour="theme">
      <Select label={t("theme.label")} value={theme} onValueChange={(value) => setTheme(value as Theme)} options={[{ value: "light", label: t("theme.light") }, { value: "dark", label: t("theme.dark") }, { value: "system", label: t("theme.system") }]} />
    </div>
  );
}
