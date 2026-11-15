import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { FileText, ArrowRight, Menu, X, Sparkles, UserCheck } from 'lucide-react'
import GithubIcon from './GithubIcon.jsx'

export default function LandingNavbar({ onOpenSignIn }) {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 20)
    }
    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const closeMobile = () => setMobileOpen(false)

  return (
    <header className={`landing-navbar-wrapper ${scrolled ? 'is-scrolled' : ''}`}>
      <div className="landing-container">
        <nav className="landing-navbar" aria-label="Main Navigation">
          {/* Brand */}
          <Link to="/" className="landing-brand" aria-label="DocuMind AI Home">
            <img
              src="/documind-logo-horizontal.png"
              alt="DocuMind AI Logo"
              className="landing-brand-logo-img"
            />
          </Link>

          {/* Desktop Links */}
          <ul className="landing-nav-links">
            <li><a href="#features" className="landing-nav-link">Features</a></li>
            <li><a href="#how-it-works" className="landing-nav-link">How It Works</a></li>
            <li><a href="#why-documind" className="landing-nav-link">Why DocuMind</a></li>
            <li><a href="#faq" className="landing-nav-link">FAQ</a></li>
          </ul>

          {/* Nav Actions */}
          <div className="landing-nav-actions">
            <a
              href="https://github.com/Akhilcharankumarpujari/DocuMind"
              target="_blank"
              rel="noopener noreferrer"
              className="nav-github-link"
              title="View Source on GitHub"
            >
              <GithubIcon size={15} />
              <span>GitHub</span>
            </a>

            <Link
              to="/login"
              className="btn-nav-signin"
              title="Sign In to DocuMind"
            >
              Sign In
            </Link>

            <Link to="/register" className="btn-nav-cta">
              <span>Get Started</span>
              <ArrowRight size={15} />
            </Link>

            {/* Mobile Toggle */}
            <button
              type="button"
              className="mobile-nav-toggle"
              onClick={() => setMobileOpen(prev => !prev)}
              aria-label={mobileOpen ? 'Close Menu' : 'Open Menu'}
              aria-expanded={mobileOpen}
            >
              {mobileOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </nav>
      </div>

      {/* Mobile Drawer */}
      {mobileOpen && (
        <div className="mobile-menu-drawer">
          <a href="#features" onClick={closeMobile}>Features</a>
          <a href="#how-it-works" onClick={closeMobile}>How It Works</a>
          <a href="#why-documind" onClick={closeMobile}>Why DocuMind</a>
          <a href="#faq" onClick={closeMobile}>FAQ</a>
          <a
            href="https://github.com/Akhilcharankumarpujari/DocuMind"
            target="_blank"
            rel="noopener noreferrer"
            onClick={closeMobile}
          >
            GitHub Repository
          </a>
          <div className="mobile-menu-actions">
            <Link
              to="/login"
              className="btn-nav-signin"
              style={{ display: 'block', width: '100%', textAlign: 'center' }}
              onClick={closeMobile}
            >
              Sign In
            </Link>
            <Link
              to="/register"
              className="btn-nav-cta"
              style={{ justifyContent: 'center' }}
              onClick={closeMobile}
            >
              <span>Get Started</span>
              <ArrowRight size={15} />
            </Link>
          </div>
        </div>
      )}
    </header>
  )
}
