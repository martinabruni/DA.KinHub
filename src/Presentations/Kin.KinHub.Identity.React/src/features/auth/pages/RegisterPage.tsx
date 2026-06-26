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
  const { register, login } = useAuth()
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
      try {
        await login({ email: values.email, password: values.password })
        const returnTo = searchParams.get('returnTo')
        navigate(
          returnTo
            ? `/select-member?returnTo=${encodeURIComponent(returnTo)}`
            : '/select-member',
          { replace: true },
        )
      } catch {
        toast.error(t('auth.autoLoginFailed'))
        navigate('/login')
      }
    } catch (err: unknown) {
      const { fields } = extractApiError(err)
      if (fields?.email) form.setError('email', { message: fields.email[0] })
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 to-white dark:from-zinc-950 dark:to-zinc-900 p-4">
      <Card className="w-full max-w-[420px] p-8 rounded-2xl shadow-xl">
        <CardContent className="p-0">
          <div className="flex flex-col items-center mb-2 gap-2">
            <Home className="w-10 h-10 text-primary" />
            <h1 className="text-2xl font-bold">{t('auth.createAccount')}</h1>
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
                          autoComplete="new-password"
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
                      <Input type="password" autoComplete="new-password" {...field} />
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
                    {t('auth.creatingAccount')}
                  </>
                ) : (
                  t('auth.createAccount')
                )}
              </Button>
            </form>
          </Form>

          <p className="text-sm text-muted-foreground text-center mt-6">
            {t('auth.alreadyHaveAccount')}{' '}
            <Link to="/login" className="font-semibold text-primary hover:underline">
              {t('auth.login')}
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
