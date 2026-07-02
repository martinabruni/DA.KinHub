import { useCallback, useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useBlocker, useNavigate, useParams } from 'react-router-dom'
import { Check, Loader2, Mic, PencilLine, Plus, RotateCcw, Save, Trash2, X } from 'lucide-react'
import { toast } from 'sonner'
import { apiClient } from '@/api/apiClient'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import { AudioCaptureDialog } from '@/features/kin-list/components/AudioCaptureDialog'
import { clearDraftSession, createDraftFromAudio, readDraftSession, saveDraftSession } from '@/features/kin-list/draftSessionStore'
import { randomUUID } from '@/lib/utils'
import type {
  KinListDetail,
  KinListDraftFromAudioResponse,
  KinListExistingDuplicate,
  KinListItem,
  KinListItemDraftsFromAudioResponse,
  ProblemDetailsError,
} from '@/types'

interface EditableDraftItem {
  id: string
  text: string
  isSelected: boolean
  duplicateOfItemId?: string | null
}

function getProblemCode(error: unknown) {
  return (error as { response?: { data?: ProblemDetailsError } })?.response?.data?.code
}

export function KinListDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const isDraftMode = !id

  const draftSession = isDraftMode ? readDraftSession() : null
  const [title, setTitle] = useState(draftSession?.title ?? '')
  const [draftItems, setDraftItems] = useState<EditableDraftItem[]>(draftSession?.items ?? [])
  const [newItemText, setNewItemText] = useState('')
  const [appendAudioOpen, setAppendAudioOpen] = useState(false)
  const [pendingAudioItems, setPendingAudioItems] = useState<EditableDraftItem[]>([])
  const [pendingDuplicates, setPendingDuplicates] = useState<KinListExistingDuplicate[]>([])
  const [dirty, setDirty] = useState(false)
  const [editingItemId, setEditingItemId] = useState<string | null>(null)
  const [editingItemText, setEditingItemText] = useState('')

  const confirmDiscardDraft = useCallback(() => {
    if (!dirty) {
      return true
    }

    return window.confirm('Discard this draft and lose the unsaved changes?')
  }, [dirty])

  useEffect(() => {
    if (isDraftMode && !draftSession) {
      navigate('/', { replace: true })
    }
  }, [draftSession, isDraftMode, navigate])

  const draftBlocker = useBlocker(({ currentLocation, nextLocation }) => {
    return isDraftMode && dirty && currentLocation.pathname !== nextLocation.pathname
  })

  useEffect(() => {
    if (draftBlocker.state !== 'blocked') {
      return
    }

    if (confirmDiscardDraft()) {
      clearDraftSession()
      draftBlocker.proceed()
      return
    }

    draftBlocker.reset()
  }, [draftBlocker, confirmDiscardDraft])

  useEffect(() => {
    if (!dirty) {
      return
    }

    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault()
      event.returnValue = ''
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [dirty])

  const detailQuery = useQuery({
    queryKey: ['kin-list-detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await apiClient.get<KinListDetail>(`/api/lists/${id}`)
      return data
    },
  })

  // Sync the editable title from the freshly loaded server snapshot. Adjusting state
  // during render (guarded by the last-synced snapshot held in state) is the recommended
  // alternative to a setState-in-effect: https://react.dev/learn/you-might-not-need-an-effect
  const [syncedDetail, setSyncedDetail] = useState<KinListDetail | null>(null)
  if (!isDraftMode && detailQuery.data && detailQuery.data !== syncedDetail) {
    setSyncedDetail(detailQuery.data)
    setTitle(detailQuery.data.title)
    setDirty(false)
  }

  const handleConflict = async () => {
    toast.error('This list changed elsewhere. Reloading the latest version.')
    setEditingItemId(null)
    setEditingItemText('')
    await queryClient.invalidateQueries({ queryKey: ['kin-list-detail', id] })
    await queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
  }

  const createMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        title: title.trim(),
        items: draftItems.filter((item) => item.isSelected && item.text.trim()).map((item) => item.text.trim()),
      }

      if (!draftSession) {
        throw new Error('Draft session not found.')
      }

      const { data } = await apiClient.post<KinListDetail>('/api/lists', payload, {
        headers: { 'Idempotency-Key': draftSession.idempotencyKey },
      })

      return data
    },
    onSuccess: async (data) => {
      clearDraftSession()
      await queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      navigate(`/lists/${data.id}`, { replace: true })
    },
  })

  const renameMutation = useMutation({
    mutationFn: async (list: KinListDetail) => {
      const { data } = await apiClient.patch<KinListDetail>(`/api/lists/${list.id}`, { title: title.trim() }, {
        headers: { 'If-Match': list.etag },
      })
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      setDirty(false)
    },
    onError: async (error) => {
      if (getProblemCode(error) === 'etag_conflict') {
        await handleConflict()
      }
    },
  })

  const addItemMutation = useMutation({
    mutationFn: async ({ listId, etag, text }: { listId: string; etag: string; text: string }) => {
      const { data } = await apiClient.post<KinListDetail>(`/api/lists/${listId}/items`, { text }, {
        headers: { 'If-Match': etag },
      })
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      setNewItemText('')
    },
    onError: async (error) => {
      if (getProblemCode(error) === 'etag_conflict') {
        await handleConflict()
      }
    },
  })

  const updateItemMutation = useMutation({
    mutationFn: async ({ listId, item, patch }: { listId: string; item: KinListItem; patch: { text: string; isCompleted: boolean } }) => {
      const { data } = await apiClient.patch<KinListDetail>(`/api/lists/${listId}/items/${item.id}`, patch, {
        headers: { 'If-Match': item.etag },
      })
      return data
    },
    onSuccess: (data, variables) => {
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      if (editingItemId === variables.item.id) {
        setEditingItemId(null)
        setEditingItemText('')
      }
    },
    onError: async (error) => {
      if (getProblemCode(error) === 'etag_conflict') {
        await handleConflict()
      }
    },
  })

  const deleteListMutation = useMutation({
    mutationFn: async (list: KinListDetail) => {
      const { data } = await apiClient.delete<KinListDetail>(`/api/lists/${list.id}`, {
        headers: { 'If-Match': list.etag },
      })
      return data
    },
    onSuccess: async (deleted) => {
      await queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      toast.success('List deleted.', {
        duration: 5000,
        action: {
          label: 'Undo',
          onClick: async () => {
            await apiClient.post(`/api/lists/${deleted.id}/restore`, null, {
              headers: { 'If-Match': deleted.etag },
            })
            await queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
            await queryClient.invalidateQueries({ queryKey: ['kin-list-detail', deleted.id] })
          },
        },
      })
      navigate('/', { replace: true })
    },
  })

  const deleteItemMutation = useMutation({
    mutationFn: async ({ listId, item }: { listId: string; item: KinListItem }) => {
      const { data } = await apiClient.delete<KinListDetail>(`/api/lists/${listId}/items/${item.id}`, {
        headers: { 'If-Match': item.etag },
      })
      return { data, item }
    },
    onSuccess: ({ data, item }) => {
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      toast.success(`Removed "${item.text}".`, {
        duration: 5000,
        action: {
          label: 'Undo',
          onClick: async () => {
            await apiClient.post(`/api/lists/${data.id}/items/${item.id}/restore`, null, {
              headers: { 'If-Match': item.etag },
            })
            await queryClient.invalidateQueries({ queryKey: ['kin-list-detail', data.id] })
            await queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
          },
        },
      })
    },
    onError: async (error) => {
      if (getProblemCode(error) === 'etag_conflict') {
        await handleConflict()
      }
    },
  })

  const audioAppendMutation = useMutation({
    mutationFn: async (blob: Blob) => {
      if (!id) {
        throw new Error('List id is required.')
      }

      const formData = new FormData()
      formData.append('audio', blob, blob.type.includes('mp4') ? 'append.m4a' : 'append.webm')
      const { data } = await apiClient.post<KinListItemDraftsFromAudioResponse>(`/api/lists/${id}/item-drafts/from-audio`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data
    },
    onSuccess: (data) => {
      setPendingAudioItems(
        data.items.map((item) => ({
          id: randomUUID(),
          text: item.text,
          isSelected: item.isSelectedByDefault,
          duplicateOfItemId: item.duplicateOfItemId,
        })),
      )
      setPendingDuplicates(data.existingDuplicates)
    },
    onError: (error) => {
      if (getProblemCode(error) === 'no_items_detected') {
        toast.error('No new items were detected from the recording.')
      }
    },
  })

  const confirmAudioItemsMutation = useMutation({
    mutationFn: async (list: KinListDetail) => {
      const items = pendingAudioItems.filter((item) => item.isSelected && item.text.trim()).map((item) => item.text.trim())
      const { data } = await apiClient.post<KinListDetail>(`/api/lists/${list.id}/items/confirm`, { items }, {
        headers: { 'If-Match': list.etag },
      })
      return data
    },
    onSuccess: (data) => {
      queryClient.setQueryData(['kin-list-detail', data.id], data)
      queryClient.invalidateQueries({ queryKey: ['kin-lists'] })
      setPendingAudioItems([])
      setPendingDuplicates([])
    },
    onError: async (error) => {
      if (getProblemCode(error) === 'etag_conflict') {
        await handleConflict()
      }
    },
  })

  const handleBackToLists = () => {
    if (isDraftMode && !confirmDiscardDraft()) {
      return
    }

    if (isDraftMode) {
      clearDraftSession()
    }

    navigate('/', { replace: true })
  }

  const startEditingItem = (item: KinListItem) => {
    setEditingItemId(item.id)
    setEditingItemText(item.text)
  }

  const list = detailQuery.data
  const completionPercent = useMemo(() => {
    if (!list || list.totalItems === 0) {
      return 0
    }

    return Math.round((list.completedItems / list.totalItems) * 100)
  }, [list])

  if (isDraftMode) {
    return (
      <div className="space-y-6">
        <Card className="rounded-[28px]">
          <CardHeader>
            <CardTitle className="flex items-center gap-3">
              <PencilLine className="h-5 w-5 text-primary" />
              Draft before save
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <Input
              value={title}
              onChange={(event) => {
                setTitle(event.target.value)
                setDirty(true)
              }}
              placeholder="List title"
            />

            <div className="space-y-3">
              {draftItems.map((item, index) => (
                <div key={item.id} className="flex gap-3">
                  <button
                    type="button"
                    className={`mt-3 h-5 w-5 rounded border-2 ${item.isSelected ? 'border-primary bg-primary text-primary-foreground' : 'border-muted-foreground/40'}`}
                    onClick={() => {
                      setDraftItems((current) => current.map((entry) => entry.id === item.id ? { ...entry, isSelected: !entry.isSelected } : entry))
                      setDirty(true)
                    }}
                  >
                    {item.isSelected ? <Check className="h-3 w-3" /> : null}
                  </button>
                  <Textarea
                    value={item.text}
                    onChange={(event) => {
                      setDraftItems((current) => current.map((entry) => entry.id === item.id ? { ...entry, text: event.target.value } : entry))
                      setDirty(true)
                    }}
                    placeholder={`Item ${index + 1}`}
                    className="min-h-0"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="mt-1 rounded-full"
                    onClick={() => {
                      setDraftItems((current) => current.filter((entry) => entry.id !== item.id))
                      setDirty(true)
                    }}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>

            <div className="flex flex-wrap gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  setDraftItems((current) => [...current, { id: randomUUID(), text: '', isSelected: true }])
                  setDirty(true)
                }}
              >
                <Plus className="mr-2 h-4 w-4" />
                Add item
              </Button>
              <Button type="button" variant="outline" onClick={() => setAppendAudioOpen(true)}>
                <Mic className="mr-2 h-4 w-4" />
                {draftSession?.audioBlob ? 'Retry audio' : 'Add from audio'}
              </Button>
              <Button type="button" variant="ghost" onClick={handleBackToLists}>
                Discard
              </Button>
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-wrap gap-3">
          <Button
            onClick={() => {
              saveDraftSession({
                ...(draftSession ?? {
                  source: 'manual',
                  idempotencyKey: randomUUID(),
                }),
                title,
                items: draftItems,
              })
              createMutation.mutate()
            }}
            disabled={createMutation.isPending || !title.trim() || draftItems.every((item) => !item.text.trim() || !item.isSelected)}
          >
            {createMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
            Save list
          </Button>
          <Button variant="outline" onClick={handleBackToLists}>
            Back to lists
          </Button>
        </div>

        <AudioCaptureDialog
          open={appendAudioOpen}
          onOpenChange={setAppendAudioOpen}
          title="Add audio to this draft"
          description="The recording stays in memory only until the draft is updated."
          onConfirm={async (blob) => {
            const formData = new FormData()
            formData.append('audio', blob, blob.type.includes('mp4') ? 'draft.m4a' : 'draft.webm')
            const { data } = await apiClient.post<KinListDraftFromAudioResponse>('/api/list-drafts/from-audio', formData, {
              headers: { 'Content-Type': 'multipart/form-data' },
            })

            const audioDraft = createDraftFromAudio({
              title: title.trim() || data.title,
              items: data.items,
              detectedLanguage: data.detectedLanguage,
              promptVersion: data.promptVersion,
              audioBlob: blob,
            })

            const mergedItems = [...draftItems, ...audioDraft.items]
            saveDraftSession({
              ...audioDraft,
              title: title.trim() || audioDraft.title,
              items: mergedItems,
            })

            setTitle((current) => current.trim() || data.title)
            setDraftItems(mergedItems)
            setDirty(true)
          }}
        />
      </div>
    )
  }

  if (detailQuery.isLoading || !list) {
    return <Skeleton className="h-[28rem] rounded-[28px]" />
  }

  return (
    <div className="space-y-6">
      <Card className="rounded-[28px]">
        <CardHeader className="space-y-3">
          <CardTitle className="text-2xl">{list.title}</CardTitle>
          <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <span>{list.completedItems}/{list.totalItems} completed</span>
            <span>{completionPercent}% progress</span>
            <span>Updated {new Date(list.lastModifiedAt).toLocaleString()}</span>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-3 sm:flex-row">
            <Input
              value={title}
              onChange={(event) => {
                setTitle(event.target.value)
                setDirty(true)
              }}
            />
            <Button variant="outline" onClick={() => renameMutation.mutate(list)} disabled={!dirty || !title.trim() || renameMutation.isPending}>
              {renameMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
              Save title
            </Button>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row">
            <Input
              value={newItemText}
              onChange={(event) => setNewItemText(event.target.value)}
              placeholder="Add an item"
              onKeyDown={(event) => {
                if (event.key === 'Enter' && newItemText.trim()) {
                  addItemMutation.mutate({ listId: list.id, etag: list.etag, text: newItemText.trim() })
                }
              }}
            />
            <Button onClick={() => addItemMutation.mutate({ listId: list.id, etag: list.etag, text: newItemText.trim() })} disabled={!newItemText.trim() || addItemMutation.isPending}>
              <Plus className="mr-2 h-4 w-4" />
              Add item
            </Button>
            <Button variant="outline" onClick={() => setAppendAudioOpen(true)}>
              <Mic className="mr-2 h-4 w-4" />
              Add from audio
            </Button>
            <Button variant="ghost" className="text-destructive hover:text-destructive" onClick={() => deleteListMutation.mutate(list)}>
              <Trash2 className="mr-2 h-4 w-4" />
              Delete list
            </Button>
          </div>

          <div className="space-y-3">
            {list.items.map((item) => {
              const isEditing = editingItemId === item.id

              return (
                <div key={item.id} className={`rounded-2xl border p-4 ${item.isCompleted ? 'bg-muted/50 text-muted-foreground' : 'bg-card'}`}>
                  <div className="flex items-start gap-3">
                    <button
                      type="button"
                      className={`mt-2 flex h-6 w-6 items-center justify-center rounded-full border-2 ${item.isCompleted ? 'border-primary bg-primary text-primary-foreground' : 'border-muted-foreground/40'}`}
                      onClick={() => updateItemMutation.mutate({ listId: list.id, item, patch: { text: item.text, isCompleted: !item.isCompleted } })}
                      disabled={isEditing}
                    >
                      {item.isCompleted ? <Check className="h-3 w-3" /> : null}
                    </button>

                    <div className="flex-1 space-y-3">
                      {isEditing ? (
                        <Textarea
                          value={editingItemText}
                          onChange={(event) => setEditingItemText(event.target.value)}
                          className="min-h-0"
                        />
                      ) : (
                        <span className={`block pt-1 ${item.isCompleted ? 'line-through' : ''}`}>{item.text}</span>
                      )}

                      <div className="flex flex-wrap gap-2">
                        {isEditing ? (
                          <>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => updateItemMutation.mutate({ listId: list.id, item, patch: { text: editingItemText.trim(), isCompleted: item.isCompleted } })}
                              disabled={!editingItemText.trim() || updateItemMutation.isPending}
                            >
                              <Save className="mr-2 h-4 w-4" />
                              Save item
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                setEditingItemId(null)
                                setEditingItemText('')
                              }}
                            >
                              Cancel
                            </Button>
                          </>
                        ) : (
                          <Button variant="ghost" size="sm" onClick={() => startEditingItem(item)}>
                            <PencilLine className="mr-2 h-4 w-4" />
                            Edit
                          </Button>
                        )}

                        <Button variant="ghost" size="sm" onClick={() => deleteItemMutation.mutate({ listId: list.id, item })}>
                          <Trash2 className="mr-2 h-4 w-4 text-destructive" />
                          Remove
                        </Button>
                      </div>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        </CardContent>
      </Card>

      {pendingAudioItems.length > 0 ? (
        <Card className="rounded-[28px] border-emerald-200 bg-emerald-50/70">
          <CardHeader>
            <CardTitle>Audio proposals</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {pendingDuplicates.length > 0 ? (
              <div className="rounded-2xl border border-amber-300/40 bg-amber-50 p-4 text-sm text-amber-900">
                Existing duplicates detected: {pendingDuplicates.map((item) => item.text).join(', ')}.
              </div>
            ) : null}

            <div className="space-y-3">
              {pendingAudioItems.map((item) => (
                <div key={item.id} className="flex gap-3">
                  <button
                    type="button"
                    className={`mt-3 h-5 w-5 rounded border-2 ${item.isSelected ? 'border-primary bg-primary text-primary-foreground' : 'border-muted-foreground/40'}`}
                    onClick={() => setPendingAudioItems((current) => current.map((entry) => entry.id === item.id ? { ...entry, isSelected: !entry.isSelected } : entry))}
                  >
                    {item.isSelected ? <Check className="h-3 w-3" /> : null}
                  </button>
                  <Textarea
                    value={item.text}
                    onChange={(event) => setPendingAudioItems((current) => current.map((entry) => entry.id === item.id ? { ...entry, text: event.target.value } : entry))}
                    className="min-h-0"
                  />
                </div>
              ))}
            </div>

            <div className="flex flex-wrap gap-3">
              <Button onClick={() => confirmAudioItemsMutation.mutate(list)} disabled={confirmAudioItemsMutation.isPending}>
                {confirmAudioItemsMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Plus className="mr-2 h-4 w-4" />}
                Confirm selected items
              </Button>
              <Button variant="outline" onClick={() => setAppendAudioOpen(true)}>
                <RotateCcw className="mr-2 h-4 w-4" />
                Retry audio
              </Button>
              <Button variant="ghost" onClick={() => { setPendingAudioItems([]); setPendingDuplicates([]) }}>
                Clear proposals
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : null}

      <AudioCaptureDialog
        open={appendAudioOpen}
        onOpenChange={setAppendAudioOpen}
        title="Add items from audio"
        description="The recording is sent once to generate item proposals. Nothing is added until you confirm the proposals."
        onConfirm={async (blob) => {
          await audioAppendMutation.mutateAsync(blob)
        }}
      />
    </div>
  )
}
