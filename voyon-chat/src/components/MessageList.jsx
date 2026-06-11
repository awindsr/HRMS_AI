import { useEffect, useRef } from 'react'
import Message from './Message'

/** Scrollable transcript. Auto-scrolls to the newest content as it streams in. */
export default function MessageList({ messages, toolActive }) {
  const scrollRef = useRef(null)

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, toolActive])

  return (
    <main className="chat" ref={scrollRef}>
      <div className="chat__inner">
        {messages.map((m, i) => (
          <Message key={i} message={m} />
        ))}
        {toolActive && (
          <div className="status">
            <span className="status__dot" /> Checking attendance…
          </div>
        )}
      </div>
    </main>
  )
}
