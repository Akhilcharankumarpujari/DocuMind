import { useState } from 'react'
import { Link } from 'react-router-dom'
import { FileText, MessageSquare, Plus, Search, Settings, PanelLeftClose, FolderOpen, Sparkles, Trash2, Loader2, LogOut } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'

export default function Sidebar({
  documents,
  activeDocument,
  onSelectDocument,
  onUpload,
  onDeleteDocument,
  collapsed,
  onToggle
}) {
  const { user, logout } = useAuth()
  const [searchTerm, setSearchTerm] = useState('')

  const userInitial = (user?.fullName || user?.email || 'U').charAt(0).toUpperCase()

  const filteredDocs = searchTerm.trim()
    ? documents.filter(d => d.name?.toLowerCase().includes(searchTerm.toLowerCase()))
    : documents

  const getStatusBadge = (status) => {
    switch (status?.toLowerCase()) {
      case 'ready':
        return <span className="status-dot status-ready" title="Ready" />
      case 'processing':
        return <Loader2 size={11} className="status-spin" title="Processing..." />
      case 'failed':
        return <span className="status-dot status-failed" title="Processing failed" />
      default:
        return <span className="status-dot status-pending" title="Pending" />
    }
  }

  return (
    <aside className={`sidebar ${collapsed ? 'sidebar--collapsed' : ''}`}>
      <div className="brand-row">
        <Link to="/" title="Return to Landing Page" style={{ textDecoration: 'none', color: 'inherit', display: 'flex', alignItems: 'center', gap: 11 }}>
          <div className="brand-mark">
            <img
              src="/documind-icon.png"
              alt="DocuMind AI Logo"
              className="brand-icon-img"
            />
          </div>
          {!collapsed && <div className="brand-name">Docu<span>Mind</span><small>AI DOCUMENT ASSISTANT</small></div>}
        </Link>
        <button className="icon-button sidebar-toggle" onClick={onToggle} aria-label="Toggle sidebar"><PanelLeftClose size={17} /></button>
      </div>
      <button className="new-chat-button" onClick={onUpload}><Plus size={17} />{!collapsed && 'Upload document'}</button>

      {!collapsed && (
        <div className="sidebar-search">
          <Search size={15} />
          <input
            type="text"
            placeholder="Search documents"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      )}

      <div className="sidebar-section">
        {!collapsed && <div className="section-label">WORKSPACE</div>}
        <button className="nav-item active"><MessageSquare size={17}/>{!collapsed && 'AI Assistant'}</button>
        <button className="nav-item"><FolderOpen size={17}/>{!collapsed && 'My documents'}<span className="nav-count">{documents.length}</span></button>
      </div>

      <div className="sidebar-section document-list">
        {!collapsed && <div className="section-label">RECENT DOCUMENTS</div>}
        {filteredDocs.map((doc) => (
          <div
            key={doc.id}
            className={`document-nav-wrapper ${activeDocument?.id === doc.id ? 'selected' : ''}`}
          >
            <button
              className="document-nav"
              onClick={() => onSelectDocument(doc)}
              title={doc.name}
            >
              <FileText size={16}/>
              {!collapsed && (
                <>
                  <span className="doc-title">{doc.name}</span>
                  {getStatusBadge(doc.status)}
                </>
              )}
            </button>
            {!collapsed && onDeleteDocument && (
              <button
                className="doc-delete-btn"
                title="Delete document"
                onClick={(e) => {
                  e.stopPropagation()
                  if (confirm(`Delete "${doc.name}"?`)) {
                    onDeleteDocument(doc.id)
                  }
                }}
              >
                <Trash2 size={13}/>
              </button>
            )}
          </div>
        ))}
        {!filteredDocs.length && !collapsed && (
          <p className="sidebar-empty">
            {searchTerm ? 'No matching documents.' : 'Your documents will appear here.'}
          </p>
        )}
      </div>

      <div className="sidebar-bottom">
        <div className="plan-card"><div className="plan-icon"><Sparkles size={16}/></div>{!collapsed && <div><strong>DocuMind Plus</strong><small>Your workspace is ready</small></div>}</div>
        <div className="profile-row" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, minWidth: 0 }}>
            <div className="avatar" title={user?.fullName || 'User'}>{userInitial}</div>
            {!collapsed && (
              <div style={{ overflow: 'hidden' }}>
                <strong style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {user?.fullName || 'DocuMind User'}
                </strong>
                <small style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {user?.email || 'Authenticated'}
                </small>
              </div>
            )}
          </div>
          {!collapsed && (
            <button
              type="button"
              className="icon-button"
              title="Sign Out"
              onClick={logout}
              style={{ color: '#B9AAA4', marginLeft: 'auto', flexShrink: 0 }}
            >
              <LogOut size={16} />
            </button>
          )}
        </div>
      </div>
    </aside>
  )
}
