import { useCallback, useEffect, useRef, useState } from 'react'
import { openChatStream } from '../api/chatStream'

/**
 * Owns the chat conversation: message list, thread continuity, streaming state, and the
 * send / new-chat actions. Components consume this hook and stay purely presentational.
 */
export function useChat() {
  const [messages, setMessages] = useState([])
  const [threadId, setThreadId] = useState(null)
  const [streaming, setStreaming] = useState(false)
  const [toolActive, setToolActive] = useState(false)

  const streamRef = useRef(null)
  const threadIdRef = useRef(null) // latest threadId for use inside the active stream

  useEffect(() => {
    threadIdRef.current = threadId
  }, [threadId])

  // Cancel any in-flight stream on unmount.
  useEffect(() => () => streamRef.current?.close(), [])

  const patchLastAssistant = useCallback((patch) => {
    setMessages((prev) => {
      const next = [...prev]
      const last = next[next.length - 1]
      if (last?.role === 'assistant') {
        next[next.length - 1] = typeof patch === 'function' ? patch(last) : { ...last, ...patch }
      }
      return next
    })
  }, [])

  const sendMessage = useCallback(
    (text) => {
      const trimmed = text.trim()
      if (!trimmed || streaming) return

      setMessages((prev) => [
        ...prev,
        { role: 'user', content: trimmed },
        { role: 'assistant', content: '', pending: true },
      ])
      setStreaming(true)
      setToolActive(false)

      streamRef.current = openChatStream({
        message: trimmed,
        threadId: threadIdRef.current,
        onThread: setThreadId,
        onDelta: (chunk) =>
          patchLastAssistant((last) => ({ ...last, content: last.content + chunk, pending: false })),
        onTool: () => setToolActive(true),
        onDone: () => {
          setStreaming(false)
          setToolActive(false)
          streamRef.current = null
        },
        onError: (msg) => {
          patchLastAssistant({ content: msg, pending: false, error: true })
          setStreaming(false)
          setToolActive(false)
          streamRef.current = null
        },
      })
    },
    [streaming, patchLastAssistant],
  )

  const newChat = useCallback(() => {
    streamRef.current?.close()
    streamRef.current = null
    setMessages([])
    setThreadId(null)
    setStreaming(false)
    setToolActive(false)
  }, [])

  return {
    messages,
    streaming,
    toolActive,
    hasChat: messages.length > 0,
    sendMessage,
    newChat,
  }
}
