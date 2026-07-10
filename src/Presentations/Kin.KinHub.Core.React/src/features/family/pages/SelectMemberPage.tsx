import { useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { kinHubApiClient } from '@/api/apiClient'
import { useAuthContext } from '@/store/authContext'
import { getInitials } from '@/lib/utils'
import type { Family, FamilyMember } from '@/types'

export function SelectMemberPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { setActiveMember } = useAuthContext()

  const { data: family, isLoading } = useQuery({
    queryKey: ['family'],
    queryFn: async () => {
      const { data } = await kinHubApiClient.get<Family>('/api/families')
      return data
    },
    retry: false,
  })

  const handleMemberClick = (member: FamilyMember) => {
    setActiveMember(member)
    const returnTo = searchParams.get('returnTo')

    if (returnTo) {
      if (/^https?:\/\//i.test(returnTo)) {
        window.location.assign(returnTo)
        return
      }

      navigate(returnTo, { replace: true })
      return
    }

    navigate('/', { replace: true })
  }

  if (!isLoading && !family) {
    const returnTo = searchParams.get('returnTo')
    navigate(
      returnTo ? `/onboarding?returnTo=${encodeURIComponent(returnTo)}` : '/onboarding',
      { replace: true },
    )
    return null
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-background px-4">
      <div className="mb-10 text-center">
        <h1 className="text-3xl font-bold tracking-tight">{t('selectMember.title')}</h1>
        <p className="text-muted-foreground mt-2">{t('selectMember.subtitle')}</p>
      </div>

      {isLoading ? (
        <div className="flex gap-6 flex-wrap justify-center">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="w-36 h-44 rounded-2xl" />
          ))}
        </div>
      ) : (
        <div className="flex gap-6 flex-wrap justify-center">
          {(family?.members ?? []).map((member) => (
            <button
              key={member.id}
              onClick={() => handleMemberClick(member)}
              className="group flex flex-col items-center gap-3 p-4 rounded-2xl w-36 hover:bg-muted/60 transition-all focus:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            >
              <Avatar className="w-20 h-20 group-hover:ring-2 group-hover:ring-primary transition-all">
                <AvatarFallback className="text-2xl bg-primary/10 text-primary">
                  {getInitials(member.name)}
                </AvatarFallback>
              </Avatar>
              <span className="font-semibold text-sm text-center leading-tight">{member.name}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
