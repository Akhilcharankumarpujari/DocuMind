import { useEffect, useRef, useState } from 'react'
import { Bot, Send, Sparkles, Copy, ThumbsUp, ThumbsDown, FileText, RotateCcw, AlertCircle, BookOpen, ChevronDown } from 'lucide-react'
import { starterMessages } from '../../data/mockData'
import { streamQuestion, getDocumentConversations, getConversationMessages } from '../../services/chatService'

const suggestions = ['Summarize this document', 'What are the key findings?', 'Explain the methodology']

export default function ChatPanel({ document, actionPrompt, onClearActionPrompt, onCitationClick }) {
  const [messages, setMessages] = useState(starterMessages)
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [isAtBottom, setIsAtBottom] = useState(true)

  const endRef = useRef(null)
  const messagesAreaRef = useRef(null)
  const isAtBottomRef = useRef(true)
  const abortControllerRef = useRef(null)

  // Track user scroll position in the conversation messages area
  const handleScroll = (e) => {
    const el = e.currentTarget
    // If user is within 85px of the bottom, treat them as viewing the latest messages
    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight <= 85
    if (isAtBottomRef.current !== atBottom) {
      isAtBottomRef.current = atBottom
      setIsAtBottom(atBottom)
    }
  }

  // Scroll to bottom only if user was already at the bottom (ChatGPT-style)
  useEffect(() => {
    if (isAtBottomRef.current) {
      endRef.current?.scrollIntoView({ behavior: 'smooth' })
    }
  }, [messages, loading])

  // Scroll down to the latest message explicitly
  const scrollToLatest = () => {
    isAtBottomRef.current = true
    setIsAtBottom(true)
    endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  // Load conversation history when active document changes
  useEffect(() => {
    let isCancelled = false

    if (!document?.id) {
      setMessages(starterMessages)
      return
    }

    async function loadHistory() {
      try {
        const conversations = await getDocumentConversations(document.id)
        if (isCancelled) return

        if (conversations && conversations.length > 0) {
          const latestConv = conversations[0]
          const histMessages = await getConversationMessages(latestConv.id)
          if (isCancelled) return

          if (histMessages && histMessages.length > 0) {
            const mapped = histMessages.map(m => ({
              id: m.id,
              role: m.role?.toLowerCase() === 'user' ? 'user' : 'assistant',
              content: m.content,
              sources: m.sources || []
            }))
            setMessages(mapped)
            isAtBottomRef.current = true
            setIsAtBottom(true)
            return
          }
        }
        setMessages(starterMessages)
      } catch {
        if (!isCancelled) {
          setMessages(starterMessages)
        }
      }
    }

    loadHistory()

    return () => {
      isCancelled = true
      abortControllerRef.current?.abort()
    }
  }, [document?.id])

  // React to quick action prompt from Header
  useEffect(() => {
    if (actionPrompt && !loading && document) {
      send(actionPrompt)
      onClearActionPrompt?.()
    }
  }, [actionPrompt])

  const send = async (text = input) => {
    const question = text.trim()
    if (!question || loading) return

    if (!document) {
      setMessages(prev => [
        ...prev,
        { id: crypto.randomUUID(), role: 'user', content: question },
        {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: 'Please upload or select a PDF first, and I will answer questions grounded in that document.',
          sources: []
        }
      ])
      setInput('')
      scrollToLatest()
      return
    }

    setInput('')
    setMessages(prev => [...prev, { id: crypto.randomUUID(), role: 'user', content: question }])
    setLoading(true)
    scrollToLatest()

    const assistantMsgId = crypto.randomUUID()
    setMessages(prev => [...prev, { id: assistantMsgId, role: 'assistant', content: '', sources: [] }])

    const abortController = new AbortController()
    abortControllerRef.current = abortController

    try {
      await streamQuestion(
        document.id,
        question,
        // Token received incrementally
        (token) => {
          setMessages(prev => prev.map(msg =>
            msg.id === assistantMsgId ? { ...msg, content: msg.content + token } : msg
          ))
        },
        // Sources received
        (sources) => {
          setMessages(prev => prev.map(msg =>
            msg.id === assistantMsgId ? { ...msg, sources } : msg
          ))
        },
        // Done received
        () => {
          // Stream completed cleanly
        },
        abortController.signal
      )
    } catch (err) {
      if (err.name === 'AbortError') return

      setMessages(prev => prev.map(msg =>
        msg.id === assistantMsgId
          ? {
              ...msg,
              content: msg.content || `I encountered an issue answering your question: ${err.message}`,
              isError: true
            }
          : msg
      ))
    } finally {
      setLoading(false)
      abortControllerRef.current = null
    }
  }

  const resetChat = () => {
    abortControllerRef.current?.abort()
    setLoading(false)
    setMessages(starterMessages)
    scrollToLatest()
  }

  return (
    <section className="chat-panel">
      <div className="chat-heading">
        <div>
          <span className="eyebrow">YOUR AI ASSISTANT</span>
          <h2><span className="online-dot"/> Chat with document</h2>
        </div>
        <button className="icon-button" title="New chat" onClick={resetChat}>
          <RotateCcw size={16}/>
        </button>
      </div>

      <div className="chat-context">
        <FileText size={15}/>
        <span>{document?.name || 'No document selected'}</span>
        <span className={`context-status ${document?.status?.toLowerCase() === 'ready' ? 'status-ready' : ''}`}>
          {document ? document.status || 'Active' : 'Waiting'}
        </span>
      </div>

      {/* INDEPENDENT SCROLL CONTAINER FOR MESSAGES */}
      <div className="messages-area" ref={messagesAreaRef} onScroll={handleScroll}>
        {messages.map(message => (
          <article key={message.id} className={`message ${message.role}`}>
            {message.role === 'assistant' && (
              <div className="bot-avatar"><Bot size={17}/></div>
            )}
            <div className="message-body">
              {message.role === 'assistant' && (
                <div className="message-author">DocuMind <span>AI</span></div>
              )}
              <div className={`message-bubble ${message.isError ? 'message-bubble--error' : ''}`}>
                {message.content || (loading ? <span className="typing-dots">Thinking…</span> : '')}

                {/* Grounded Source Citations */}
                {message.sources && message.sources.length > 0 && (
                  <div className="message-citations">
                    <div className="citations-header">
                      <BookOpen size={12}/>
                      <span>Cited Sources ({message.sources.length}):</span>
                    </div>
                    <div className="citations-chips">
                      {message.sources.map((src, idx) => {
                        const pageNum = src.page || src.pageNumber
                        return (
                          <button
                            key={idx}
                            className="citation-chip"
                            title={src.snippet ? `Page ${pageNum}: "${src.snippet}"` : `Jump to Page ${pageNum}`}
                            onClick={() => onCitationClick?.(pageNum)}
                          >
                            <strong>Page {pageNum}</strong>
                            {src.score ? <span className="citation-score">{(src.score * 100).toFixed(0)}%</span> : null}
                          </button>
                        )
                      })}
                    </div>
                  </div>
                )}
              </div>

              {message.role === 'assistant' && message.content && !message.isError && (
                <div className="message-tools">
                  <button title="Copy" onClick={() => navigator.clipboard?.writeText(message.content)}>
                    <Copy size={13}/>
                  </button>
                  <button title="Helpful"><ThumbsUp size={13}/></button>
                  <button title="Not helpful"><ThumbsDown size={13}/></button>
                </div>
              )}
            </div>
          </article>
        ))}
        <div ref={endRef}/>
      </div>

      {/* Floating Scroll To Bottom Button when reading older messages */}
      {!isAtBottom && messages.length > 2 && (
        <button
          type="button"
          className="scroll-bottom-btn"
          onClick={scrollToLatest}
          title="Scroll to latest message"
          aria-label="Scroll to latest message"
        >
          <ChevronDown size={16}/>
        </button>
      )}

      {!loading && messages.length <= 1 && (
        <div className="suggestion-area">
          <span>TRY ASKING</span>
          {suggestions.map(item => (
            <button key={item} onClick={() => send(item)}>{item}</button>
          ))}
        </div>
      )}

      {/* Fixed Composer Form */}
      <div className="composer-wrap">
        <div className="composer">
          <textarea
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault()
                send()
              }
            }}
            placeholder={
              document
                ? (document.status === 'Ready' ? 'Ask anything about your PDF…' : `Document status: ${document.status}...`)
                : 'Upload a PDF to get started…'
            }
            disabled={loading || (document && document.status !== 'Ready')}
            rows={2}
          />
          <button
            className="send-button"
            disabled={!input.trim() || loading || (document && document.status !== 'Ready')}
            onClick={() => send()}
            aria-label="Send message"
          >
            <Send size={17}/>
          </button>
        </div>
        <div className="composer-footer">
          <span><Sparkles size={12}/> Answers grounded in your document</span>
          <span>Enter to send · Shift + Enter for newline</span>
        </div>
      </div>
    </section>
  )
}
