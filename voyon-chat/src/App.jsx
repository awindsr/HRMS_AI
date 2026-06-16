import { useChat } from './hooks/useChat'
import { useAuth } from './auth/AuthContext'
import HeroView from './components/HeroView'
import ChatView from './components/ChatView'
import LoginView from './components/LoginView'
import AppBar from './components/AppBar'

export default function App() {
  const { status } = useAuth()
  const { messages, streaming, toolActive, hasChat, sendMessage, newChat } = useChat()

  // While the initial session check is in flight, show just the brand background (no flash of login).
  if (status === 'loading') {
    return (
      <div className="app app--hero">
        <div className="bg" aria-hidden="true" />
      </div>
    )
  }

  if (status !== 'authed') {
    return (
      <div className="app app--hero">
        <div className="bg" aria-hidden="true" />
        <LoginView />
      </div>
    )
  }

  return (
    <div className={`app ${hasChat ? 'app--chat' : 'app--hero'}`}>
      <div className="bg" aria-hidden="true" />
      <AppBar />
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
