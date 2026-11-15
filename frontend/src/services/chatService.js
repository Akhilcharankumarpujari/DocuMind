import { authenticatedFetch, apiRequest } from './apiClient'

// Uses fetch streaming because the chat endpoint accepts a POST body.
// Backend SSE contract sends:
// data: {"type":"sources","sources":[...]}
// data: {"type":"token","content":"..."}
// data: {"type":"done","answer":"..."}
export async function streamQuestion(documentId, question, onToken, onSources, onDone, signal) {
  const response = await authenticatedFetch(`/api/documents/${documentId}/ask/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: JSON.stringify({ question }),
    signal,
  })

  if (!response.ok || !response.body) {
    let errorMsg = 'Unable to start the assistant response.'
    try {
      const errJson = await response.json()
      errorMsg = errJson.message || errorMsg
    } catch {
      const errText = await response.text().catch(() => '')
      if (errText) errorMsg = errText
    }
    throw new Error(errorMsg)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let receivedAnyToken = false

  while (true) {
    const { value, done } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })
    const events = buffer.split('\n\n')
    buffer = events.pop() || ''
    for (const event of events) {
      const line = event.split('\n').find((part) => part.startsWith('data:'))
      if (!line) continue
      try {
        const payload = JSON.parse(line.slice(5).trim())
        if (payload.type === 'sources') {
          onSources?.(payload.sources || [])
        }
        if (payload.type === 'token') {
          receivedAnyToken = true
          onToken?.(payload.content || '')
        }
        if (payload.type === 'done') {
          // If no incremental tokens were received, use final answer
          if (!receivedAnyToken && payload.answer) {
            onToken?.(payload.answer)
          }
          onDone?.(payload.answer)
        }
        if (payload.type === 'error') {
          throw new Error(payload.error || 'Streaming error occurred.')
        }
      } catch (parseErr) {
        if (parseErr.message && !parseErr.message.includes('JSON')) {
          throw parseErr
        }
      }
    }
  }
}

export async function askQuestion(documentId, question) {
  return apiRequest(`/api/documents/${documentId}/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question }),
  })
}

export async function getDocumentConversations(documentId) {
  try {
    return await apiRequest(`/api/documents/${documentId}/conversations`)
  } catch {
    return []
  }
}

export async function getConversationMessages(conversationId) {
  try {
    return await apiRequest(`/api/conversations/${conversationId}/messages`)
  } catch {
    return []
  }
}
