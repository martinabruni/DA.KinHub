import { useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Menu, MessageSquarePlus, PanelLeftClose, SendHorizontal, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import { chatApi } from '@/features/chat/api/chatApi'
import { cn } from '@/lib/utils'
import type { ChatMessage, ChatMessageRole, ChatToolCall } from '@/types'

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}

function normalizeMessageRole(role: ChatMessage['role']): ChatMessageRole {
  switch (String(role)) {
    case '0':
    case 'User':
    case 'user':
      return 'User'
    case '1':
    case 'Assistant':
    case 'assistant':
      return 'Assistant'
    case '2':
    case 'Tool':
    case 'tool':
      return 'Tool'
    default:
      return 'Assistant'
  }
}

function formatToolArguments(argumentsJson: string, emptyArgumentsLabel: string) {
  try {
    const parsedArguments = JSON.parse(argumentsJson)

    if (
      parsedArguments === null ||
      parsedArguments === '' ||
      (Array.isArray(parsedArguments) && parsedArguments.length === 0) ||
      (typeof parsedArguments === 'object' && !Array.isArray(parsedArguments) && Object.keys(parsedArguments).length === 0)
    ) {
      return emptyArgumentsLabel
    }

    return JSON.stringify(parsedArguments, null, 2)
  } catch {
    const trimmedArguments = argumentsJson.trim()
    return trimmedArguments.length > 0 ? trimmedArguments : emptyArgumentsLabel
  }
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const { t } = useTranslation()
  const role = normalizeMessageRole(message.role)
  const isUser = role === 'User'
  const content = message.content.trim()

  if (!content) return null

  return (
    <div className={cn('flex w-full', isUser ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[85%] rounded-3xl px-4 py-3 shadow-sm',
          isUser ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground'
        )}
      >
        <div className="mb-2 flex items-center justify-between gap-4 text-xs font-medium uppercase tracking-wide opacity-70">
          <p>{t(`chat.senders.${role.toLowerCase()}`)}</p>
          <p>{formatTime(message.createdAt)}</p>
        </div>
        <p className="whitespace-pre-wrap text-sm leading-6">{content}</p>
      </div>
    </div>
  )
}

export function ConversationDetailPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { id } = useParams<{ id: string }>()
  const conversationId = id ?? ''
  const [message, setMessage] = useState('')
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [localPendingToolCall, setLocalPendingToolCall] = useState<{
    conversationId: string
    value: ChatToolCall | null
  } | null>(null)
  const bottomRef = useRef<HTMLDivElement | null>(null)

  const { data: conversations = [] } = useQuery({
    queryKey: ['chat', 'conversations'],
    queryFn: chatApi.listConversations,
  })

  const conversationQuery = useQuery({
    queryKey: ['chat', 'conversation', conversationId],
    queryFn: () => chatApi.getConversation(conversationId),
    enabled: !!id,
  })

  const queryPendingToolCall = conversationQuery.data?.pendingToolCalls[0] ?? null
  const pendingToolCall = localPendingToolCall?.conversationId === conversationId
    ? localPendingToolCall.value
    : queryPendingToolCall

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [conversationQuery.data?.messages.length, pendingToolCall?.id])

  const createConversationMutation = useMutation({
    mutationFn: () => chatApi.createConversation(t('chat.untitledConversation')),
    onSuccess: async (conversation) => {
      await queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
      navigate(`/conversations/${conversation.id}`)
    },
  })

  const sendMessageMutation = useMutation({
    mutationFn: (text: string) => chatApi.sendMessage(conversationId, text),
    onSuccess: async (response) => {
      setMessage('')
      setLocalPendingToolCall({
        conversationId,
        value: response.hasPendingToolCall ? response.pendingToolCall ?? null : null,
      })
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversation', conversationId] }),
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] }),
      ])
    },
  })

  const toolCallMutation = useMutation({
    mutationFn: ({ toolCallId, action }: { toolCallId: string; action: 'confirm' | 'reject' }) => {
      return action === 'confirm' ? chatApi.confirmToolCall(toolCallId) : chatApi.rejectToolCall(toolCallId)
    },
    onSuccess: async () => {
      setLocalPendingToolCall({ conversationId, value: null })
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversation', conversationId] }),
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] }),
      ])
    },
  })

  const translatedToolName = useMemo(() => {
    if (!pendingToolCall) return ''
    return t(`chat.toolNames.${pendingToolCall.toolName}`, { defaultValue: pendingToolCall.toolName })
  }, [pendingToolCall, t])

  const formattedToolArguments = useMemo(() => {
    if (!pendingToolCall) return ''
    return formatToolArguments(pendingToolCall.argumentsJson, t('chat.noToolArguments'))
  }, [pendingToolCall, t])

  const submitMessage = () => {
    const trimmed = message.trim()
    if (!trimmed) return
    sendMessageMutation.mutate(trimmed)
  }

  const activeConversationId = conversationQuery.data?.conversation.id

  if (!id) return <Navigate to="/conversations" replace />

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      {sidebarOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-20 bg-black/40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
          aria-label="Close sidebar"
        />
      ) : null}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 flex w-80 max-w-[85vw] flex-col border-r border-border bg-card/95 p-4 backdrop-blur transition-transform lg:static lg:max-w-none lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex items-center justify-between gap-3 border-b border-border pb-4">
          <div>
            <p className="text-sm font-medium text-primary">{t('app.name')}</p>
            <p className="text-xs text-muted-foreground">{t('app.tagline')}</p>
          </div>
          <Button variant="ghost" size="icon-sm" className="lg:hidden" onClick={() => setSidebarOpen(false)}>
            <PanelLeftClose className="h-4 w-4" />
          </Button>
        </div>

        <Button className="mt-4" onClick={() => createConversationMutation.mutate()} disabled={createConversationMutation.isPending}>
          {createConversationMutation.isPending ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <MessageSquarePlus className="mr-2 h-4 w-4" />
          )}
          {t('chat.newConversation')}
        </Button>

        <div className="mt-4 flex-1 space-y-2 overflow-y-auto pr-1">
          {conversations.map((conversation) => (
            <button
              key={conversation.id}
              type="button"
              className={cn(
                'w-full rounded-2xl border px-4 py-3 text-left transition hover:bg-muted',
                conversation.id === activeConversationId ? 'border-primary bg-primary/10' : 'border-border bg-background'
              )}
              onClick={() => {
                navigate(`/conversations/${conversation.id}`)
                setSidebarOpen(false)
              }}
            >
              <p className="line-clamp-1 font-medium">{conversation.title || t('chat.untitledConversation')}</p>
              <p className="mt-1 text-xs text-muted-foreground">{formatDate(conversation.updatedAt)}</p>
            </button>
          ))}
        </div>
      </aside>

      <main className="flex min-h-screen flex-1 flex-col">
        <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-border bg-background/90 px-4 py-4 backdrop-blur sm:px-6">
          <Button variant="outline" size="icon-sm" className="lg:hidden" onClick={() => setSidebarOpen(true)}>
            <Menu className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-lg font-semibold">{conversationQuery.data?.conversation.title || t('chat.untitledConversation')}</h1>
            <p className="text-sm text-muted-foreground">{t('app.tagline')}</p>
          </div>
        </header>

        <div className="flex flex-1 flex-col px-4 py-4 sm:px-6">
          {conversationQuery.isPending ? (
            <div className="flex flex-1 items-center justify-center gap-3 text-muted-foreground">
              <Loader2 className="h-5 w-5 animate-spin" />
              <span>{t('chat.sending')}</span>
            </div>
          ) : conversationQuery.data ? (
            <>
              <div className="flex-1 space-y-4 overflow-y-auto pb-4">
                {conversationQuery.data.messages.map((chatMessage) => (
                  <MessageBubble key={chatMessage.id} message={chatMessage} />
                ))}
                <div ref={bottomRef} />
              </div>

              {pendingToolCall ? (
                <Card className="mb-4 rounded-3xl border border-amber-200 bg-amber-50/70 dark:border-amber-500/20 dark:bg-amber-500/10">
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <Sparkles className="h-4 w-4 text-amber-600" />
                      <CardTitle>{t('chat.pendingAction')}</CardTitle>
                    </div>
                    <CardDescription>{t('chat.pendingActionDesc')}</CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div>
                      <p className="text-sm font-medium">{translatedToolName}</p>
                      <div className="mt-2 overflow-x-auto rounded-2xl bg-background/80 p-4 text-xs text-muted-foreground">
                        <pre className="whitespace-pre-wrap font-sans">{formattedToolArguments}</pre>
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-3">
                      <Button
                        className="bg-emerald-600 text-white hover:bg-emerald-700"
                        onClick={() => toolCallMutation.mutate({ toolCallId: pendingToolCall.id, action: 'confirm' })}
                        disabled={toolCallMutation.isPending}
                      >
                        {t('chat.confirm')}
                      </Button>
                      <Button
                        variant="destructive"
                        onClick={() => toolCallMutation.mutate({ toolCallId: pendingToolCall.id, action: 'reject' })}
                        disabled={toolCallMutation.isPending}
                      >
                        {t('chat.reject')}
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ) : null}

              <Card className="rounded-3xl border border-border/70">
                <CardContent className="p-4">
                  <form
                    className="flex flex-col gap-3 sm:flex-row sm:items-end"
                    onSubmit={(event) => {
                      event.preventDefault()
                      submitMessage()
                    }}
                  >
                    <Textarea
                      value={message}
                      onChange={(event) => setMessage(event.target.value)}
                      placeholder={t('chat.typeMessage')}
                      className="min-h-24 flex-1 resize-none"
                      disabled={sendMessageMutation.isPending}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' && !event.shiftKey) {
                          event.preventDefault()
                          submitMessage()
                        }
                      }}
                    />
                    <Button type="submit" size="lg" disabled={sendMessageMutation.isPending || !message.trim()}>
                      {sendMessageMutation.isPending ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <SendHorizontal className="mr-2 h-4 w-4" />
                      )}
                      {sendMessageMutation.isPending ? t('chat.sending') : t('chat.send')}
                    </Button>
                  </form>
                </CardContent>
              </Card>
            </>
          ) : (
            <div className="flex flex-1 items-center justify-center text-muted-foreground">{t('errors.notFound')}</div>
          )}
        </div>
      </main>
    </div>
  )
}
