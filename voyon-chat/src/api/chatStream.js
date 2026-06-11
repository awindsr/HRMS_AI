import { API_BASE } from '../config'

/**
 * Opens an SSE chat stream against the backend relay and routes the named events
 * (thread | delta | tool | done | error) to callbacks. This is the only module that knows
 * about EventSource / the wire format — UI and hooks stay transport-agnostic.
 *
 * @returns {{ close: () => void }} handle to cancel the stream.
 */
export function openChatStream({ message, threadId, onThread, onDelta, onTool, onDone, onError }) {
  const params = new URLSearchParams({ message })
  if (threadId) params.set('threadId', threadId)

  const source = new EventSource(`${API_BASE}/api/v1/chat/stream?${params.toString()}`)
  let closed = false

  const close = () => {
    if (closed) return
    closed = true
    source.close()
  }

  source.addEventListener('thread', (e) => onThread?.(e.data))

  source.addEventListener('delta', (e) => {
    const text = safeParse(e.data)?.text
    if (text) onDelta?.(text)
  })

  source.addEventListener('tool', (e) => onTool?.(safeParse(e.data)?.name))

  source.addEventListener('done', () => {
    onDone?.()
    close()
  })

  source.addEventListener('error', (e) => {
    // A named SSE 'error' event carries data; a transport drop fires the same event without it.
    if (e.data) {
      onError?.(safeParse(e.data)?.message || 'Something went wrong. Please try again.')
      close()
    } else if (!closed) {
      onError?.('Connection lost. Please try again.')
      close()
    }
  })

  return { close }
}

function safeParse(raw) {
  try {
    return JSON.parse(raw)
  } catch {
    return null
  }
}
