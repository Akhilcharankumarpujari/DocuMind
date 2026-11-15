import { useEffect, useRef, useState } from 'react'
import { FileText, MessageSquare, FolderOpen } from 'lucide-react'
import { Sidebar } from '../components/index.js'
import Header from '../components/Header/Header.jsx'
import PDFViewer from '../components/PDFViewer/PDFViewer.jsx'
import ChatPanel from '../components/ChatPanel/ChatPanel.jsx'
import { getDocuments, uploadDocument, deleteDocument, getDocumentStatus, getDocumentFileUrl } from '../services/documentService.js'

export default function Dashboard() {
  const [documents, setDocuments] = useState([])
  const [activeDocument, setActiveDocument] = useState(null)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const [mobileTab, setMobileTab] = useState('chat') // 'pdf' | 'chat'
  const [notice, setNotice] = useState('')
  const [targetPage, setTargetPage] = useState(1)
  const [actionPrompt, setActionPrompt] = useState('')
  const [loadingDocs, setLoadingDocs] = useState(true)
  const fileInput = useRef(null)

  // 1. Fetch documents from backend on initial mount
  useEffect(() => {
    let isCancelled = false

    async function fetchDocs() {
      try {
        setLoadingDocs(true)
        const list = await getDocuments()
        if (isCancelled) return

        if (Array.isArray(list)) {
          setDocuments(list)
          if (list.length > 0) {
            setActiveDocument(list[0])
          }
        }
      } catch (err) {
        if (!isCancelled) {
          setNotice(`Could not load documents: ${err.message}`)
        }
      } finally {
        if (!isCancelled) {
          setLoadingDocs(false)
        }
      }
    }

    fetchDocs()
    return () => { isCancelled = true }
  }, [])

  // 2. Poll status if active document is Pending or Processing
  useEffect(() => {
    if (!activeDocument?.id) return
    if (activeDocument.status === 'Ready' || activeDocument.status === 'Failed') return

    const pollInterval = setInterval(async () => {
      try {
        const statusData = await getDocumentStatus(activeDocument.id)
        if (statusData && statusData.status !== activeDocument.status) {
          setActiveDocument(prev => prev ? { ...prev, status: statusData.status, pages: statusData.pages || prev.pages } : null)
          setDocuments(prev => prev.map(d => d.id === statusData.id ? { ...d, status: statusData.status, pages: statusData.pages || d.pages } : d))

          if (statusData.status === 'Ready') {
            setNotice(`"${activeDocument.name}" is indexed and ready for chat!`)
          } else if (statusData.status === 'Failed') {
            setNotice(`Processing failed for "${activeDocument.name}": ${statusData.errorMessage || 'Unknown error'}`)
          }
        }
      } catch {
        // Ignore polling errors
      }
    }, 2000)

    return () => clearInterval(pollInterval)
  }, [activeDocument?.id, activeDocument?.status])

  const chooseFile = () => fileInput.current?.click()

  // 3. Handle actual file upload to backend
  const handleFile = async (event) => {
    const file = event.target.files?.[0]
    if (!file) return

    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      setNotice('Please choose a valid PDF file.')
      return
    }

    if (file.size > 20 * 1024 * 1024) {
      setNotice('The maximum PDF size is 20 MB.')
      return
    }

    setNotice(`Uploading and analyzing "${file.name}"...`)

    try {
      const createdDoc = await uploadDocument(file)

      // Add file object locally for instantaneous preview while server finishes
      const docWithFile = {
        ...createdDoc,
        file
      }

      setDocuments(prev => [docWithFile, ...prev.filter(d => d.id !== createdDoc.id)])
      setActiveDocument(docWithFile)
      setTargetPage(1)
      setMobileTab('pdf')

      if (createdDoc.status === 'Ready') {
        setNotice(`"${createdDoc.name}" is ready! You can now ask questions about it.`)
      } else {
        setNotice(`"${createdDoc.name}" uploaded. Processing pages and vectors...`)
      }
    } catch (err) {
      setNotice(`Upload failed: ${err.message}`)
    } finally {
      event.target.value = ''
    }
  }

  // 4. Handle document deletion
  const handleDeleteDocument = async (id) => {
    try {
      await deleteDocument(id)
      setDocuments(prev => prev.filter(d => d.id !== id))
      if (activeDocument?.id === id) {
        const remaining = documents.filter(d => d.id !== id)
        setActiveDocument(remaining.length > 0 ? remaining[0] : null)
        setTargetPage(1)
      }
      setNotice('Document deleted successfully.')
    } catch (err) {
      setNotice(`Failed to delete document: ${err.message}`)
    }
  }

  // 5. Handle Header quick actions
  const handleAction = (action) => {
    if (!activeDocument) {
      setNotice('Upload or select a PDF first.')
      return
    }
    if (activeDocument.status !== 'Ready') {
      setNotice(`Document is still ${activeDocument.status.toLowerCase()}. Please wait until it is ready.`)
      return
    }

    setMobileTab('chat')

    switch (action) {
      case 'summary':
        setActionPrompt('Summarize this document concisely.')
        break
      case 'explain':
        setActionPrompt('Explain the key concepts and findings in this document.')
        break
      case 'rewrite':
        setActionPrompt('Rewrite the main conclusions of this document in simple bullet points.')
        break
      default:
        break
    }
  }

  // Determine file prop for PDFViewer
  const pdfFileProp = activeDocument
    ? (activeDocument.file || { url: getDocumentFileUrl(activeDocument.id) })
    : null

  return (
    <main className={`app-shell ${sidebarCollapsed ? 'is-collapsed' : ''} show-${mobileTab}`}>
      <input
        ref={fileInput}
        className="visually-hidden"
        type="file"
        accept="application/pdf,.pdf"
        onChange={handleFile}
      />

      {/* Mobile-only Segmented 3-Panel Switcher */}
      <div className="mobile-panel-switcher" role="tablist" aria-label="Workspace view options">
        <button
          type="button"
          role="tab"
          aria-selected={mobileTab === 'docs'}
          className={`mobile-switch-btn ${mobileTab === 'docs' ? 'active' : ''}`}
          onClick={() => setMobileTab('docs')}
        >
          <FolderOpen size={14} />
          <span>Documents ({documents.length})</span>
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={mobileTab === 'pdf'}
          className={`mobile-switch-btn ${mobileTab === 'pdf' ? 'active' : ''}`}
          onClick={() => setMobileTab('pdf')}
        >
          <FileText size={14} />
          <span>PDF Viewer</span>
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={mobileTab === 'chat'}
          className={`mobile-switch-btn ${mobileTab === 'chat' ? 'active' : ''}`}
          onClick={() => setMobileTab('chat')}
        >
          <MessageSquare size={14} />
          <span>AI Assistant</span>
        </button>
      </div>

      <Sidebar
        documents={documents}
        activeDocument={activeDocument}
        onSelectDocument={(doc) => {
          setActiveDocument(doc)
          setTargetPage(1)
          if (window.innerWidth <= 820) {
            setMobileTab('pdf')
          }
        }}
        onUpload={chooseFile}
        onDeleteDocument={handleDeleteDocument}
        collapsed={sidebarCollapsed}
        onToggle={() => setSidebarCollapsed(v => !v)}
      />
      <div className="workspace">
        <Header
          document={activeDocument}
          onUpload={chooseFile}
          onAction={handleAction}
          onDelete={handleDeleteDocument}
        />

        {/* Independent Multi-Panel Workspace Grid */}
        <div className={`workspace-grid show-${mobileTab}`}>
          <PDFViewer
            document={activeDocument}
            file={activeDocument?.file}
            onUpload={chooseFile}
            targetPage={targetPage}
          />
          <ChatPanel
            document={activeDocument}
            actionPrompt={actionPrompt}
            onClearActionPrompt={() => setActionPrompt('')}
            onCitationClick={(page) => {
              setTargetPage(page)
              setMobileTab('pdf') // Auto-switch to PDF tab on mobile when citation is tapped!
            }}
          />
        </div>
      </div>
      {notice && (
        <div className="toast" role="status">
          {notice}
          <button onClick={() => setNotice('')} aria-label="Close notification">×</button>
        </div>
      )}
    </main>
  )
}
