import { ArrowRight, Check, Clock3, Copy, Ellipsis, ListChecks, Mail, Users } from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { cn } from "../lib/utils";
import { Avatar, Badge, Button, Card, Chip, IconBadge, StatusDot, type Tone } from "./ui/core";

function BaseServiceCard({ to, icon, title, description, tone, badge }: { to?: string; icon: Parameters<typeof IconBadge>[0]["icon"]; title: string; description: string; tone: Tone; badge?: ReactNode }) {
  const content = <>
    <IconBadge icon={icon} tone={tone} size="lg" />
    <span><strong>{title}</strong><span>{description}</span></span>
    {badge ?? <ArrowRight aria-hidden="true" />}
  </>;

  if (to) {
    return <Link className="kh-service-card" to={to}>{content}</Link>;
  }

  return <Card className="kh-service-card kh-service-card--soon">{content}</Card>;
}

export function FeatureCard({ to, icon, title, description, tone = "brand" }: { to: string; icon: Parameters<typeof IconBadge>[0]["icon"]; title: string; description: string; tone?: Tone }) {
  return <BaseServiceCard to={to} icon={icon} tone={tone} title={title} description={description} />;
}

export function KinServiceGrid({ children }: { children: ReactNode }) {
  return <div className="kh-service-grid">{children}</div>;
}

export function KinServiceCard({ to, title, description, icon = ListChecks, tone = "accent" }: { to: string; title: string; description: string; icon?: Parameters<typeof IconBadge>[0]["icon"]; tone?: Tone }) {
  return <BaseServiceCard to={to} icon={icon} tone={tone} title={title} description={description} />;
}

export function ComingSoonServiceCard({ title, description, badgeLabel }: { title: string; description: string; badgeLabel: string }) {
  return <BaseServiceCard icon={Ellipsis} tone="warning" title={title} description={description} badge={<Badge tone="warning">{badgeLabel}</Badge>} />;
}

export function CategoryCarousel({ label, categories, selected, onSelect }: { label: string; categories: Array<{ id: string; label: string; tone?: Tone }>; selected: string; onSelect: (id: string) => void }) {
  return <div className="kh-category-carousel" role="group" aria-label={label}>{categories.map((category) => <Chip key={category.id} tone={category.tone} selected={category.id === selected} onClick={() => onSelect(category.id)}>{category.label}</Chip>)}</div>;
}

export function KinListItem({ title, detail, completed, selected, onToggle, onSelect }: { title: string; detail: string; completed?: boolean; selected?: boolean; onToggle: () => void; onSelect: () => void }) {
  return <article className={cn("kh-list-item", completed && "is-completed", selected && "is-selected")}><Button variant={completed ? "primary" : "secondary"} className="kh-list-item__check" aria-pressed={completed} onClick={onToggle}>{completed && <Check aria-hidden="true" />}</Button><Button variant="ghost" className="kh-list-item__body" aria-pressed={selected} onClick={onSelect}><strong>{title}</strong><span>{detail}</span></Button></article>;
}

export function FamilyCard({ name, members, label }: { name: string; members: number; label: string }) {
  return <Card className="kh-family-card" highlighted><IconBadge icon={Users} tone="accent" size="lg" /><div><strong>{name}</strong><p>{label}</p></div><Badge tone="success">{members}</Badge></Card>;
}

export function MemberRow({ name, role, status }: { name: string; role: string; status: string }) {
  return <div className="kh-person-row"><Avatar name={name} /><div><strong>{name}</strong><span>{role}</span></div><StatusDot label={status} /></div>;
}

export function InviteRow({ address, status, actionLabel, onAction }: { address: string; status: string; actionLabel: string; onAction: () => void }) {
  return <div className="kh-person-row"><IconBadge icon={Mail} tone="info" /><div><strong>{address}</strong><span><Clock3 size={14} aria-hidden="true" /> {status}</span></div><Button variant="soft" onClick={onAction}><Copy size={16} aria-hidden="true" />{actionLabel}</Button></div>;
}
