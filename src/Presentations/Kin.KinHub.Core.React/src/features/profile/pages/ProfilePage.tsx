import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTheme } from 'next-themes'
import { toast } from 'sonner'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Separator } from '@/components/ui/separator'
import { Switch } from '@/components/ui/switch'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { useAuth } from '@/features/auth/AuthProvider'
import { useFamily } from '@/features/family/FamilyProvider'
import { apiClient } from '@/api/apiClient'
import { getInitials } from '@/lib/utils'
import { cn } from '@/lib/utils'

function PasswordStrength({ password }: { password: string }) {
  const score = Math.min(
    4,
    [/.{8,}/, /[A-Z]/, /[0-9]/, /[^A-Za-z0-9]/].filter((r) => r.test(password)).length,
  )
  const colors = ['bg-destructive', 'bg-orange-400', 'bg-yellow-400', 'bg-green-400', 'bg-green-600']
  return (
    <div className="flex gap-1 mt-1">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className={cn('h-1 flex-1 rounded-full transition-colors', i < score ? colors[score] : 'bg-muted')} />
      ))}
    </div>
  )
}

const emailSchema = z.object({
  newEmail: z.string().email(),
  currentPassword: z.string().min(1),
})

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1),
    newPassword: z.string().min(8),
    confirmPassword: z.string(),
  })
  .refine((d) => d.newPassword === d.confirmPassword, { path: ['confirmPassword'], message: 'Passwords do not match' })

export function ProfilePage() {
  const { t, i18n } = useTranslation()
  const { theme, setTheme } = useTheme()
  const { user, logout } = useAuth()
  const { family, leaveFamily } = useFamily()
  const [deleteConfirmText, setDeleteConfirmText] = useState('')

  const emailForm = useForm<z.infer<typeof emailSchema>>({ resolver: zodResolver(emailSchema) })
  const passwordForm = useForm<z.infer<typeof passwordSchema>>({ resolver: zodResolver(passwordSchema) })
  const newPassword = passwordForm.watch('newPassword', '')

  const handleUpdateEmail = async (values: z.infer<typeof emailSchema>) => {
    try {
      await apiClient.put('/api/auth/me/email', values)
      toast.success(t('profile.updateEmail.success'))
      emailForm.reset()
    } catch {
      toast.error(t('common.error'))
    }
  }

  const handleUpdatePassword = async (values: z.infer<typeof passwordSchema>) => {
    try {
      await apiClient.put('/api/auth/me/password', values)
      toast.success(t('profile.updatePassword.success'))
      passwordForm.reset()
    } catch {
      toast.error(t('common.error'))
    }
  }

  const handleDeleteAccount = async () => {
    if (deleteConfirmText !== 'DELETE') return
    try {
      await apiClient.delete('/api/auth/me')
      await logout()
      toast.success(t('profile.dangerZone.success'))
    } catch {
      toast.error(t('common.error'))
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">{t('profile.title')}</h1>
      <div className="max-w-[560px] mx-auto space-y-6">

        {/* Identity */}
        <Card>
          <CardContent className="pt-6 flex flex-col items-center gap-2">
            <Avatar className="w-16 h-16">
              <AvatarFallback className="text-xl bg-primary/20 text-primary">
                {getInitials(user?.email ?? 'U')}
              </AvatarFallback>
            </Avatar>
            <p className="font-medium">{user?.email}</p>
          </CardContent>
        </Card>

        {/* Update Email */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">{t('profile.updateEmail.title')}</CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="pt-4">
            <form onSubmit={emailForm.handleSubmit(handleUpdateEmail)} className="space-y-3">
              <Input type="email" placeholder={t('profile.updateEmail.newEmail')} {...emailForm.register('newEmail')} />
              <Input type="password" placeholder={t('profile.updateEmail.currentPassword')} {...emailForm.register('currentPassword')} />
              <Button type="submit" size="sm">{t('profile.updateEmail.save')}</Button>
            </form>
          </CardContent>
        </Card>

        {/* Update Password */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">{t('profile.updatePassword.title')}</CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="pt-4">
            <form onSubmit={passwordForm.handleSubmit(handleUpdatePassword)} className="space-y-3">
              <Input type="password" placeholder={t('profile.updatePassword.currentPassword')} {...passwordForm.register('currentPassword')} />
              <div>
                <Input type="password" placeholder={t('profile.updatePassword.newPassword')} {...passwordForm.register('newPassword')} />
                <PasswordStrength password={newPassword} />
              </div>
              <Input type="password" placeholder={t('profile.updatePassword.confirmPassword')} {...passwordForm.register('confirmPassword')} />
              <Button type="submit" size="sm">{t('profile.updatePassword.save')}</Button>
            </form>
          </CardContent>
        </Card>

        {/* Preferences */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">{t('profile.preferences.title')}</CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="pt-4 space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm">{t('profile.preferences.darkMode')}</span>
              <Switch
                checked={theme === 'dark'}
                onCheckedChange={(checked) => setTheme(checked ? 'dark' : 'light')}
              />
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm">{t('profile.preferences.language')}</span>
              <Select value={i18n.language} onValueChange={(v) => i18n.changeLanguage(v)}>
                <SelectTrigger className="w-36">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="en">{t('profile.preferences.english')}</SelectItem>
                  <SelectItem value="it">{t('profile.preferences.italian')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        {/* Danger Zone */}
        <Card className="border-destructive/40">
          <CardHeader className="pb-2">
            <CardTitle className="text-base text-destructive">{t('profile.dangerZone.title')}</CardTitle>
          </CardHeader>
          <Separator />
          <CardContent className="pt-4 space-y-2">
            {(family?.members?.length ?? 0) > 1 && (
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button variant="outline" className="w-full border-destructive text-destructive hover:bg-destructive/10">
                    {t('family.dangerZone.leave')}
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>{t('family.dangerZone.leaveTitle')}</AlertDialogTitle>
                    <AlertDialogDescription>{t('family.dangerZone.leaveDescription')}</AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                    <AlertDialogAction onClick={leaveFamily}>
                      {t('family.dangerZone.leaveConfirm')}
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            )}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="destructive" className="w-full">{t('profile.dangerZone.delete')}</Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>{t('profile.dangerZone.deleteTitle')}</AlertDialogTitle>
                  <AlertDialogDescription>{t('profile.dangerZone.deleteDescription')}</AlertDialogDescription>
                </AlertDialogHeader>
                <Input
                  placeholder={t('profile.dangerZone.typeToConfirm')}
                  value={deleteConfirmText}
                  onChange={(e) => setDeleteConfirmText(e.target.value)}
                  className="mt-2"
                />
                <AlertDialogFooter>
                  <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={handleDeleteAccount}
                    disabled={deleteConfirmText !== 'DELETE'}
                    className="bg-destructive hover:bg-destructive/90"
                  >
                    {t('profile.dangerZone.deleteConfirm')}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
