import { FileText, Upload, MoreHorizontal, Sparkles } from 'lucide-react'

export default function Header({ document, onUpload, onAction }) {
  const status = document?.status || (document ? 'Ready' : 'No document')

  return (
    <header className="topbar">
      <div className="breadcrumb">
        <span>Workspace</span>
        <span className="crumb-slash">/</span>
        <FileText size={15}/>
        <strong>{document?.name || 'New document'}</strong>
        <span className={`ready-pill status-${status.toLowerCase()}`}>
          {status}
        </span>
      </div>
      <div className="topbar-actions">
        <button className="subtle-button" onClick={() => onAction('summary')} title="Summarize document">
          <Sparkles size={15}/> Summary
        </button>
        <button className="subtle-button" onClick={() => onAction('explain')} title="Explain key findings">
          Explain
        </button>
        <button className="subtle-button" onClick={() => onAction('rewrite')} title="Rewrite main points">
          Rewrite
        </button>
        <button className="primary-button upload-top" onClick={onUpload}>
          <Upload size={15}/> Upload PDF
        </button>
        <button className="icon-button" aria-label="More options"><MoreHorizontal size={19}/></button>
      </div>
    </header>
  )
}
