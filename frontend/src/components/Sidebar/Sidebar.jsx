import { useState } from 'react'
import { FileText, MessageSquare, Plus, Search, Settings, PanelLeftClose, FolderOpen, Sparkles, Trash2, Loader2 } from 'lucide-react'

export default function Sidebar({
  documents,
  activeDocument,
  onSelectDocument,
  onUpload,
  onDeleteDocument,
  collapsed,
  onToggle
}) {
  const [searchTerm, setSearchTerm] = useState('')

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
        <div className="brand-mark"><FileText size={19} /></div>
        {!collapsed && <div className="brand-name">Docu<span>Mind</span><small>AI DOCUMENT ASSISTANT</small></div>}
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
        <button className="nav-item"><Settings size={17}/>{!collapsed && 'Settings'}</button>
        <div className="profile-row"><div className="avatar">M</div>{!collapsed && <div><strong>My Workspace</strong><small>Personal account</small></div>}</div>
      </div>
    </aside>
  )
}
