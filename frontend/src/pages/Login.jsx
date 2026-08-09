import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import api, { errorMessage } from '../services/api'
import useAuth from '../hooks/useAuth'
import Icon from '../components/Icon'
import GoogleSignIn from '../components/GoogleSignIn'

export default function Login() {
  const { login } = useAuth(); const navigate = useNavigate()
  const [form, setForm] = useState({ email: '', password: '' }); const [error, setError] = useState(''); const [loading, setLoading] = useState(false)
  const submit = async (event) => {
    event.preventDefault(); setLoading(true); setError('')
    try { const { data } = await api.post('/auth/login', form); login(data); navigate(data.user.role === 'Admin' ? '/admin/requests' : '/services') }
    catch (err) { setError(errorMessage(err, 'Login failed.')) } finally { setLoading(false) }
  }
  return <section className="auth-shell">
    <aside className="auth-story"><div className="story-brand"><span className="story-logo">SR</span><span>School Requests</span></div><div className="story-copy"><span className="story-kicker">Student services, simplified</span><h1>Official school papers, without the paperwork.</h1><p>Request documents, follow their progress, and receive decisions through one secure portal.</p><div className="trust-row"><span><Icon name="shield" /> Secure access</span><span><Icon name="clock" /> Live status</span></div></div><p className="story-foot">School Administration Portal</p></aside>
    <div className="auth-panel"><div className="auth-panel-inner"><span className="mobile-logo">SR</span><p className="eyebrow">Secure portal</p><h2>Welcome back</h2><p>Sign in or create a student account with Google.</p>{error && <div className="message error">{error}</div>}<GoogleSignIn onError={setError} /><form onSubmit={submit}><label>Email address<input type="email" autoComplete="email" required placeholder="you@school.com" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label><label>Password<input type="password" autoComplete="current-password" required placeholder="Enter your password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label><button className="wide-button" disabled={loading}>{loading ? 'Signing you in...' : 'Sign in to your account'}<Icon name="arrow" size={18} /></button></form><p className="auth-switch">New to the portal? <Link to="/register" state={{ fromLogin: true }}>Create with email instead</Link></p><p className="security-note"><Icon name="shield" size={16} /> Your credentials are encrypted and protected.</p></div></div>
  </section>
}
