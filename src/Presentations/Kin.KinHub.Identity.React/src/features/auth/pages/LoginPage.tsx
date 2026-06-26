import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Home, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Separator } from '@/components/ui/separator'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { appendSessionToUrl, buildCoreSelectMemberUrl } from '@/config/appLinks'
import { useAuth } from '@/features/auth/AuthProvider'
import { extractApiError } from '@/lib/errors'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { t } = useTranslation()
  const { login } = useAuth()
  const [searchParams] = useSearchParams()
  const [showPwd, setShowPwd] = useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = async (values: FormValues) => {
    try {
      await login(values)
      const returnTo = searchParams.get('returnTo')
      window.location.assign(
        appendSessionToUrl(buildCoreSelectMemberUrl(returnTo), null),
      )
    } catch (err: unknown) {
      const { message, fields } = extractApiError(err)
      if (fields?.email) form.setError('email', { message: fields.email[0] })
      if (fields?.password) form.setError('password', { message: fields.password[0] })
      if (message && !fields) toast.error(message)
    }
  }

  return (
    <div className="min-h-dvh bg-[radial-gradient(circle_at_top,theme(colors.primary/0.12),transparent_35%),linear-gradient(180deg,theme(colors.background),theme(colors.muted/0.45))] px-4 py-6 sm:px-6">
      <div className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-md items-center">
        <Card className="w-full border-border/70 bg-card/95 shadow-xl backdrop-blur">
          <CardContent className="p-5 sm:p-7">
            <div className="mb-2 flex flex-col items-center gap-2 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
                <Home className="h-7 w-7" />
              </div>
              <h1 className="text-2xl font-semibold tracking-tight">{t('app.name')}</h1>
              <p className="text-sm text-muted-foreground">{t('app.tagline')}</p>
              <p className="text-xs text-muted-foreground">
                {t('auth.centralizedLogin')}
              </p>
            </div>
            <Separator className="my-6" />

            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('auth.email')}</FormLabel>
                      <FormControl>
                        <Input type="email" autoComplete="email" className="h-11" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="password"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('auth.password')}</FormLabel>
                      <FormControl>
                        <div className="relative">
                          <Input
                            type={showPwd ? 'text' : 'password'}
                            autoComplete="current-password"
                            className="h-11 pr-11"
                            {...field}
                          />
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="absolute right-0 top-0 h-11 w-11"
                            onClick={() => setShowPwd((v) => !v)}
                            tabIndex={-1}
                          >
                            {showPwd ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                          </Button>
                        </div>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <Button
                  type="submit"
                  className="h-11 w-full"
                  size="lg"
                  disabled={form.formState.isSubmitting}
                >
                  {form.formState.isSubmitting ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      {t('auth.signingIn')}
                    </>
                  ) : (
                    t('auth.signIn')
                  )}
                </Button>
              </form>
            </Form>

            <p className="mt-6 text-center text-sm text-muted-foreground">
              {t('auth.noAccount')}{' '}
              <Link
                to={searchParams.get('returnTo')
                  ? `/register?returnTo=${encodeURIComponent(searchParams.get('returnTo')!)}`
                  : '/register'}
                className="font-semibold text-primary hover:underline"
              >
                {t('auth.register')}
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
