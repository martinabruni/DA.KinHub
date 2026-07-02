import { useState } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
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
import { useAuth } from '@/features/auth/AuthProvider'
import { extractApiError } from '@/lib/errors'
import { cn } from '@/lib/utils'

const schema = z
  .object({
    email: z.string().email(),
    password: z.string().min(8, 'Min 8 characters'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: 'auth.passwordMismatch',
    path: ['confirmPassword'],
  })

type FormValues = z.infer<typeof schema>

function PasswordStrength({ password }: { password: string }) {
  const score = Math.min(
    4,
    [/.{8,}/, /[A-Z]/, /[0-9]/, /[^A-Za-z0-9]/].filter((r) => r.test(password)).length,
  )
  const colors = ['bg-destructive', 'bg-orange-400', 'bg-yellow-400', 'bg-green-400', 'bg-green-600']
  return (
    <div className="flex gap-1 mt-1">
      {Array.from({ length: 4 }).map((_, i) => (
        <div
          key={i}
          className={cn('h-1 flex-1 rounded-full transition-colors', i < score ? colors[score] : 'bg-muted')}
        />
      ))}
    </div>
  )
}

export function RegisterPage() {
  const { t } = useTranslation()
  const { register } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [showPwd, setShowPwd] = useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '', confirmPassword: '' },
  })

  const password = form.watch('password')

  const onSubmit = async (values: FormValues) => {
    try {
      await register({ email: values.email, password: values.password })
      const returnTo = searchParams.get('returnTo')
      if (returnTo) {
        window.location.assign(returnTo)
        return
      }

      toast.success(t('auth.accountCreated'))
      navigate('/login')
    } catch (err: unknown) {
      const { fields } = extractApiError(err)
      if (fields?.email) form.setError('email', { message: fields.email[0] })
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
              <h1 className="text-2xl font-semibold tracking-tight">{t('auth.createAccount')}</h1>
              <p className="text-sm text-muted-foreground">{t('app.tagline')}</p>
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
                            autoComplete="new-password"
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
                      <PasswordStrength password={password} />
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="confirmPassword"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('auth.confirmPassword')}</FormLabel>
                      <FormControl>
                        <Input type="password" autoComplete="new-password" className="h-11" {...field} />
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
                      {t('auth.creatingAccount')}
                    </>
                  ) : (
                    t('auth.createAccount')
                  )}
                </Button>
              </form>
            </Form>

            <p className="mt-6 text-center text-sm text-muted-foreground">
              {t('auth.alreadyHaveAccount')}{' '}
              <Link
                to={searchParams.get('returnTo')
                  ? `/login?returnTo=${encodeURIComponent(searchParams.get('returnTo')!)}`
                  : '/login'}
                className="font-semibold text-primary hover:underline"
              >
                {t('auth.login')}
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
