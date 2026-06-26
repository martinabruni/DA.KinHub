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
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 to-white dark:from-zinc-950 dark:to-zinc-900 p-4">
      <Card className="w-full max-w-[420px] p-8 rounded-2xl shadow-xl">
        <CardContent className="p-0">
          <div className="flex flex-col items-center mb-2 gap-2">
            <Home className="w-10 h-10 text-primary" />
            <h1 className="text-2xl font-bold">{t('app.name')}</h1>
            <p className="text-muted-foreground text-sm">{t('app.tagline')}</p>
            <p className="text-center text-xs text-muted-foreground">
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
                      <Input type="email" autoComplete="email" {...field} />
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
                          className="pr-10"
                          {...field}
                        />
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="absolute right-0 top-0 h-full px-3"
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
                className="w-full"
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

          <p className="text-sm text-muted-foreground text-center mt-6">
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
  )
}
