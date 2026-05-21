import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, MessageSquarePlus, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { chatApi } from '@/features/chat/api/chatApi'

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function ConversationListPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: conversations = [], isLoading } = useQuery({
    queryKey: ['chat', 'conversations'],
    queryFn: chatApi.listConversations,
  })

  const createConversationMutation = useMutation({
    mutationFn: () => chatApi.createConversation(t('chat.untitledConversation')),
    onSuccess: async (conversation) => {
      await queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
      navigate(`/conversations/${conversation.id}`)
    },
  })

  const deleteConversationMutation = useMutation({
    mutationFn: (id: string) => chatApi.deleteConversation(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] }),
  })

  return (
    <div className="min-h-screen bg-background px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-6">
        <div className="flex flex-col gap-4 rounded-3xl border border-border/70 bg-card/80 p-6 shadow-sm backdrop-blur sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-primary">{t('app.name')}</p>
            <h1 className="text-3xl font-semibold tracking-tight">{t('chat.newConversation')}</h1>
            <p className="text-sm text-muted-foreground">{t('app.tagline')}</p>
          </div>
          <Button size="lg" onClick={() => createConversationMutation.mutate()} disabled={createConversationMutation.isPending}>
            {createConversationMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <MessageSquarePlus className="mr-2 h-4 w-4" />
            )}
            {t('chat.newConversation')}
          </Button>
        </div>

        {isLoading ? (
          <Card>
            <CardContent className="flex items-center gap-3 py-10 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>{t('chat.sending')}</span>
            </CardContent>
          </Card>
        ) : conversations.length === 0 ? (
          <Card className="rounded-3xl border-dashed">
            <CardContent className="py-16 text-center">
              <p className="text-lg font-medium">{t('chat.noConversations')}</p>
              <p className="mt-2 text-sm text-muted-foreground">{t('app.tagline')}</p>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {conversations.map((conversation) => (
              <Card
                key={conversation.id}
                className="cursor-pointer rounded-3xl border border-border/70 transition hover:-translate-y-0.5 hover:shadow-md"
                onClick={() => navigate(`/conversations/${conversation.id}`)}
              >
                <CardHeader>
                  <div className="flex items-start justify-between gap-3">
                    <div className="space-y-1">
                      <CardTitle className="line-clamp-2">{conversation.title || t('chat.untitledConversation')}</CardTitle>
                      <CardDescription>{formatDate(conversation.createdAt)}</CardDescription>
                    </div>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          className="text-muted-foreground hover:text-destructive"
                          onClick={(event) => event.stopPropagation()}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent onClick={(event) => event.stopPropagation()}>
                        <AlertDialogHeader>
                          <AlertDialogTitle>{t('chat.deleteConversation')}</AlertDialogTitle>
                          <AlertDialogDescription>{t('chat.confirmDelete')}</AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>{t('chat.cancel')}</AlertDialogCancel>
                          <AlertDialogAction
                            variant="destructive"
                            onClick={() => deleteConversationMutation.mutate(conversation.id)}
                          >
                            {t('chat.confirm')}
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </div>
                </CardHeader>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
