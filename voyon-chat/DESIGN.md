# Voyon Folks — Chat App Design System

Brand-aligned tokens for the attendance assistant UI, derived from the Voyon Folks site.
All values live in [`src/styles/tokens.css`](src/styles/tokens.css) as CSS custom properties;
components reference `var(--token)` only — never hard-coded brand values.

## Typography
- **Instrument Sans** (Google Fonts, weights 400–700). Token: `--font-sans`.
- Scale: `--text-xs` … `--text-3xl`. Headings use `--weight-bold`, body `--weight-regular/medium`.

## Color
| Role | Token | Value |
|---|---|---|
| Primary | `--brand-600` | `#1f5bff` |
| Primary hover | `--brand-700` | `#1747d6` |
| Light brand wash | `--brand-50 / -100` | `#f2f6ff / #e6efff` |
| Cyan accent | `--accent-cyan` | `#21c7f0` |
| Rating gold | `--gold` | `#f6b40a` |
| Success / present | `--success` | `#16a34a` |
| Error | `--danger` | `#dc2626` |
| Heading ink | `--ink` | `#0b1220` |
| Body | `--muted` | `#586273` |
| Surface | `--surface` | `#ffffff` |
| Background | `--bg` + `--grad-bg` | cool near-white + brand wash |
| Border | `--border` | `#e7ecf5` |

## Shape & depth
- Radius: `--radius-sm/md/lg` and `--radius-pill` (controls are fully pill-shaped, per the brand).
- Shadows: `--shadow-sm/md/lg` (soft, cool-tinted) and `--shadow-brand` (blue glow for primary actions).
- Motion: `--dur`, `--ease`.

## Component mapping
- **Orb** → brand blue→cyan radial gradient (`--grad-orb`).
- **User bubble** → solid `--brand-600`, white text, brand glow.
- **Assistant bubble** → white card, `--border`, `--shadow-md`.
- **Send button** → `--brand-600` when active (was neutral when empty).
- **Chips / composer** → white pills with brand-tinted hover and focus ring (`--brand-100`).
