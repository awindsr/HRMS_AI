// Time-of-day greeting prefix (no login → no per-user data).
export function greeting(date = new Date()) {
  const h = date.getHours()
  if (h < 12) return 'Morning'
  if (h < 18) return 'Afternoon'
  return 'Evening'
}
