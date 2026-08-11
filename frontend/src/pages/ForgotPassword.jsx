import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import api, { errorMessage } from '../services/api'
import Icon from '../components/Icon'

export default function ForgotPassword() {
  const navigate = useNavigate()
  const [step, setStep] = useState('email')
  const [form, setForm] = useState({ email: '', code: '', newPassword: '', confirmPassword: '' })
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const update = (field) => (event) => setForm({ ...form, [field]: event.target.value })

  const requestCode = async (event) => {
    event.preventDefault(); setError(''); setMessage(''); setLoading(true)
    try {
      const { data } = await api.post('/auth/forgot-password', { email: form.email })
      setMessage(data.message); setStep('reset')
    } catch (err) { setError(errorMessage(err, 'The verification code could not be sent.')) }
    finally { setLoading(false) }
  }

  const resetPassword = async (event) => {
    event.preventDefault(); setError(''); setMessage('')
    if (form.newPassword !== form.confirmPassword) { setError('Passwords do not match.'); return }
    setLoading(true)
    try {
      const { data } = await api.post('/auth/reset-password', form)
      navigate('/login', { replace: true, state: { message: data.message } })
    } catch (err) { setError(errorMessage(err, 'Your password could not be reset.')) }
    finally { setLoading(false) }
  }

  return <section className="auth-shell">
    <aside className="auth-story"><div className="story-brand"><span className="story-logo">SR</span><span>School Requests</span></div><div className="story-copy"><span className="story-kicker">Account recovery</span><h1>Return to your requests securely.</h1><p>We verify access to your school email before allowing you to choose a new password.</p><div className="trust-row"><span><Icon name="shield" /> Expiring code</span><span><Icon name="clock" /> 10-minute limit</span></div></div><p className="story-foot">School Administration Portal</p></aside>
    <div className="auth-panel"><div className="auth-panel-inner register-panel"><span className="mobile-logo">SR</span>
      {step === 'email' ? <><p className="eyebrow">Forgot password</p><h2>Recover your account</h2><p>Enter your account email and we will send you a six-digit verification code.</p>{error && <div className="message error">{error}</div>}<form onSubmit={requestCode}><label>Email address<input type="email" required maxLength="200" autoComplete="email" placeholder="you@school.com" value={form.email} onChange={update('email')} /></label><button className="wide-button" disabled={loading}>{loading ? 'Sending code...' : 'Send verification code'}<Icon name="arrow" size={18} /></button></form><p className="auth-switch"><Link to="/login">Back to sign in</Link></p></>
        : <div className="verification-panel"><div className="verification-icon">✉</div><p className="eyebrow">Reset password</p><h2>Enter the code</h2><p>Enter the code sent to <strong>{form.email}</strong>, then choose a new password.</p>{message && <div className="message success">{message}</div>}{error && <div className="message error">{error}</div>}<form onSubmit={resetPassword}><label>Verification code<input className="code-input" type="text" required inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength="6" placeholder="000000" value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value.replace(/\D/g, '') })} /></label><div className="form-row"><label>New password<input type="password" required minLength="8" maxLength="100" autoComplete="new-password" value={form.newPassword} onChange={update('newPassword')} /></label><label>Confirm password<input type="password" required minLength="8" maxLength="100" autoComplete="new-password" value={form.confirmPassword} onChange={update('confirmPassword')} /></label></div><p className="password-hint">Use at least 8 characters with uppercase, lowercase, and a number.</p><button className="wide-button" disabled={loading || form.code.length !== 6}>{loading ? 'Resetting password...' : 'Reset password'}<Icon name="arrow" size={18} /></button></form><button type="button" className="text-button" onClick={() => { setStep('email'); setError(''); setMessage(''); setForm({ ...form, code: '' }) }}>Use a different email</button></div>}
    </div></div>
  </section>
}
