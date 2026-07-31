import { ArrowRight, CalendarClock, Clock3, Ellipsis, ListChecks, Users } from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { cn } from "../lib/utils";
import { Avatar, Badge, Card, Chip, IconBadge, StatusDot, type Tone } from "./ui/core";

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

export function KinListItem({ name, categories, remainingCategoryCount, authorName, authorDisplayName }: { name: string; categories: Array<{ id: string; name: string }>; remainingCategoryCount: number; authorName: string; authorDisplayName?: string | null }) {
  return <li className={cn("kh-list-item")}><article className="kh-list-item__body"><div className="kh-list-item__content"><strong>{name}</strong><div className="kh-list-item__meta">{categories.map((category) => <Badge key={category.id} tone="accent">{category.name}</Badge>)}{remainingCategoryCount > 0 ? <Badge tone="neutral">+{remainingCategoryCount}</Badge> : null}</div></div><div className="kh-list-item__author"><Avatar name={authorName} displayName={authorDisplayName} fallback="?" size="sm" /><span>{authorDisplayName ?? authorName}</span></div></article></li>;
}

export function FamilyCard({ name, members, label }: { name: string; members: number; label: string }) {
  return <Card className="kh-family-card" highlighted><IconBadge icon={Users} tone="accent" size="lg" /><div><strong>{name}</strong><p>{label}</p></div><Badge tone="success">{members}</Badge></Card>;
}

export function MemberRow({ label, displayName, initials, status }: { label: string; displayName: string; initials: string; status: string }) {
  return <li className="kh-person-row"><Avatar name={label} displayName={displayName} fallback={initials} /><div><strong>{displayName}</strong><span>{status}</span></div><StatusDot label={status} /></li>;
}

export function InviteRow({ creatorLabel, creatorDisplayName, creatorInitials, createdAtLabel, expiresAtLabel, status }: { creatorLabel: string; creatorDisplayName: string; creatorInitials: string; createdAtLabel: string; expiresAtLabel: string; status: string }) {
  return <li className="kh-person-row"><Avatar name={creatorLabel} displayName={creatorDisplayName} fallback={creatorInitials} tone="info" /><div><strong>{creatorDisplayName}</strong><span><Clock3 size={14} aria-hidden="true" /> {createdAtLabel}</span><span><CalendarClock size={14} aria-hidden="true" /> {expiresAtLabel}</span></div><StatusDot tone="info" label={status} /></li>;
}
