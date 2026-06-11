import GlowOrb from './GlowOrb'
import Composer from './Composer'
import SuggestionChips from './SuggestionChips'
import { greeting } from '../utils/greeting'
import { useProfile } from '../hooks/useProfile'

/** Empty state: orb, greeting, composer, and suggestion chips. */
export default function HeroView({ onSend, onNewChat, disabled }) {
  const { firstName } = useProfile()
  const title = firstName ? `${greeting()}, ${firstName}.` : `${greeting()}.`

  return (
    <main className="hero">
      <div className="hero__orb">
        <GlowOrb hue={0} hoverIntensity={0.4} backgroundColor="#fbfcff" />
      </div>
      <h1 className="hero__title">{title}</h1>
      <p className="hero__subtitle">Check your team's attendance, or log a live check-in/out.</p>
      <Composer onSend={onSend} onNewChat={onNewChat} disabled={disabled} />
      <SuggestionChips onPick={onSend} disabled={disabled} />
    </main>
  )
}
