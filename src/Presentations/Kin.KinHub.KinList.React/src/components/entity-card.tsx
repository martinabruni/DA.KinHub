import * as React from 'react'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'

type EntityCardProps = React.ComponentProps<typeof Card> & {
  icon: React.ReactNode
  title: React.ReactNode
  description?: React.ReactNode
  topRight?: React.ReactNode
  meta?: React.ReactNode
  footer?: React.ReactNode
  contentClassName?: string
  bodyClassName?: string
}

export function EntityCard({
  icon,
  title,
  description,
  topRight,
  meta,
  footer,
  className,
  contentClassName,
  bodyClassName,
  ...props
}: EntityCardProps) {
  return (
    <Card
      className={cn(
        'h-full overflow-hidden border-border/70 bg-card/80 transition-all hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md',
        className
      )}
      {...props}
    >
      <CardContent
        className={cn('flex aspect-square h-full flex-col p-4 sm:p-5', contentClassName)}
      >
        <div className="flex items-start justify-between gap-3">
          {icon}
          {topRight}
        </div>
        <div className={cn('mt-4 flex-1', bodyClassName)}>
          <p className="font-semibold leading-tight">{title}</p>
          {description ? (
            <div className="mt-2 text-sm text-muted-foreground line-clamp-3">{description}</div>
          ) : null}
          {meta}
        </div>
        {footer ? <div className="mt-4">{footer}</div> : null}
      </CardContent>
    </Card>
  )
}
