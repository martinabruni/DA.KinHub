import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { Mic, MoreHorizontal, Plus, ShoppingBasket, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { apiClient } from '@/api/apiClient'
import { AudioCaptureDialog } from '@/features/kin-list/components/AudioCaptureDialog'
import { createDraftFromAudio, createEmptyDraft } from '@/features/kin-list/draftSessionStore'
import type { KinListDetail, KinListDraftFromAudioResponse, KinListSummary, ProblemDetailsError } from '@/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter } from '@/components/ui/card'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Progress } from '@/components/ui/progress'
import { Skeleton } from '@/components/ui/skeleton'

function getProblemCode(error: unknown) {
  const response = (error as { response?: { data?: ProblemDetailsError } })?.response?.data
  return response?.code
}

export function KinListsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isAudioOpen, setIsAudioOpen] = useState(false)

  const listsQuery = useQuery({
    queryKey: ['kin-lists'],
    queryFn: async () => {
      const { data } = await apiClient.get<KinListSummary[]>('/api/lists')
      return data
    },
  })

  const audioDraftMutation = useMutation({
    mutationFn: async (blob: Blob) => {
      const formData = new FormData()
      formData.append('audio', blob, blob.type.includes('mp4') ? 'recording.m4a' : 'recording.webm')
      const { data } = await apiClient.post<KinListDraftFromAudioResponse>('/api/list-drafts/from-audio', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return { data, blob }
    },
    onSuccess: ({ data, blob }) => {
      createDraftFromAudio({
        title: data.title,
        items: data.items,
        detectedLanguage: data.detectedLanguage,
        promptVersion: data.promptVersion,
        audioBlob: blob,
      })
      navigate('/draft/new')
    },
    onError: (error) => {
      if (getProblemCode(error) === 'no_items_detected') {
        toast.error('No list items were detected. Try speaking more clearly or use manual creation.')
      }
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (list: KinListSummary) => {
      const { data } = await apiClient.delete<KinListDetail>(`/api/lists/${list.id}`, {
        headers: { 'If-Match': list.etag },
      })

      return { list, deleted: data }
    },
    onSuccess: ({ list, deleted }) => {
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      queryClient.removeQueries({ queryKey: ['kin-list-detail', list.id] })
      toast.success(`"${list.title}" moved to trash.`, {
        duration: 5000,
        action: {
          label: 'Undo',
          onClick: async () => {
            await apiClient.post(`/api/lists/${list.id}/restore`, null, {
              headers: { 'If-Match': deleted.etag },
            })
            queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
          },
        },
      })
    },
  })

  const startManualDraft = () => {
    createEmptyDraft()
    navigate('/draft/new')
  }

  const lists = listsQuery.data ?? []

  return (
    <div className="space-y-6">
      <section className="relative overflow-hidden rounded-[32px] border bg-gradient-to-br from-emerald-500 via-emerald-400 to-lime-300 px-6 py-7 text-emerald-950 shadow-lg shadow-emerald-900/10">
        <div className="absolute right-[-3rem] top-[-3rem] h-32 w-32 rounded-full bg-white/25 blur-2xl" />
        <div className="relative max-w-xl space-y-3">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-900/75">Family grocery flow</p>
          <h2 className="text-3xl font-semibold leading-tight sm:text-4xl">Capture lists quickly, then refine only what matters.</h2>
          <p className="max-w-md text-sm text-emerald-950/80 sm:text-base">
            Start from voice or type a draft manually. Nothing is saved until you confirm the draft.
          </p>
          <div className="flex flex-wrap gap-3 pt-2">
            <Button type="button" variant="secondary" className="rounded-full bg-white text-emerald-950 hover:bg-white/90" onClick={() => setIsAudioOpen(true)}>
              <Mic className="mr-2 h-4 w-4" />
              Record a new list
            </Button>
            <Button type="button" variant="outline" className="rounded-full border-white/60 bg-transparent text-emerald-950 hover:bg-white/15" onClick={startManualDraft}>
              <Plus className="mr-2 h-4 w-4" />
              Create manually
            </Button>
          </div>
        </div>
      </section>

      {listsQuery.isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-48 rounded-[28px]" />
          ))}
        </div>
      ) : lists.length === 0 ? (
        <section className="rounded-[28px] border border-dashed bg-muted/30 px-6 py-16 text-center">
          <div className="mx-auto flex max-w-sm flex-col items-center gap-4">
            <div className="rounded-full bg-primary/10 p-4 text-primary">
              <ShoppingBasket className="h-8 w-8" />
            </div>
            <div className="space-y-2">
              <h3 className="text-xl font-semibold">No lists yet</h3>
              <p className="text-sm text-muted-foreground">
                Start with the microphone for a fast capture, or create a blank draft and add items by hand.
              </p>
            </div>
            <div className="flex flex-wrap justify-center gap-3">
              <Button onClick={() => setIsAudioOpen(true)} className="rounded-full">
                <Mic className="mr-2 h-4 w-4" />
                Record
              </Button>
              <Button variant="outline" onClick={startManualDraft} className="rounded-full">
                <Plus className="mr-2 h-4 w-4" />
                Create manually
              </Button>
            </div>
          </div>
        </section>
      ) : (
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-xl font-semibold">Shared family lists</h3>
              <p className="text-sm text-muted-foreground">Completed lists stay at the bottom. Active items stay visible first.</p>
            </div>
            <Button variant="outline" className="hidden rounded-full sm:flex" onClick={startManualDraft}>
              <Plus className="mr-2 h-4 w-4" />
              New draft
            </Button>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {lists.map((list) => {
              const progressValue = list.totalItems === 0 ? 0 : Math.round((list.completedItems / list.totalItems) * 100)

              return (
                <Card
                  key={list.id}
                  className={`overflow-hidden rounded-[28px] border transition hover:-translate-y-0.5 hover:shadow-lg ${list.isCompleted ? 'bg-muted/50 text-muted-foreground' : 'bg-card'}`}
                >
                  <CardContent className="space-y-4 p-5">
                    <div className="flex items-start gap-3">
                      <Link to={`/lists/${list.id}`} className="flex-1 space-y-2">
                        <div className="flex items-center gap-3">
                          <div className={`rounded-2xl p-3 ${list.isCompleted ? 'bg-muted' : 'bg-primary/10 text-primary'}`}>
                            <ShoppingBasket className="h-5 w-5" />
                          </div>
                          <div className="min-w-0">
                            <p className="truncate text-lg font-semibold">{list.title}</p>
                            <p className="text-xs uppercase tracking-[0.18em] text-muted-foreground">
                              {list.completedItems}/{list.totalItems} completed
                            </p>
                          </div>
                        </div>
                        <Progress value={progressValue} className="h-2" />
                      </Link>

                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon" className="rounded-full">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => navigate(`/lists/${list.id}`)}>Open</DropdownMenuItem>
                          <DropdownMenuItem className="text-destructive" onClick={() => deleteMutation.mutate(list)}>
                            <Trash2 className="mr-2 h-4 w-4" />
                            Delete
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </CardContent>
                  <CardFooter className="flex items-center justify-between border-t bg-muted/20 px-5 py-4 text-xs text-muted-foreground">
                    <span>Updated {new Date(list.lastModifiedAt).toLocaleString()}</span>
                    {list.isCompleted ? <span>Archived at bottom</span> : <span>Active</span>}
                  </CardFooter>
                </Card>
              )
            })}
          </div>
        </section>
      )}

      <div className="fixed bottom-6 left-1/2 z-40 -translate-x-1/2 md:hidden">
        <Button className="h-14 rounded-full px-6 shadow-xl" onClick={() => setIsAudioOpen(true)}>
          <Mic className="mr-2 h-5 w-5" />
          Record
        </Button>
      </div>

      <AudioCaptureDialog
        open={isAudioOpen}
        onOpenChange={setIsAudioOpen}
        title="New audio draft"
        description="Record up to 60 seconds. The audio stays in memory only long enough to build the draft."
        onConfirm={async (blob) => {
          await audioDraftMutation.mutateAsync(blob)
        }}
      />
    </div>
  )
}
