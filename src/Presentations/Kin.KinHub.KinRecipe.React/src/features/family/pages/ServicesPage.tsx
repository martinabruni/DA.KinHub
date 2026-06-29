import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Settings2 } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useServices } from "@/features/family/ServicesProvider";
import { serviceConfig, defaultServiceConfig } from "@/config/serviceConfig";

export function ServicesPage() {
  const { t } = useTranslation();
  const { services, isLoading } = useServices();

  const enabledServices = services.filter((s) => s.isEnabled);

  return (
    <div className="space-y-5">
      <section className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">
            {t("services.title")}
          </h1>
          <p className="text-sm text-muted-foreground">{t("services.subtitle")}</p>
        </div>
        <Button asChild variant="outline" className="h-11 justify-between gap-2 sm:w-auto">
          <Link to="/console/services">
            <span>{t("hub.manageServices")}</span>
            <Settings2 className="h-4 w-4" />
          </Link>
        </Button>
      </section>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="aspect-square rounded-3xl" />
            ))
          : enabledServices.map((service) => {
              const cfg = serviceConfig[service.name] ?? defaultServiceConfig;
              const Icon = cfg.icon;
              return (
                <Link key={service.id} to={cfg.path} className="block">
                  <Card className="h-full border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
                    <CardContent className="flex aspect-square h-full flex-col p-4 sm:p-5">
                      <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-muted sm:h-11 sm:w-11">
                        <Icon className={`h-5 w-5 ${cfg.color}`} />
                      </div>
                      <div className="mt-4 flex-1">
                        <p className="font-semibold leading-tight">{service.name}</p>
                        <p className="mt-2 text-sm text-muted-foreground line-clamp-3">
                          {service.description}
                        </p>
                      </div>
                      <span className="mt-4 text-sm font-medium text-primary">{t("services.open")}</span>
                    </CardContent>
                  </Card>
                </Link>
              );
            })}
      </div>
    </div>
  );
}
