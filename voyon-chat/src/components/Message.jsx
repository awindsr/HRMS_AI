import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import Orb from './Orb'

/** One chat bubble. User messages are plain text; assistant messages render markdown. */
export default function Message({ message }) {
  const { role, content, pending, error } = message

  if (role === 'user') {
    return (
      <div className="msg msg--user">
        <div className="bubble bubble--user">{content}</div>
      </div>
    )
  }

  return (
    <div className="msg msg--assistant">
      <Orb className="orb--avatar" />
      <div className={`bubble bubble--assistant ${error ? 'bubble--error' : ''}`}>
        {pending && !content ? (
          <TypingDots />
        ) : (
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        )}
      </div>
    </div>
  )
}

function TypingDots() {
  return (
    <span className="typing">
      <span />
      <span />
      <span />
    </span>
  )
}
