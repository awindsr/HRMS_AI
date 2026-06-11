import { SUGGESTIONS } from '../config'

export default function SuggestionChips({ onPick, disabled }) {
  return (
    <div className="chips">
      {SUGGESTIONS.map((s) => (
        <button key={s} className="chip" onClick={() => onPick(s)} disabled={disabled}>
          {s}
        </button>
      ))}
    </div>
  )
}
