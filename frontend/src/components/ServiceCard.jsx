import { Link } from 'react-router-dom'
import Icon from './Icon'

export default function ServiceCard({ service, index }) {
  const codes = ['ENR', 'TRN', 'ATT']
  return <article className="service-card">
    <div className="service-card-top"><span className="service-icon"><Icon name="document" size={24} /></span><span className="service-code">{codes[index] || `DOC ${index + 1}`}</span></div>
    <div><p className="eyebrow">Official document</p><h2>{service.name}</h2><p>{service.description}</p></div>
    <Link className="service-link" to={`/submit-request/${service.id}`} state={{ service }}><span>Start request</span><Icon name="arrow" size={18} /></Link>
  </article>
}
