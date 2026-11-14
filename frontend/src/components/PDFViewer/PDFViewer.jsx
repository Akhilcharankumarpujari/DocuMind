import { useEffect, useState } from 'react'
import { Document, Page, pdfjs } from 'react-pdf'
import { ChevronLeft, ChevronRight, Minus, Plus, Maximize2, FileText, UploadCloud } from 'lucide-react'
import 'react-pdf/dist/Page/AnnotationLayer.css'
import 'react-pdf/dist/Page/TextLayer.css'

pdfjs.GlobalWorkerOptions.workerSrc = new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url).toString()

export default function PDFViewer({ file, onUpload, targetPage }) {
  const [page, setPage] = useState(1)
  const [pages, setPages] = useState(0)
  const [scale, setScale] = useState(1)
  const [error, setError] = useState('')

  // Jump to citation target page if requested
  useEffect(() => {
    if (targetPage && targetPage >= 1) {
      setPage(targetPage)
    }
  }, [targetPage])

  // Reset page when file source changes
  useEffect(() => {
    setPage(targetPage || 1)
    setError('')
  }, [file])

  const onLoad = ({ numPages }) => {
    setPages(numPages)
    setPage(targetPage && targetPage <= numPages ? targetPage : 1)
    setError('')
  }

  return (
    <section className="pdf-panel">
      <div className="panel-heading">
        <div>
          <span className="eyebrow">DOCUMENT PREVIEW</span>
          <h2>Reading space</h2>
        </div>
        <button className="icon-button" aria-label="Fullscreen">
          <Maximize2 size={16}/>
        </button>
      </div>
      <div className="pdf-toolbar">
        <div className="page-controls">
          <button
            className="viewer-control"
            disabled={!file || page <= 1}
            onClick={() => setPage(p => Math.max(1, p - 1))}
          >
            <ChevronLeft size={16}/>
          </button>
          <span>Page <strong>{file ? page : '—'}</strong> of {pages || '—'}</span>
          <button
            className="viewer-control"
            disabled={!file || (pages > 0 && page >= pages)}
            onClick={() => setPage(p => Math.min(pages, p + 1))}
          >
            <ChevronRight size={16}/>
          </button>
        </div>
        <div className="zoom-controls">
          <button
            className="viewer-control"
            onClick={() => setScale(s => Math.max(0.65, +(s - 0.1).toFixed(2)))}
          >
            <Minus size={15}/>
          </button>
          <span>{Math.round(scale * 100)}%</span>
          <button
            className="viewer-control"
            onClick={() => setScale(s => Math.min(1.6, +(s + 0.1).toFixed(2)))}
          >
            <Plus size={15}/>
          </button>
        </div>
      </div>
      <div className="pdf-canvas">
        {file ? (
          <Document
            file={file}
            onLoadSuccess={onLoad}
            onLoadError={() => setError('This PDF could not be opened. Please verify the document is ready.')}
            loading={<div className="pdf-placeholder">Loading PDF…</div>}
          >
            {error ? (
              <div className="pdf-placeholder">{error}</div>
            ) : (
              <Page pageNumber={page} scale={scale} renderTextLayer renderAnnotationLayer />
            )}
          </Document>
        ) : (
          <div className="pdf-empty">
            <div className="empty-file-icon"><FileText size={30}/></div>
            <h3>Your reading space</h3>
            <p>Upload a PDF to read it here and ask DocuMind questions about its content.</p>
            <button className="primary-button" onClick={onUpload}>
              <UploadCloud size={16}/> Choose a PDF
            </button>
            <span>PDF files up to 20 MB</span>
          </div>
        )}
      </div>
    </section>
  )
}
