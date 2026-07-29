import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Dialog } from "./ui/feedback";
import { Button } from "./ui/core";

const tutorialVersion = 1;
const storageKey = `kinhub.tutorial.${tutorialVersion}`;
const steps = [
  { id: "navigation", target: "[data-tour='navigation']" },
  { id: "language", target: "[data-tour='language']" },
  { id: "theme", target: "[data-tour='theme']" },
  { id: "help", target: "[data-tour='help']" },
  { id: "version", target: "[data-tour='version']" },
  { id: "lifecycle", target: "[data-tour='lifecycle']" }
] as const;

export function restartTutorial() {
  localStorage.removeItem(storageKey);
  window.dispatchEvent(new Event("kinhub:tutorial-restart"));
}

export function Onboarding() {
  const { t } = useTranslation(["tutorial", "common"]);
  const [open, setOpen] = useState(() => localStorage.getItem(storageKey) !== "completed");
  const [index, setIndex] = useState(0);
  const step = steps[index];

  useEffect(() => {
    const restart = () => { setIndex(0); setOpen(true); };
    window.addEventListener("kinhub:tutorial-restart", restart);
    return () => window.removeEventListener("kinhub:tutorial-restart", restart);
  }, []);

  useEffect(() => {
    document.querySelectorAll(".tour-target").forEach((element) => element.classList.remove("tour-target"));
    if (!open) return;
    const target = document.querySelector(step.target);
    target?.classList.add("tour-target");
    target?.scrollIntoView({ block: "center", behavior: matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
    return () => target?.classList.remove("tour-target");
  }, [open, step]);

  const complete = () => { localStorage.setItem(storageKey, "completed"); setOpen(false); };
  const isLast = index === steps.length - 1;
  const labels = useMemo(() => ({
    title: t(`steps.${step.id}.title`, { ns: "tutorial" }),
    body: t(`steps.${step.id}.body`, { ns: "tutorial" })
  }), [step.id, t]);
  if (!open) return null;

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => { if (!nextOpen) complete(); }} title={labels.title} closeLabel={t("actions.close", { ns: "common" })}>
      <section className="tutorial-dialog-body">
        <p className="eyebrow">{t("progress", { ns: "tutorial", current: index + 1, total: steps.length })}</p>
        <p>{labels.body}</p>
        <div className="tutorial-actions">
          <Button variant="ghost" onClick={complete}>{t("actions.skip", { ns: "common" })}</Button>
          <span className="spacer" />
          {index > 0 && <Button variant="secondary" onClick={() => setIndex(index - 1)}>{t("actions.back", { ns: "common" })}</Button>}
          <Button onClick={() => isLast ? complete() : setIndex(index + 1)}>
            {t(isLast ? "actions.finish" : "actions.next", { ns: "common" })}
          </Button>
        </div>
      </section>
    </Dialog>
  );
}
