import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom'
import useAuth from '../hooks/useAuth'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  if (!user || location.pathname === '/login' || location.pathname === '/register') return null
  const signOut = () => { logout(); navigate('/login') }
  const home = user.role === 'Admin' ? '/admin/requests' : '/services'

  return <header className="navbar">
    <div className="nav-content">
      <Link className="brand" to={home}><span className="brand-mark">SR</span><span>School Requests</span></Link>
      <nav>
        {user.role === 'Student' && <><NavLink to="/services">Services</NavLink><NavLink to="/my-requests">My Requests</NavLink></>}
        {user.role === 'Admin' && <><NavLink to="/admin/requests">Admin Requests</NavLink><NavLink to="/admin/services">Manage Services</NavLink></>}
        <NavLink to="/profile">Profile</NavLink>
        <button className="link-button" onClick={signOut}>Logout</button>
        <div className="nav-user"><span className="avatar">{user.fullName.charAt(0)}</span><span className="user-copy"><strong>{user.fullName}</strong><span>{user.role}</span></span></div>
      </nav>
    </div>
  </header>
}
