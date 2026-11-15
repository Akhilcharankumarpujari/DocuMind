import { useState, useRef, useEffect } from 'react'
import {
  FileText,
  Upload,
  MoreHorizontal,
  Sparkles,
  Download,
  Info,
  Trash2,
  Copy,
  Check,
  X,
  Loader2
} from 'lucide-react'
import { fetchDocumentBlob } from '../../services/documentService'

export default function Header({ document, onUpload, onAction, onDelete }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [copied, setCopied] = useState(false)
  const menuRef = useRef(null)

  const status = document?.status || (document ? 'Ready' : 'No document')

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event) {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setMenuOpen(false)
      }
    }
    if (menuOpen) {
      window.document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      window.document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [menuOpen])

  // Close dropdown or modal on Escape key
  useEffect(() => {
    function handleKeyDown(e) {
      if (e.key === 'Escape') {
        setMenuOpen(false)
        setDetailsOpen(false)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  const handleDownload = async () => {
    if (!document?.id || downloading) return
    setDownloading(true)
    try {
      const blob = await fetchDocumentBlob(document.id)
      const url = URL.createObjectURL(blob)
      const a = window.document.createElement('a')
      a.href = url
      a.download = document.name || 'document.pdf'
      window.document.body.appendChild(a)
      a.click()
      a.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      alert(`Could not download file: ${err.message}`)
    } finally {
      setDownloading(false)
      setMenuOpen(false)
    }
  }

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // ignore clipboard error
    }
  }

  const handleDelete = () => {
    setMenuOpen(false)
    if (!document?.id || !onDelete) return
    if (window.confirm(`Are you sure you want to delete "${document.name}"? This action cannot be undone.`)) {
      onDelete(document.id)
    }
  }

  return (
    <header className="topbar">
      <div className="breadcrumb">
        <span>Workspace</span>
        <span className="crumb-slash">/</span>
        <FileText size={15} />
        <strong>{document?.name || 'New document'}</strong>
        <span className={`ready-pill status-${status.toLowerCase()}`}>
          {status}
        </span>
      </div>

      <div className="topbar-actions">
        <button
          className="subtle-button"
          onClick={() => onAction('summary')}
          title="Summarize document"
          disabled={!document}
        >
          <Sparkles size={15} /> Summary
        </button>
        <button
          className="subtle-button"
          onClick={() => onAction('explain')}
          title="Explain key findings"
          disabled={!document}
        >
          Explain
        </button>
        <button
          className="subtle-button"
          onClick={() => onAction('rewrite')}
          title="Rewrite main points"
          disabled={!document}
        >
          Rewrite
        </button>
        <button className="primary-button upload-top" onClick={onUpload} title="Upload a new PDF">
          <Upload size={15} /> Upload PDF
        </button>

        {/* 3-Dots Dropdown Trigger */}
        <div className="dropdown-wrapper" ref={menuRef}>
          <button
            className={`icon-button ${menuOpen ? 'active' : ''}`}
            aria-label="More document options"
            aria-haspopup="true"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen(prev => !prev)}
            title="More options"
          >
            <MoreHorizontal size={19} />
          </button>

          {menuOpen && (
            <div className="header-dropdown-menu" role="menu">
              {document && (
                <>
                  <button
                    className="dropdown-item"
                    role="menuitem"
                    onClick={handleDownload}
                    disabled={downloading}
                  >
                    {downloading ? <Loader2 size={15} className="status-spin" /> : <Download size={15} />}
                    <span>{downloading ? 'Downloading...' : 'Download PDF'}</span>
                  </button>

                  <button
                    className="dropdown-item"
                    role="menuitem"
                    onClick={() => {
                      setMenuOpen(false)
                      setDetailsOpen(true)
                    }}
                  >
                    <Info size={15} />
                    <span>Document details</span>
                  </button>

                  <button
                    className="dropdown-item"
                    role="menuitem"
                    onClick={handleCopyLink}
                  >
                    {copied ? <Check size={15} color="#70A77A" /> : <Copy size={15} />}
                    <span>{copied ? 'Link copied!' : 'Copy link'}</span>
                  </button>

                  <div className="dropdown-divider" />

                  <button
                    className="dropdown-item dropdown-item-danger"
                    role="menuitem"
                    onClick={handleDelete}
                  >
                    <Trash2 size={15} />
                    <span>Delete document</span>
                  </button>
                </>
              )}

              {!document && (
                <div className="dropdown-empty-state">
                  <span>No active document</span>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Document Details Modal */}
      {detailsOpen && document && (
        <div className="doc-modal-overlay" onClick={() => setDetailsOpen(false)}>
          <div
            className="doc-modal-card"
            role="dialog"
            aria-modal="true"
            aria-labelledby="doc-details-title"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="doc-modal-header">
              <div className="doc-modal-title-row">
                <div className="doc-modal-icon">
                  <FileText size={20} />
                </div>
                <div>
                  <h3 id="doc-details-title" className="doc-modal-title">{document.name}</h3>
                  <span className={`ready-pill status-${status.toLowerCase()}`}>{status}</span>
                </div>
              </div>
              <button
                className="icon-button"
                onClick={() => setDetailsOpen(false)}
                aria-label="Close details"
              >
                <X size={18} />
              </button>
            </div>

            <div className="doc-modal-body">
              <div className="doc-detail-row">
                <span className="doc-detail-label">File Name</span>
                <span className="doc-detail-value">{document.name}</span>
              </div>
              <div className="doc-detail-row">
                <span className="doc-detail-label">Pages</span>
                <span className="doc-detail-value">{document.pages || 1} {document.pages === 1 ? 'page' : 'pages'}</span>
              </div>
              <div className="doc-detail-row">
                <span className="doc-detail-label">File Size</span>
                <span className="doc-detail-value">{document.size || 'Standard PDF'}</span>
              </div>
              <div className="doc-detail-row">
                <span className="doc-detail-label">Indexing Status</span>
                <span className="doc-detail-value status-text-ready">{document.status}</span>
              </div>
              <div className="doc-detail-row">
                <span className="doc-detail-label">Document ID</span>
                <span className="doc-detail-value doc-id-code">{document.id}</span>
              </div>
            </div>

            <div className="doc-modal-footer">
              <button
                className="subtle-button"
                onClick={handleDownload}
                disabled={downloading}
              >
                <Download size={14} /> Download PDF
              </button>
              <button
                className="primary-button"
                onClick={() => setDetailsOpen(false)}
              >
                Done
              </button>
            </div>
          </div>
        </div>
      )}
    </header>
  )
}
