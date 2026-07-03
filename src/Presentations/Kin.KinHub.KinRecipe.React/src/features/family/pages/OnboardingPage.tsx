import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Home, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Separator } from '@/components/ui/separator'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { identityApiClient } from '@/api/apiClient'
import { getApiErrorMessage } from '@/lib/errors'
import type { Family } from '@/types'

const schema = z.object({
  familyName: z.string().min(1).max(100),
  ownerProfileName: z.string().min(1).max(100),
})

type FormValues = z.infer<typeof schema>

export function OnboardingPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: family, isLoading: checkingFamily } = useQuery({
    queryKey: ['family'],
    queryFn: async () => {
      const { data } = await identityApiClient.get<Family>('/api/families')
      return data
    },
    retry: false,
  })

  useEffect(() => {
    if (!checkingFamily && family) {
      navigate('/select-member', { replace: true })
    }
  }, [family, checkingFamily, navigate])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { familyName: '', ownerProfileName: '' },
  })

  const createMutation = useMutation({
    mutationFn: (values: FormValues) =>
      identityApiClient.post('/api/families', {
        familyName: values.familyName,
        ownerProfileName: values.ownerProfileName,
        additionalMembers: [],
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['family'] })
      navigate('/select-member', { replace: true })
    },
    onError: (err) => {
      toast.error(getApiErrorMessage(err, t('onboarding.errorGeneric')))
    },
  })

  if (checkingFamily) return null

  const onSubmit = (values: FormValues) => {
    createMutation.mutate(values)
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 to-white dark:from-zinc-950 dark:to-zinc-900 p-4">
      <Card className="w-full max-w-[420px] p-8 rounded-2xl shadow-xl">
        <CardContent className="p-0">
          <div className="flex flex-col items-center mb-2 gap-2">
            <Home className="w-10 h-10 text-primary" />
            <h1 className="text-2xl font-bold">{t('onboarding.title')}</h1>
            <p className="text-muted-foreground text-sm text-center">{t('onboarding.subtitle')}</p>
          </div>
          <Separator className="my-6" />

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="familyName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('onboarding.familyName')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('onboarding.familyNamePlaceholder')} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="ownerProfileName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('onboarding.ownerProfileName')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('onboarding.ownerProfileNamePlaceholder')} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button
                type="submit"
                className="w-full"
                size="lg"
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    {t('onboarding.submitting')}
                  </>
                ) : (
                  t('onboarding.submit')
                )}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  )
}
