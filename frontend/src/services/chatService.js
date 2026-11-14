import { API_BASE_URL } from './apiClient'

// Uses fetch streaming because the chat endpoint accepts a POST body.
// Backend SSE contract sends:
// data: {"type":"sources","sources":[...]}
// data: {"type":"token","content":"..."}
// data: {"type":"done","answer":"..."}
export async function streamQuestion(documentId, question, onToken, onSources, onDone, signal) {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/ask/stream`, {
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
      } catch (err) {
        if (err.message && err.message.includes('Streaming error')) throw err
        // Ignore parsing errors for heartbeats or non-JSON comments
      }
    }
  }
}

export async function askQuestion(documentId, question) {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question }),
  })
  if (!response.ok) {
    const text = await response.text().catch(() => 'Request failed')
    throw new Error(text)
  }
  return response.json()
}

export async function getDocumentConversations(documentId) {
  const response = await fetch(`${API_BASE_URL}/api/documents/${documentId}/conversations`)
  if (!response.ok) return []
  return response.json()
}

export async function getConversationMessages(conversationId) {
  const response = await fetch(`${API_BASE_URL}/api/conversations/${conversationId}/messages`)
  if (!response.ok) return []
  return response.json()
}
