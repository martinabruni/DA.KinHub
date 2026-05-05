import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { apiClient } from '@/api/apiClient'
import type { FamilyMember } from '@/types'

interface AdminCodeDialogProps {
  open: boolean
  familyId: string
  member: FamilyMember
  onSuccess: (member: FamilyMember) => void
  onClose: () => void
}

export function AdminCodeDialog({ open, familyId, member, onSuccess, onClose }: AdminCodeDialogProps) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!code.trim()) return
    setLoading(true)
    try {
      const { data } = await apiClient.post<boolean>(
        `/api/families/${familyId}/verify-admin-code`,
        { adminCode: code },
      )
      if (data) {
        onSuccess(member)
      } else {
        toast.error(t('selectMember.adminCode.error'))
        setCode('')
      }
    } catch {
      toast.error(t('selectMember.adminCode.error'))
      setCode('')
    } finally {
      setLoading(false)
    }
  }

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setCode('')
      onClose()
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[360px]">
        <DialogHeader>
          <DialogTitle>{t('selectMember.adminCode.title')}</DialogTitle>
          <DialogDescription>{t('selectMember.adminCode.description')}</DialogDescription>
        </DialogHeader>

        <Input
          type="password"
          placeholder={t('selectMember.adminCode.placeholder')}
          value={code}
          onChange={(e) => setCode(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleConfirm()}
          autoFocus
        />

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={loading}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleConfirm} disabled={!code.trim() || loading}>
            {loading ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : null}
            {t('selectMember.adminCode.confirm')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
