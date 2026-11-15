import { Link } from 'react-router-dom'
import { ShieldCheck, ArrowRight, X } from 'lucide-react'

export default function SignInModal({ isOpen, onClose }) {
  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="modal-card" onClick={e => e.stopPropagation()}>
        <button
          type="button"
          className="modal-close-btn"
          onClick={onClose}
          aria-label="Close dialog"
        >
          <X size={20} />
        </button>

        <div className="modal-icon-badge">
          <ShieldCheck size={26} />
        </div>

        <h3 style={{ fontFamily: "var(--font-heading, 'Manrope', sans-serif)", fontSize: 22, fontWeight: 800, color: '#352A28', margin: '0 0 10px' }}>
          Open Access Workspace
        </h3>

        <p style={{ fontSize: 14, color: '#6A5B55', lineHeight: 1.6, margin: '0 0 24px' }}>
          DocuMind is currently running in local direct access mode. No username or password is required
          to start analyzing PDFs and chatting with documents.
        </p>

        <div style={{ background: '#FFF5F0', border: '1px solid #F5DFD5', borderRadius: 10, padding: 14, marginBottom: 24, fontSize: 12.5, color: '#55443E', lineHeight: 1.5 }}>
          <strong>Enterprise Note:</strong> Single Sign-On (SSO), OAuth 2.0, and role-based access control (RBAC)
          are scheduled for the cloud enterprise release.
        </div>

        <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
          <button
            type="button"
            className="btn-hero-secondary"
            style={{ padding: '10px 18px', fontSize: 14 }}
            onClick={onClose}
          >
            Cancel
          </button>
          <Link
            to="/app"
            className="btn-hero-primary"
            style={{ padding: '10px 22px', fontSize: 14 }}
            onClick={onClose}
          >
            <span>Enter Workspace</span>
            <ArrowRight size={15} />
          </Link>
        </div>
      </div>
    </div>
  )
}
