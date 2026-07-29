import { forwardRef, useId, type InputHTMLAttributes } from "react";
import * as CheckboxPrimitive from "@radix-ui/react-checkbox";
import * as SelectPrimitive from "@radix-ui/react-select";
import * as SwitchPrimitive from "@radix-ui/react-switch";
import { Check, ChevronDown } from "lucide-react";
import { cn } from "../../lib/utils";

export function Checkbox({ id, label, checked, onCheckedChange, disabled }: { id: string; label: string; checked: boolean; onCheckedChange: (checked: boolean) => void; disabled?: boolean }) {
  return <label className="kh-check" htmlFor={id}><CheckboxPrimitive.Root id={id} className="kh-checkbox" checked={checked} disabled={disabled} onCheckedChange={(value) => onCheckedChange(value === true)}><CheckboxPrimitive.Indicator><Check aria-hidden="true" /></CheckboxPrimitive.Indicator></CheckboxPrimitive.Root><span>{label}</span></label>;
}

export function Switch({ id, label, checked, onCheckedChange, disabled }: { id: string; label: string; checked: boolean; onCheckedChange: (checked: boolean) => void; disabled?: boolean }) {
  return <label className="kh-switch-row" htmlFor={id}><span>{label}</span><SwitchPrimitive.Root id={id} className="kh-switch" checked={checked} disabled={disabled} onCheckedChange={onCheckedChange}><SwitchPrimitive.Thumb className="kh-switch__thumb" /></SwitchPrimitive.Root></label>;
}

export function Select({ id, label, value, onValueChange, options, placeholder, disabled }: { id?: string; label: string; value: string; onValueChange: (value: string) => void; options: Array<{ value: string; label: string }>; placeholder?: string; disabled?: boolean }) {
  const generatedId = useId();
  const triggerId = id ?? generatedId;
  return <label className="kh-field" htmlFor={triggerId}><span className="kh-field__label">{label}</span><SelectPrimitive.Root value={value} onValueChange={onValueChange} disabled={disabled}><SelectPrimitive.Trigger id={triggerId} className="kh-select" aria-label={label}><SelectPrimitive.Value placeholder={placeholder} /><SelectPrimitive.Icon><ChevronDown size={17} aria-hidden="true" /></SelectPrimitive.Icon></SelectPrimitive.Trigger><SelectPrimitive.Portal><SelectPrimitive.Content className="kh-select-content" position="popper"><SelectPrimitive.Viewport>{options.map((option) => <SelectPrimitive.Item className="kh-select-item" key={option.value} value={option.value}><SelectPrimitive.ItemText>{option.label}</SelectPrimitive.ItemText><SelectPrimitive.ItemIndicator><Check size={16} /></SelectPrimitive.ItemIndicator></SelectPrimitive.Item>)}</SelectPrimitive.Viewport></SelectPrimitive.Content></SelectPrimitive.Portal></SelectPrimitive.Root></label>;
}

export const InviteCodeField = forwardRef<HTMLInputElement, Omit<InputHTMLAttributes<HTMLInputElement>, "value" | "onChange"> & { label: string; value: string; onChange: (value: string) => void }>(function InviteCodeField({ id, label, value, onChange, className, ...props }, ref) {
  const normalized = value.replace(/[^a-z0-9]/gi, "").slice(0, 6).toUpperCase();
  const generatedId = useId();
  const inputId = id ?? generatedId;
  return <label className="kh-field" htmlFor={inputId}><span className="kh-field__label">{label}</span><span className="kh-code-field">{Array.from({ length: 6 }, (_, index) => <span key={index} aria-hidden="true">{normalized[index] ?? ""}</span>)}<input ref={ref} id={inputId} className={cn(className)} value={normalized} onChange={(event) => onChange(event.target.value)} maxLength={6} autoCapitalize="characters" aria-label={label} {...props} /></span></label>;
});
