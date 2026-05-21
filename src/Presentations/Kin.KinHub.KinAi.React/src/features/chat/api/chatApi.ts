import { apiClient } from '@/api/apiClient'
import type { ChatConversation, ConversationWithMessages, SendMessageResponse, ChatToolCall } from '@/types'

const ACTIVE_MEMBER_KEY = 'activeMember'

function getMemberIdHeader(): Record<string, string> {
  try {
    const raw = sessionStorage.getItem(ACTIVE_MEMBER_KEY)
    if (!raw) return {}
    const member = JSON.parse(raw) as { id: string }
    return member?.id ? { 'X-Member-Id': member.id } : {}
  } catch {
    return {}
  }
}

export const chatApi = {
  listConversations: async (): Promise<ChatConversation[]> => {
    const { data } = await apiClient.get<ChatConversation[]>('/api/chat/conversations', {
      headers: getMemberIdHeader(),
    })
    return data
  },
  createConversation: async (title: string): Promise<ChatConversation> => {
    const { data } = await apiClient.post<ChatConversation>('/api/chat/conversations', { title }, {
      headers: getMemberIdHeader(),
    })
    return data
  },
  getConversation: async (id: string): Promise<ConversationWithMessages> => {
    const { data } = await apiClient.get<ConversationWithMessages>(`/api/chat/conversations/${id}`, {
      headers: getMemberIdHeader(),
    })
    return data
  },
  deleteConversation: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/chat/conversations/${id}`, {
      headers: getMemberIdHeader(),
    })
  },
  sendMessage: async (conversationId: string, message: string): Promise<SendMessageResponse> => {
    const { data } = await apiClient.post<SendMessageResponse>(
      `/api/chat/conversations/${conversationId}/messages`,
      { message },
      { headers: getMemberIdHeader() }
    )
    return data
  },
  confirmToolCall: async (toolCallId: string): Promise<ChatToolCall> => {
    const { data } = await apiClient.post<ChatToolCall>(`/api/chat/tool-calls/${toolCallId}/confirm`, null, {
      headers: getMemberIdHeader(),
    })
    return data
  },
  rejectToolCall: async (toolCallId: string): Promise<ChatToolCall> => {
    const { data } = await apiClient.post<ChatToolCall>(`/api/chat/tool-calls/${toolCallId}/reject`, null, {
      headers: getMemberIdHeader(),
    })
    return data
  },
}
