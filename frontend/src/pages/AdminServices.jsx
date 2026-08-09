import { useCallback, useEffect, useState } from 'react'
import Icon from '../components/Icon'
import api, { errorMessage } from '../services/api'

export default function AdminServices() {
  const [services, setServices] = useState([])
  const [form, setForm] = useState({ name: '', description: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [removing, setRemoving] = useState(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    try { const { data } = await api.get('/admin/services'); setServices(data) }
    catch (err) { setError(errorMessage(err, 'Services could not be loaded.')) }
    finally { setLoading(false) }
  }, [])
  useEffect(() => { load() }, [load])

  const create = async (event) => {
    event.preventDefault(); setSaving(true); setError(''); setSuccess('')
    try {
      await api.post('/admin/services', form)
      setForm({ name: '', description: '' }); setSuccess('Service added successfully.'); await load()
    } catch (err) { setError(errorMessage(err, 'Service could not be added.')) }
    finally { setSaving(false) }
  }

  const remove = async (service) => {
    if (!window.confirm(`Remove "${service.name}"?`)) return
    setRemoving(service.id); setError(''); setSuccess('')
    try { await api.delete(`/admin/services/${service.id}`); setSuccess('Service removed successfully.'); await load() }
    catch (err) { setError(errorMessage(err, 'Service could not be removed.')) }
    finally { setRemoving(null) }
  }

  return <><div className="dashboard-heading"><div><p className="eyebrow">Service catalog</p><h1>Manage services</h1><p>Add the official papers students can request or remove unused services.</p></div><span className="result-count">{services.length} services</span></div>
    {error && <div className="message error">{error}</div>}{success && <div className="message success">{success}</div>}
    <div className="service-management-layout"><section className="management-form"><p className="eyebrow">New service</p><h2>Add a school paper</h2><p>The service will immediately appear in the student catalog.</p><form onSubmit={create}><label>Service name<input required minLength="2" maxLength="100" placeholder="For example: Graduation certificate" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label><label>Description<textarea required minLength="2" maxLength="500" rows="5" placeholder="Explain what this document is for..." value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label><button className="wide-button" disabled={saving}>{saving ? 'Adding service...' : 'Add service'}</button></form></section>
      <section><div className="section-heading"><div><p className="eyebrow">Available papers</p><h2>Current services</h2></div></div>{loading ? <div className="loading-panel"><span className="spinner" /> Loading services...</div> : services.length === 0 ? <div className="empty-state"><span><Icon name="document" /></span><h2>No services yet</h2><p>Add the first service using the form.</p></div> : <div className="management-list">{services.map((service) => <article key={service.id} className="management-row"><span className="service-icon"><Icon name="document" /></span><div><h3>{service.name}</h3><p>{service.description}</p></div><button className="danger remove-service" disabled={removing === service.id} onClick={() => remove(service)}><Icon name="close" size={17} />{removing === service.id ? 'Removing...' : 'Remove'}</button></article>)}</div>}</section>
    </div></>
}
