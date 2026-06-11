import { useChat } from './hooks/useChat'
import HeroView from './components/HeroView'
import ChatView from './components/ChatView'

export default function App() {
  const { messages, streaming, toolActive, hasChat, sendMessage, newChat } = useChat()

  return (
    <div className={`app ${hasChat ? 'app--chat' : 'app--hero'}`}>
      <div className="bg" aria-hidden="true" />
      {hasChat ? (
        <ChatView
          messages={messages}
          toolActive={toolActive}
          onSend={sendMessage}
          onNewChat={newChat}
          disabled={streaming}
        />
      ) : (
        <HeroView onSend={sendMessage} onNewChat={newChat} disabled={streaming} />
      )}
    </div>
  )
}
