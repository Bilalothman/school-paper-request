import { useEffect, useState } from 'react'
import ServiceCard from '../components/ServiceCard'
import Icon from '../components/Icon'
import api, { errorMessage } from '../services/api'
import useAuth from '../hooks/useAuth'

export default function Services() {
  const { user } = useAuth(); const [services, setServices] = useState([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  useEffect(() => { api.get('/services').then(({ data }) => setServices(data)).catch((err) => setError(errorMessage(err, 'Could not load services.'))).finally(() => setLoading(false)) }, [])
  return <><section className="portal-hero"><div><p className="hero-kicker">Student portal</p><h1>Good to see you, {user.fullName.split(' ')[0]}.</h1><p>Choose an official document below and send your request in just a few steps.</p></div><div className="hero-symbol"><Icon name="document" size={42} /></div></section>
    <div className="section-heading"><div><p className="eyebrow">Document catalog</p><h2>Available services</h2></div><span className="result-count">{services.length} services</span></div>
    {loading && <div className="loading-panel"><span className="spinner" /> Loading services...</div>}{error && <div className="message error">{error}</div>}
    <div className="service-grid">{services.map((service, index) => <ServiceCard key={service.id} service={service} index={index} />)}</div>
  </>
}
