import { ChevronLeft, ChevronRight, LoaderCircle, type LucideIcon } from "lucide-react";
import { forwardRef, useId, useRef, type ButtonHTMLAttributes, type HTMLAttributes, type InputHTMLAttributes, type KeyboardEvent, type ReactNode } from "react";
import { Link, type LinkProps } from "react-router-dom";
import { cn } from "../../lib/utils";

export type Tone = "neutral" | "brand" | "accent" | "success" | "warning" | "danger" | "info";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "soft" | "ghost" | "danger";
  loading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { className, variant = "primary", loading = false, disabled, children, ...props },
  ref
) {
  return <button ref={ref} type={props.type ?? "button"} className={cn("kh-button", `kh-button--${variant}`, className)} disabled={disabled || loading} aria-busy={loading || undefined} {...props}>
    {loading && <LoaderCircle className="kh-spinner" size={18} aria-hidden="true" />}
    {children}
  </button>;
});

export interface ButtonLinkProps extends LinkProps {
  variant?: ButtonProps["variant"];
}

export const ButtonLink = forwardRef<HTMLAnchorElement, ButtonLinkProps>(function ButtonLink(
  { className, variant = "primary", ...props },
  ref
) {
  return <Link ref={ref} className={cn("kh-button", `kh-button--${variant}`, className)} {...props} />;
});

export interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  label: string;
  tone?: Tone;
  variant?: "solid" | "ghost";
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(function IconButton(
  { label, tone = "neutral", variant = "solid", className, children, ...props },
  ref
) {
  return <button ref={ref} type={props.type ?? "button"} className={cn("kh-icon-button", `kh-tone--${tone}`, variant === "ghost" && "kh-icon-button--ghost", className)} aria-label={label} title={label} {...props}>{children}</button>;
});

export const FloatingActionButton = forwardRef<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement> & { label: string; extended?: string; tone?: Tone }>(function FloatingActionButton({ label, extended, tone = "brand", className, children, ...props }, ref) {
  return <button ref={ref} type={props.type ?? "button"} className={cn("kh-fab", `kh-tone--${tone}`, extended && "kh-fab--extended", className)} aria-label={label} {...props}>{children}{extended && <span>{extended}</span>}</button>;
});

export function IconBadge({ icon: Icon, tone = "brand", size = "md" }: { icon: LucideIcon; tone?: Tone; size?: "sm" | "md" | "lg" }) {
  return <span className={cn("kh-icon-badge", `kh-tone--${tone}`, `kh-icon-badge--${size}`)} aria-hidden="true"><Icon /></span>;
}

export const Card = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement> & { interactive?: boolean; highlighted?: boolean }>(function Card({ className, interactive, highlighted, ...props }, ref) {
  return <div ref={ref} className={cn("kh-card", interactive && "kh-card--interactive", highlighted && "kh-card--highlighted", className)} {...props} />;
});

export function Surface({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("kh-surface", className)} {...props} />;
}

export function Separator({ className, ...props }: HTMLAttributes<HTMLHRElement>) {
  return <hr className={cn("kh-separator", className)} {...props} />;
}

export function Badge({ tone = "neutral", className, ...props }: HTMLAttributes<HTMLSpanElement> & { tone?: Tone }) {
  return <span className={cn("kh-badge", `kh-tone--${tone}`, className)} {...props} />;
}

export const Chip = forwardRef<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement> & { selected?: boolean; tone?: Tone }>(function Chip({ selected, tone = "brand", className, ...props }, ref) {
  return <button ref={ref} type={props.type ?? "button"} className={cn("kh-chip", `kh-tone--${tone}`, selected && "is-selected", className)} aria-pressed={selected} {...props} />;
});

export function Avatar({ name, displayName, fallback = "?", src, tone = "accent", size = "md" }: { name: string; displayName?: string | null; fallback?: string; src?: string; tone?: Tone; size?: "sm" | "md" | "lg" }) {
  const visualName = displayName?.trim() ?? "";
  const initials = visualName.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]?.toUpperCase()).join("");
  return <span className={cn("kh-avatar", `kh-avatar--${size}`, `kh-tone--${tone}`)} aria-label={name}>{src ? <img src={src} alt="" /> : initials || fallback}</span>;
}

export function StatusDot({ tone = "success", label }: { tone?: Tone; label: string }) {
  return <span className="kh-status"><span className={cn("kh-status__dot", `kh-tone--${tone}`)} aria-hidden="true" />{label}</span>;
}

export const TextField = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & { label: string; helper?: string; error?: string; leading?: ReactNode }>(function TextField({ id, label, helper, error, leading, className, "aria-describedby": ariaDescribedBy, ...props }, ref) {
  const generatedId = useId();
  const fieldId = id ?? props.name ?? generatedId;
  const helperId = helper ? `${fieldId}-helper` : undefined;
  const errorId = error ? `${fieldId}-error` : undefined;
  const describedBy = [helperId, errorId, ariaDescribedBy].filter(Boolean).join(" ") || undefined;
  return <label className="kh-field" htmlFor={fieldId}>
    <span className="kh-field__label">{label}</span>
    <span className={cn("kh-field__control", error && "is-invalid")}>
      {leading}
      <input ref={ref} id={fieldId} className={className} aria-invalid={Boolean(error)} aria-describedby={describedBy} {...props} />
    </span>
    {helper && <span id={helperId} className="kh-field__message">{helper}</span>}
    {error && <span id={errorId} className={cn("kh-field__message", "is-error")} role="alert">{error}</span>}
  </label>;
});

export function ProgressIndicator({ value, label }: { value: number; label: string }) {
  const normalized = Math.min(100, Math.max(0, value));
  return <div className="kh-progress"><div className="kh-progress__label"><span>{label}</span><span>{normalized}%</span></div><div className="kh-progress__track" role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={100} aria-valuenow={normalized}><span style={{ width: `${normalized}%` }} /></div></div>;
}

export function Spinner({ label }: { label: string }) {
  return <span className="kh-loader" role="status"><LoaderCircle className="kh-spinner" aria-hidden="true" />{label}</span>;
}

export function Skeleton({ className }: { className?: string }) {
  return <span className={cn("kh-skeleton", className)} aria-hidden="true" />;
}

export function Tabs({ items, value, onValueChange, label }: { items: Array<{ value: string; label: string; panelId?: string }>; value: string; onValueChange: (value: string) => void; label: string }) {
  const baseId = useId();
  const buttonsRef = useRef<Array<HTMLButtonElement | null>>([]);
  const selectedIndex = Math.max(0, items.findIndex((item) => item.value === value));

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) {
      return;
    }

    event.preventDefault();
    let nextIndex = selectedIndex;

    if (event.key === "ArrowRight") {
      nextIndex = (selectedIndex + 1) % items.length;
    } else if (event.key === "ArrowLeft") {
      nextIndex = (selectedIndex - 1 + items.length) % items.length;
    } else if (event.key === "Home") {
      nextIndex = 0;
    } else if (event.key === "End") {
      nextIndex = items.length - 1;
    }

    onValueChange(items[nextIndex].value);
    buttonsRef.current[nextIndex]?.focus();
  };

  return <div className="kh-tabs" role="tablist" aria-label={label} onKeyDown={onKeyDown}>{items.map((item, index) => {
    const tabId = `${baseId}-tab-${item.value}`;
    const panelId = item.panelId ?? `${baseId}-panel-${item.value}`;
    const selected = value === item.value;

    return <button key={item.value} ref={(element) => { buttonsRef.current[index] = element; }} type="button" id={tabId} role="tab" aria-selected={selected} aria-controls={panelId} tabIndex={selected ? 0 : -1} className={selected ? "is-selected" : undefined} onClick={() => onValueChange(item.value)}>{item.label}</button>;
  })}</div>;
}

export function Pagination({ hasPrevious, hasNext, busy = false, onPrevious, onNext, label, previousLabel, nextLabel, statusLabel }: { hasPrevious: boolean; hasNext: boolean; busy?: boolean; onPrevious: () => void; onNext: () => void; label: string; previousLabel: string; nextLabel: string; statusLabel: string }) {
  return <nav className="kh-pagination" aria-label={label}><Button variant="ghost" disabled={!hasPrevious || busy} aria-label={previousLabel} onClick={onPrevious}><ChevronLeft aria-hidden="true" />{previousLabel}</Button><span aria-live="polite" aria-atomic="true">{statusLabel}</span><Button variant="ghost" disabled={!hasNext || busy} aria-label={nextLabel} onClick={onNext}>{nextLabel}<ChevronRight aria-hidden="true" /></Button></nav>;
}
