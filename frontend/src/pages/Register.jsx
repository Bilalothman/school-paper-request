import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import api, { errorMessage } from '../services/api'
import useAuth from '../hooks/useAuth'
import Icon from '../components/Icon'
import GoogleSignIn from '../components/GoogleSignIn'

export default function Register() {
  const { user, login } = useAuth(); const navigate = useNavigate(); const location = useLocation()
  const [form, setForm] = useState({ fullName: '', email: '', password: '', confirmPassword: '' })
  const [step, setStep] = useState('details'); const [code, setCode] = useState('')
  const [error, setError] = useState(''); const [loading, setLoading] = useState(false)
  if (user) return <Navigate to="/" replace />
  if (!location.state?.fromLogin) return <Navigate to="/login" replace />

  const update = (field) => (event) => setForm({ ...form, [field]: event.target.value })
  const submit = async (event) => {
    event.preventDefault(); setError('')
    if (form.password !== form.confirmPassword) { setError('Passwords do not match.'); return }
    setLoading(true)
    try { await api.post('/auth/register', form); setStep('verification') }
    catch (err) { setError(errorMessage(err, 'Verification code could not be sent.')) }
    finally { setLoading(false) }
  }
  const verify = async (event) => {
    event.preventDefault(); setError(''); setLoading(true)
    try { const { data } = await api.post('/auth/verify-email', { email: form.email, code }); login(data); navigate('/services') }
    catch (err) { setError(errorMessage(err, 'The verification code is not valid.')) }
    finally { setLoading(false) }
  }

  return <section className="auth-shell register-shell">
    <aside className="auth-story register-story"><div className="story-brand"><span className="story-logo">SR</span><span>School Requests</span></div><div className="story-copy"><span className="story-kicker">Join your student portal</span><h1>Your documents, one secure place.</h1><p>Create your account to request official school papers and follow every decision from your personal dashboard.</p><div className="benefit-list"><span><Icon name="check" /> Fast online requests</span><span><Icon name="check" /> Clear status tracking</span><span><Icon name="check" /> Secure student access</span></div></div><p className="story-foot">For registered students only</p></aside>
    <div className="auth-panel"><div className="auth-panel-inner register-panel"><span className="mobile-logo">SR</span>
      {step === 'details' ? <><p className="eyebrow">Student registration</p><h2>Create your account</h2><p>Register instantly with Google or verify your email manually.</p>{error && <div className="message error">{error}</div>}<GoogleSignIn onError={setError} allowRegistration />
        <form onSubmit={submit}><label>Full name<input type="text" required minLength="2" maxLength="100" autoComplete="name" placeholder="Your full name" value={form.fullName} onChange={update('fullName')} /></label><label>Email address<input type="email" required maxLength="200" autoComplete="email" placeholder="you@gmail.com" value={form.email} onChange={update('email')} /></label><div className="form-row"><label>Password<input type="password" required minLength="8" maxLength="100" autoComplete="new-password" placeholder="Minimum 8 characters" value={form.password} onChange={update('password')} /></label><label>Confirm password<input type="password" required autoComplete="new-password" placeholder="Repeat password" value={form.confirmPassword} onChange={update('confirmPassword')} /></label></div><p className="password-hint">Use at least 8 characters with uppercase, lowercase, and a number.</p><button className="wide-button" disabled={loading}>{loading ? 'Sending code...' : 'Continue with email verification'}<Icon name="arrow" size={18} /></button></form><p className="auth-switch">Already registered? <Link to="/login">Sign in instead</Link></p></>
        : <div className="verification-panel"><div className="verification-icon">✉</div><p className="eyebrow">Verify your email</p><h2>Enter your security code</h2><p>We sent a six-digit code to <strong>{form.email}</strong>. It expires in 10 minutes.</p>{error && <div className="message error">{error}</div>}<form onSubmit={verify}><label>Verification code<input className="code-input" type="text" required inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength="6" placeholder="000000" value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ''))} /></label><button className="wide-button" disabled={loading || code.length !== 6}>{loading ? 'Verifying...' : 'Verify and create account'}<Icon name="arrow" size={18} /></button></form><button type="button" className="text-button" onClick={() => { setStep('details'); setCode(''); setError('') }}>Change account details</button></div>}
    </div></div>
  </section>
}
