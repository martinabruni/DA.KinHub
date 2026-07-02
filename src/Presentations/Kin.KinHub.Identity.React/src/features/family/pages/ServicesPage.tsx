import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { buildKinListLaunchUrl, buildKinRecipeLaunchUrl } from "@/config/appLinks";
import { useServices } from "@/features/family/ServicesProvider";
import { serviceConfig, defaultServiceConfig } from "@/config/serviceConfig";

export function ServicesPage() {
  const { t } = useTranslation();
  const { services, isLoading } = useServices();

  const enabledServices = services.filter((s) => s.isEnabled);

  return (
    <div>
      <h1 className="text-2xl font-bold">{t("services.title")}</h1>
      <p className="text-muted-foreground text-sm mt-1">
        {t("services.subtitle")}
      </p>

      <div className="mt-6 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-40 rounded-2xl" />
            ))
          : enabledServices.map((service) => {
              const cfg = serviceConfig[service.name] ?? defaultServiceConfig;
              const Icon = cfg.icon;
              const href =
                cfg.external
                  ? service.name === 'KinList' || service.name === 'Lists'
                    ? buildKinListLaunchUrl('/')
                    : buildKinRecipeLaunchUrl('/')
                  : cfg.path;
              const content = (
                <Card className="h-full hover:shadow-lg hover:border-primary/40 transition-all cursor-pointer group">
                  <CardContent className="flex flex-col gap-3 p-5 h-full">
                    <div className="w-12 h-12 rounded-xl bg-muted flex items-center justify-center group-hover:bg-primary/10 transition-colors">
                      <Icon className={`w-6 h-6 ${cfg.color}`} />
                    </div>
                    <div className="flex-1">
                      <p className="font-semibold leading-tight">
                        {service.name}
                      </p>
                      <p className="text-muted-foreground text-xs mt-1 line-clamp-2">
                        {service.description}
                      </p>
                    </div>
                    <span className="text-xs font-medium text-primary">
                      {t("services.open")} →
                    </span>
                  </CardContent>
                </Card>
              );

              if (cfg.external) {
                return (
                  <a key={service.id} href={href}>
                    {content}
                  </a>
                );
              }

              return (
                <Link key={service.id} to={href}>
                  {content}
                </Link>
              );
            })}
      </div>

      <div className="mt-8">
        <Link
          to="/console/services"
          className="text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          {t("services.manage")}
        </Link>
      </div>
    </div>
  );
}
