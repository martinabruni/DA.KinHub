import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Home } from 'lucide-react'
import { apiClient } from '@/api/apiClient'
import { useAuthContext } from '@/store/authContext'
import type { Family, FamilyMember } from '@/types'

export function SelectMemberPage() {
  const navigate = useNavigate()
  const { setActiveMember } = useAuthContext()

  const { data: family, isLoading } = useQuery({
    queryKey: ['family'],
    queryFn: async () => {
      const { data } = await apiClient.get<Family>('/api/families')
      return data
    },
    retry: false,
  })

  const handleMemberClick = (member: FamilyMember) => {
    setActiveMember(member)
    navigate('/', { replace: true })
  }

  if (!isLoading && !family) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-4">
        <Home className="h-10 w-10 text-muted-foreground" />
        <p className="text-muted-foreground">No family found. Please set up your family first.</p>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-10 bg-background p-4">
      <div className="text-center">
        <h1 className="text-3xl font-bold tracking-tight">Who's chatting?</h1>
        <p className="mt-2 text-muted-foreground">Select your profile to continue</p>
      </div>

      {isLoading ? (
        <div className="flex gap-6">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-36 w-36 animate-pulse rounded-2xl bg-muted" />
          ))}
        </div>
      ) : (
        <div className="flex flex-wrap justify-center gap-6">
          {(family?.members ?? []).map((member) => (
            <button
              key={member.id}
              onClick={() => handleMemberClick(member)}
              className="flex w-36 flex-col items-center gap-3 rounded-2xl p-4 transition-all hover:bg-muted/60 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            >
              <div className="flex h-20 w-20 items-center justify-center rounded-full bg-primary/10 text-2xl font-bold text-primary">
                {member.name.charAt(0).toUpperCase()}
              </div>
              <span className="text-center text-sm font-semibold leading-tight">{member.name}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
