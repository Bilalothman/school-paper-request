import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api, { errorMessage } from '../services/api'
import useAuth from '../hooks/useAuth'

let scriptPromise
function loadScript() {
  if (window.google?.accounts?.id) return Promise.resolve()
  if (!scriptPromise) scriptPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://accounts.google.com/gsi/client'; script.async = true; script.defer = true
    script.onload = resolve; script.onerror = reject; document.head.appendChild(script)
  })
  return scriptPromise
}

export default function GoogleSignIn({ onError, allowRegistration = false }) {
  const ref = useRef(null); const { login } = useAuth(); const navigate = useNavigate(); const [available, setAvailable] = useState(true)
  const [pendingEmail, setPendingEmail] = useState(''); const [code, setCode] = useState(''); const [verifying, setVerifying] = useState(false)
  useEffect(() => {
    let active = true
    api.get('/auth/google-config').then(async ({ data }) => {
      if (!data.clientId) { if (active) setAvailable(false); return }
      await loadScript(); if (!active || !ref.current) return
      window.google.accounts.id.initialize({ client_id: data.clientId, callback: async ({ credential }) => {
        try { const { data: session } = await api.post('/auth/google', { credential, allowRegistration }); if (session.requiresVerification) setPendingEmail(session.email); else { login(session); navigate('/services') } }
        catch (error) { onError(errorMessage(error, 'Google Sign-In failed.')) }
      } })
      window.google.accounts.id.renderButton(ref.current, { theme: 'outline', size: 'large', width: 390, text: 'continue_with' })
    }).catch(() => { if (active) setAvailable(false) })
    return () => { active = false }
  }, [allowRegistration, login, navigate, onError])
  if (!available) return null
  const verify = async (event) => {
    event.preventDefault(); setVerifying(true)
    try { const { data } = await api.post('/auth/verify-email', { email: pendingEmail, code }); login(data); navigate('/services') }
    catch (error) { onError(errorMessage(error, 'The verification code is not valid.')) }
    finally { setVerifying(false) }
  }
  if (pendingEmail) return <div className="google-verification"><p>A six-digit verification code was sent to <strong>{pendingEmail}</strong>.</p><form onSubmit={verify}><input className="code-input" required inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength="6" placeholder="000000" value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))} /><button className="wide-button" disabled={verifying || code.length !== 6}>{verifying ? 'Verifying...' : 'Verify Google registration'}</button></form></div>
  return <><div className="auth-divider"><span>or continue with</span></div><div className="google-button" ref={ref} /></>
}
