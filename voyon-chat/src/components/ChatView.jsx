import MessageList from './MessageList'
import Composer from './Composer'

/** Active conversation: transcript above, composer pinned to the bottom. */
export default function ChatView({ messages, toolActive, onSend, onNewChat, disabled }) {
  return (
    <>
      <MessageList messages={messages} toolActive={toolActive} />
      <footer className="composer-bar">
        <Composer onSend={onSend} onNewChat={onNewChat} disabled={disabled} />
      </footer>
    </>
  )
}
