import * as DialogPrimitive from "@radix-ui/react-dialog";
import * as TooltipPrimitive from "@radix-ui/react-tooltip";
import { AlertCircle, CheckCircle2, Info, TriangleAlert, X } from "lucide-react";
import type { ComponentPropsWithoutRef, HTMLAttributes, ReactNode } from "react";
import { cn } from "../../lib/utils";
import { Button, IconButton, type Tone } from "./core";

const toneIcons = { neutral: Info, brand: Info, accent: Info, success: CheckCircle2, warning: TriangleAlert, danger: AlertCircle, info: Info };

export function Alert({ tone = "info", title, children }: { tone?: Tone; title: string; children: ReactNode }) {
  const Icon = toneIcons[tone];
  return <div className={cn("kh-alert", `kh-tone--${tone}`)} role={tone === "danger" ? "alert" : "status"}><Icon aria-hidden="true" /><div><strong>{title}</strong><div>{children}</div></div></div>;
}

export function FormMessage({ error, children }: { error?: boolean; children: ReactNode }) {
  return <p className={cn("kh-form-message", error && "is-error")} role={error ? "alert" : undefined}>{children}</p>;
}

export function StatePanel({ icon: Icon = Info, tone = "neutral", title, description, action, role, live, busy, headingLevel = 3, className, ...props }: { icon?: typeof Info; tone?: Tone; title: string; description: string; action?: ReactNode; role?: "status" | "alert" | "region"; live?: "off" | "polite" | "assertive"; busy?: boolean; headingLevel?: 2 | 3 | 4 } & HTMLAttributes<HTMLDivElement>) {
  const TitleTag = `h${headingLevel}` as const;
  return <div className={cn("kh-state-panel", `kh-tone--${tone}`, className)} role={role} aria-live={live} aria-busy={busy || undefined} {...props}><span className="kh-state-panel__icon"><Icon aria-hidden="true" /></span><TitleTag>{title}</TitleTag><p>{description}</p>{action}</div>;
}

interface OverlayDialogProps {
  mode: "dialog" | "drawer";
  trigger?: ReactNode;
  title: string;
  description?: string;
  children: ReactNode;
  closeLabel: string;
  open?: boolean;
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
  modal?: boolean;
}

function OverlayDialog({ mode, trigger, title, description, children, closeLabel, open, defaultOpen, onOpenChange, modal = true }: OverlayDialogProps) {
  return <DialogPrimitive.Root open={open} defaultOpen={defaultOpen} onOpenChange={onOpenChange} modal={modal}>{trigger ? <DialogPrimitive.Trigger asChild>{trigger}</DialogPrimitive.Trigger> : null}<DialogPrimitive.Portal><DialogPrimitive.Overlay className="kh-overlay" /><DialogPrimitive.Content className={cn("kh-dialog", mode === "drawer" && "kh-drawer")}><div className="kh-dialog__heading"><div><DialogPrimitive.Title>{title}</DialogPrimitive.Title>{description && <DialogPrimitive.Description>{description}</DialogPrimitive.Description>}</div><DialogPrimitive.Close asChild><IconButton label={closeLabel} variant="ghost"><X aria-hidden="true" /></IconButton></DialogPrimitive.Close></div>{children}</DialogPrimitive.Content></DialogPrimitive.Portal></DialogPrimitive.Root>;
}

export function Dialog(props: Omit<OverlayDialogProps, "mode">) {
  return <OverlayDialog mode="dialog" {...props} />;
}

export function Drawer(props: Omit<OverlayDialogProps, "mode">) {
  return <OverlayDialog mode="drawer" {...props} />;
}

export function Tooltip({ label, children }: { label: string; children: ReactNode }) {
  return <TooltipPrimitive.Provider delayDuration={300}><TooltipPrimitive.Root><TooltipPrimitive.Trigger asChild>{children}</TooltipPrimitive.Trigger><TooltipPrimitive.Portal><TooltipPrimitive.Content className="kh-tooltip" sideOffset={8}>{label}<TooltipPrimitive.Arrow className="kh-tooltip__arrow" /></TooltipPrimitive.Content></TooltipPrimitive.Portal></TooltipPrimitive.Root></TooltipPrimitive.Provider>;
}

export function Snackbar({ open, message, actionLabel, onAction, onDismiss, dismissLabel, role = "status", live = "polite", ...props }: { open: boolean; message: string; actionLabel?: string; onAction?: () => void; onDismiss: () => void; dismissLabel: string; role?: "status" | "alert"; live?: "polite" | "assertive" } & ComponentPropsWithoutRef<"div">) {
  if (!open) return null;
  return <div className="kh-snackbar" role={role} aria-live={live} {...props}><span>{message}</span>{actionLabel && <Button variant="ghost" onClick={onAction}>{actionLabel}</Button>}<IconButton label={dismissLabel} variant="ghost" onClick={onDismiss}><X aria-hidden="true" /></IconButton></div>;
}
