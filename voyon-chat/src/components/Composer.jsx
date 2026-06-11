import { useState } from 'react'
import { PlusIcon, ArrowUpIcon } from '../icons/Icons'

/**
 * The input pill: '+' (new chat) on the left, text field, send arrow on the right.
 * Owns only its own draft text; submitting hands the value up via onSend.
 */
export default function Composer({ onSend, onNewChat, disabled }) {
  const [value, setValue] = useState('')

  const submit = (e) => {
    e.preventDefault()
    const text = value.trim()
    if (!text || disabled) return
    onSend(text)
    setValue('')
  }

  return (
    <form className="composer" onSubmit={submit}>
      <button type="button" className="composer__plus" onClick={onNewChat} aria-label="New chat">
        <PlusIcon />
      </button>
      <input
        className="composer__input"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder="Ask about attendance..."
        autoFocus
      />
      <button type="submit" className="composer__send" disabled={disabled || !value.trim()} aria-label="Send">
        <ArrowUpIcon />
      </button>
    </form>
  )
}
