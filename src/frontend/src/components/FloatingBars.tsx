import * as PopoverPrimitive from "@radix-ui/react-popover";
import { Globe, Home, Info, LogIn, LogOut, Moon, Settings, Sun, UserRound, X } from "lucide-react";
import { Children, useEffect, useRef, useState, type ReactNode, type TouchEvent } from "react";
import { Link, NavLink } from "react-router-dom";
import { cn } from "../lib/utils";
import { IconButton } from "./ui/core";

export function FloatingBarPage({ children, label }: { children: ReactNode; label: string }) {
  return <div className="kh-floating-page" role="group" aria-label={label}>{children}</div>;
}

export function FloatingBarCarousel({ children, defaultIndex = 0, routeKey, label, pageLabel }: { children: ReactNode; defaultIndex?: number; routeKey: string; label: string; pageLabel: (current: number, total: number) => string }) {
  const pages = Children.toArray(children);
  const [index, setIndex] = useState(defaultIndex);
  const [interacting, setInteracting] = useState(false);
  const touchStart = useRef<number | null>(null);
  const timer = useRef<number | undefined>(undefined);

  useEffect(() => setIndex(defaultIndex), [defaultIndex, routeKey]);
  useEffect(() => () => window.clearTimeout(timer.current), []);

  const showPosition = () => {
    setInteracting(true);
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => setInteracting(false), 1600);
  };

  const move = (next: number) => {
    setIndex(Math.min(pages.length - 1, Math.max(0, next)));
    showPosition();
  };

  const onTouchEnd = (event: TouchEvent) => {
    if (touchStart.current === null) {
      return;
    }

    const delta = event.changedTouches[0].clientX - touchStart.current;
    if (Math.abs(delta) > 36) {
      move(index + (delta < 0 ? 1 : -1));
    }

    touchStart.current = null;
  };

  return <div className="kh-floating-carousel" aria-label={label} onMouseEnter={showPosition} onFocusCapture={showPosition} onPointerDown={showPosition} onTouchStart={(event) => { touchStart.current = event.touches[0].clientX; showPosition(); }} onTouchEnd={onTouchEnd} onKeyDown={(event) => { if (event.key === "ArrowLeft") move(index - 1); if (event.key === "ArrowRight") move(index + 1); }} tabIndex={0}>
    <div className="kh-floating-carousel__viewport"><div className="kh-floating-carousel__track">{pages[index]}</div></div>
    {pages.length > 1 ? <div className={cn("kh-carousel-position", interacting && "is-visible")} aria-label={pageLabel(index + 1, pages.length)}>{pages.map((_, pageIndex) => <button key={pageIndex} type="button" aria-label={pageLabel(pageIndex + 1, pages.length)} aria-current={index === pageIndex ? "true" : undefined} onClick={() => move(pageIndex)} />)}</div> : null}
  </div>;
}

function BarPopover({ trigger, label, children }: { trigger: ReactNode; label: string; children: ReactNode }) {
  return <PopoverPrimitive.Root><PopoverPrimitive.Trigger asChild>{trigger}</PopoverPrimitive.Trigger><PopoverPrimitive.Portal><PopoverPrimitive.Content className="kh-popover" sideOffset={12} aria-label={label}>{children}<PopoverPrimitive.Close className="kh-popover__close" aria-label={label}><X aria-hidden="true" /></PopoverPrimitive.Close><PopoverPrimitive.Arrow className="kh-popover__arrow" /></PopoverPrimitive.Content></PopoverPrimitive.Portal></PopoverPrimitive.Root>;
}

export function LanguageMenu({ label, currentLanguage, options, onSelect }: { label: string; currentLanguage: string; options: Array<{ value: string; label: string }>; onSelect: (value: string) => void }) {
  return <BarPopover label={label} trigger={<IconButton label={label} tone="accent" data-tour="language"><Globe aria-hidden="true" /></IconButton>}><div className="kh-menu-list">{options.map((option) => <button key={option.value} type="button" aria-pressed={currentLanguage === option.value} onClick={() => onSelect(option.value)}>{option.label}</button>)}</div></BarPopover>;
}

export function ThemeToggle({ theme, onToggle, label }: { theme: "light" | "dark"; onToggle: () => void; label: string }) {
  return <IconButton label={label} tone="warning" onClick={onToggle} data-tour="theme">{theme === "dark" ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}</IconButton>;
}

export function InformationMenu({ label, releaseNotesLabel, versionLabel, userGuideLabel, releaseNotesPath, versionPath, userGuidePath }: { label: string; releaseNotesLabel: string; versionLabel: string; userGuideLabel: string; releaseNotesPath: string; versionPath: string; userGuidePath: string }) {
  return <BarPopover label={label} trigger={<IconButton label={label} tone="info"><Info aria-hidden="true" /></IconButton>}><div className="kh-menu-list"><Link to={releaseNotesPath}>{releaseNotesLabel}</Link><Link to={versionPath}>{versionLabel}</Link><Link to={userGuidePath}>{userGuideLabel}</Link></div></BarPopover>;
}

export function UserMenu({ authenticated, name, loginLabel, logoutLabel, accountLabel, onLogin, onLogout }: { authenticated: boolean; name?: string; loginLabel: string; logoutLabel: string; accountLabel: string; onLogin: () => void; onLogout: () => void }) {
  if (!authenticated) {
    return <button type="button" className="kh-login-pill" onClick={onLogin}><LogIn aria-hidden="true" />{loginLabel}</button>;
  }

  return <BarPopover label={accountLabel} trigger={<IconButton label={accountLabel} tone="accent"><UserRound aria-hidden="true" /></IconButton>}><div className="kh-menu-list">{name ? <strong>{name}</strong> : null}<button type="button" onClick={onLogout}><LogOut aria-hidden="true" />{logoutLabel}</button></div></BarPopover>;
}

export function GlobalNavigationBar({ labels, paths, theme, authenticated, accountName, currentLanguage, onLanguageChange, onThemeToggle, onLogin, onLogout }: { labels: { navigation: string; home: string; information: string; releaseNotes: string; version: string; userGuide: string; language: string; languageOptions: Array<{ value: string; label: string }>; theme: string; settings: string; login: string; logout: string; account: string }; paths: { home: string; releaseNotes: string; about: string; settings: string; userGuide: string }; theme: "light" | "dark"; authenticated: boolean; accountName?: string; currentLanguage: string; onLanguageChange: (value: string) => void; onThemeToggle: () => void; onLogin: () => void; onLogout: () => void }) {
  return <nav className="kh-floating-bar" aria-label={labels.navigation} data-tour="navigation">
    <NavLink className={({ isActive }) => cn("kh-floating-link", isActive && "is-active")} to={paths.home} end aria-label={labels.home}><Home aria-hidden="true" /></NavLink>
    <InformationMenu label={labels.information} releaseNotesLabel={labels.releaseNotes} versionLabel={labels.version} userGuideLabel={labels.userGuide} releaseNotesPath={paths.releaseNotes} versionPath={paths.about} userGuidePath={paths.userGuide} />
    <LanguageMenu label={labels.language} currentLanguage={currentLanguage} options={labels.languageOptions} onSelect={onLanguageChange} />
    <ThemeToggle theme={theme} onToggle={onThemeToggle} label={labels.theme} />
    <NavLink className={({ isActive }) => cn("kh-floating-link", isActive && "is-active")} to={paths.settings} aria-label={labels.settings}><Settings aria-hidden="true" /></NavLink>
    <UserMenu authenticated={authenticated} name={accountName} loginLabel={labels.login} logoutLabel={labels.logout} accountLabel={labels.account} onLogin={onLogin} onLogout={onLogout} />
  </nav>;
}
