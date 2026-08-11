import { useEffect, useState } from 'react'
import api, { errorMessage } from '../services/api'
import useAuth from '../hooks/useAuth'

export default function Profile() {
  const { user } = useAuth()
  const [profile, setProfile] = useState(user)
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    api.get('/profile')
      .then(({ data }) => setProfile(data))
      .catch((err) => setError(errorMessage(err, 'Your profile could not be loaded.')))
      .finally(() => setLoading(false))
  }, [])

  const update = (field) => (event) => setForm({ ...form, [field]: event.target.value })
  const submit = async (event) => {
    event.preventDefault()
    setError(''); setMessage(''); setSaving(true)
    try {
      const { data } = await api.put('/profile/password', form)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setMessage(data.message)
    } catch (err) {
      setError(errorMessage(err, 'Your profile could not be updated.'))
    } finally {
      setSaving(false)
    }
  }

  return <section className="profile-layout">
    <aside className="profile-summary">
      <div className="profile-avatar">{user.fullName.charAt(0).toUpperCase()}</div>
      <p className="eyebrow">Account profile</p>
      <h1>{user.fullName}</h1>
      <span className="profile-role">{user.role}</span>
      <p>Keep your personal information accurate and up to date.</p>
    </aside>
    <div className="profile-form-panel">
      <p className="eyebrow">Personal information</p>
      <h2>Edit your profile</h2>
      <p>Your personal details are fixed. You can securely change your account password here.</p>
      {error && <div className="message error">{error}</div>}
      {message && <div className="message success">{message}</div>}
      {loading ? <div className="loading-panel"><span className="spinner" />Loading profile...</div> :
        <form onSubmit={submit}>
          <label>Full name<input type="text" value={profile.fullName} disabled /></label>
          <label>Email address<input type="email" value={profile.email} disabled /></label>
          <label>Account role<input type="text" value={profile.role} disabled /></label>
          <label>Current password<input type="password" required autoComplete="current-password" value={form.currentPassword} onChange={update('currentPassword')} /></label>
          <label>New password<input type="password" required minLength="8" maxLength="100" autoComplete="new-password" value={form.newPassword} onChange={update('newPassword')} /></label>
          <label>Confirm new password<input type="password" required minLength="8" maxLength="100" autoComplete="new-password" value={form.confirmPassword} onChange={update('confirmPassword')} /></label>
          <p className="password-hint">Use at least 8 characters with uppercase, lowercase, and a number.</p>
          <button disabled={saving}>{saving ? 'Changing password...' : 'Change password'}</button>
        </form>}
    </div>
  </section>
}
