export interface User { id: string; email: string; familyId: string | null }
export interface FamilyMember { id: string; name: string }
export interface Family { id: string; name: string; members: FamilyMember[] }
export interface LoginRequest { email: string; password: string }
export interface RegisterRequest { email: string; password: string }
export interface AuthTokens { accessToken: string; refreshToken: string }

export interface ChatConversation {
  id: string
  familyMemberId: string
  title: string
  createdAt: string
  updatedAt: string
}

export type ChatMessageRole = 'User' | 'Assistant' | 'Tool'
export type ChatToolCallStatus = 'Pending' | 'Confirmed' | 'Rejected'
export type ChatMessageRoleValue = ChatMessageRole | number
export type ChatToolCallStatusValue = ChatToolCallStatus | number

export interface ChatMessage {
  id: string
  conversationId: string
  role: ChatMessageRoleValue
  content: string
  createdAt: string
}

export interface ChatToolCall {
  id: string
  messageId: string
  toolName: string
  argumentsJson: string
  status: ChatToolCallStatusValue
  createdAt: string
}

export interface ConversationWithMessages {
  conversation: ChatConversation
  messages: ChatMessage[]
  pendingToolCalls: ChatToolCall[]
}

export interface SendMessageResponse {
  assistantMessage: ChatMessage
  pendingToolCall?: ChatToolCall
  hasPendingToolCall: boolean
}
