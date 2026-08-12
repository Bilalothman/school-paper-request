import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import RequestStatus from '../components/RequestStatus'
import Icon from '../components/Icon'
import api, { errorMessage } from '../services/api'

export default function MyRequests() {
  const location = useLocation(); const [requests, setRequests] = useState([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  useEffect(() => { api.get('/requests/mine').then(({ data }) => setRequests(data)).catch((err) => setError(errorMessage(err, 'Could not load requests.'))).finally(() => setLoading(false)) }, [])
  const count = (status) => requests.filter((request) => request.status === status).length
  return <><div className="dashboard-heading"><div><p className="eyebrow">Request center</p><h1>My requests</h1><p>Follow every document request from submission to decision.</p></div><Link className="button" to="/services">New request <Icon name="arrow" size={18} /></Link></div>
    <div className="stats-grid"><div className="stat-card"><span className="stat-icon blue"><Icon name="inbox" /></span><div><strong>{requests.length}</strong><span>Total requests</span></div></div><div className="stat-card"><span className="stat-icon amber"><Icon name="clock" /></span><div><strong>{count('Submitted')}</strong><span>Under review</span></div></div><div className="stat-card"><span className="stat-icon green"><Icon name="check" /></span><div><strong>{count('Approved')}</strong><span>Approved</span></div></div></div>
    {location.state?.success && <div className="message success">{location.state.success}</div>}{loading && <div className="loading-panel"><span className="spinner" /> Loading your requests...</div>}{error && <div className="message error">{error}</div>}
    {!loading && !error && requests.length === 0 && <div className="empty-state"><span><Icon name="inbox" size={30} /></span><h2>No requests yet</h2><p>Choose a service to submit your first paper request.</p><Link className="button" to="/services">Browse services</Link></div>}
    <div className="request-list">{requests.map((request) => <article className="request-row" key={request.id}><div className="request-document"><span className="service-icon"><Icon name="document" /></span><div><span className="request-id">Request #{String(request.id).padStart(4, '0')}</span><h2>{request.service}</h2><p>{request.note || 'No additional note provided'}</p></div></div><div className="request-details"><div><span>Phone</span><strong>{request.phoneNumber}</strong></div><div><span>Grade</span><strong>{request.grade}</strong></div><div><span>Address</span><strong title={request.address}>{request.address}</strong></div><div><span>Submitted</span><strong>{new Date(request.createdAt).toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' })}</strong></div><div><span>Admin comment</span><strong>{request.adminComment || 'Not available yet'}</strong></div></div><RequestStatus status={request.status} /></article>)}</div>
  </>
}
