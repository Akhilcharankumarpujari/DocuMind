import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { Document, Page, pdfjs } from 'react-pdf'
import {
  ChevronLeft,
  ChevronRight,
  Minus,
  Plus,
  Maximize2,
  FileText,
  UploadCloud,
  Loader2,
  AlertCircle,
  ShieldAlert,
  RefreshCw,
} from 'lucide-react'
import { fetchDocumentBlob } from '../../services/documentService'
import 'react-pdf/dist/Page/AnnotationLayer.css'
import 'react-pdf/dist/Page/TextLayer.css'

pdfjs.GlobalWorkerOptions.workerSrc = new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url).toString()

export default function PDFViewer({
  document: activeDoc,
  file: initialFile,
  onUpload,
  targetPage,
}) {
  const [page, setPage] = useState(1)
  const [pages, setPages] = useState(0)
  const [scale, setScale] = useState(1)
  const [pdfSource, setPdfSource] = useState(null)
  const [isDownloading, setIsDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState(null)
  const [renderError, setRenderError] = useState('')
  const [containerWidth, setContainerWidth] = useState(0)
  const [retryCount, setRetryCount] = useState(0)
  const canvasRef = useRef(null)

  // Resolve document metadata whether passed via `document` prop or `file` prop
  const currentDoc = activeDoc || (typeof initialFile === 'object' && initialFile?.id ? initialFile : null)
  const directFile = initialFile instanceof File || initialFile instanceof Blob
    ? initialFile
    : (activeDoc?.file instanceof File || activeDoc?.file instanceof Blob ? activeDoc.file : null)

  // Measure container width for responsive mobile and desktop page fitting
  useEffect(() => {
    if (!canvasRef.current) return

    const updateWidth = () => {
      if (canvasRef.current) {
        const clientWidth = canvasRef.current.clientWidth
        if (clientWidth > 0) {
          setContainerWidth(clientWidth)
        }
      }
    }

    updateWidth()

    const observer = new ResizeObserver(() => {
      updateWidth()
    })
    observer.observe(canvasRef.current)

    window.addEventListener('resize', updateWidth)
    return () => {
      observer.disconnect()
      window.removeEventListener('resize', updateWidth)
    }
  }, [])

  // Safely retrieve authenticated document blob and manage object URLs
  useEffect(() => {
    let isCancelled = false
    let currentBlobUrl = null

    async function loadPdf() {
      setDownloadError(null)
      setRenderError('')
      setPages(0)

      // 1. If no document is selected
      if (!currentDoc && !directFile) {
        setPdfSource(null)
        setIsDownloading(false)
        return
      }

      // 2. If an in-memory File/Blob already exists (e.g. freshly uploaded by user)
      if (directFile) {
        setPdfSource(directFile)
        setIsDownloading(false)
        return
      }

      // 3. Fetch PDF file via authenticated API (allows viewing even if AI indexing failed/processing)
      if (currentDoc?.id) {
        try {
          setIsDownloading(true)
          const blob = await fetchDocumentBlob(currentDoc.id)
          if (isCancelled) return

          const blobUrl = URL.createObjectURL(blob)
          currentBlobUrl = blobUrl
          setPdfSource(blobUrl)
        } catch (err) {
          if (isCancelled) return
          const msg = err.message || 'Failed to download document.'
          const isAuth = msg.includes('expired') || msg.includes('authorized') || msg.includes('401')
          const isNotFound = msg.includes('not be found') || msg.includes('404')

          setDownloadError({
            message: msg,
            isAuthError: isAuth,
            isNotFound: isNotFound,
            isRecoverable: !isNotFound,
          })
          setPdfSource(null)
        } finally {
          if (!isCancelled) {
            setIsDownloading(false)
          }
        }
      }
    }

    loadPdf()

    return () => {
      isCancelled = true
      if (currentBlobUrl) {
        URL.revokeObjectURL(currentBlobUrl)
      }
    }
  }, [currentDoc?.id, currentDoc?.status, directFile, retryCount])

  // Jump to citation target page if requested
  useEffect(() => {
    if (targetPage && targetPage >= 1) {
      setPage(targetPage)
      if (canvasRef.current) {
        canvasRef.current.scrollTop = 0
      }
    }
  }, [targetPage])

  // Reset page when document source changes
  useEffect(() => {
    setPage(targetPage || 1)
    if (canvasRef.current) {
      canvasRef.current.scrollTop = 0
    }
  }, [pdfSource])

  // Reset canvas scroll when navigating between pages
  useEffect(() => {
    if (canvasRef.current) {
      canvasRef.current.scrollTop = 0
    }
  }, [page])

  const onDocumentLoadSuccess = ({ numPages }) => {
    setPages(numPages)
    setPage(targetPage && targetPage <= numPages ? targetPage : 1)
    setRenderError('')
  }

  const onDocumentLoadError = (err) => {
    console.error('react-pdf load error:', err)
    setRenderError('This PDF could not be opened. The file may be damaged or invalid.')
  }

  // Calculate responsive page width to fit mobile viewport while preserving aspect ratio
  const pageWidth = containerWidth > 0
    ? (containerWidth < 680 ? Math.max(260, containerWidth - 28) : Math.min(containerWidth - 40, 800))
    : undefined

  return (
    <section className="pdf-panel">
      <div className="panel-heading">
        <div>
          <span className="eyebrow">DOCUMENT PREVIEW</span>
          <h2>Reading space</h2>
        </div>
        <button
          className="icon-button"
          aria-label="Fit to Width"
          title="Fit to Width (100%)"
          onClick={() => setScale(1.0)}
        >
          <Maximize2 size={16} />
        </button>
      </div>

      <div className="pdf-toolbar">
        <div className="page-controls">
          <button
            className="viewer-control"
            disabled={!pdfSource || page <= 1}
            onClick={() => setPage(p => Math.max(1, p - 1))}
            title="Previous Page"
            aria-label="Previous Page"
          >
            <ChevronLeft size={16} />
          </button>
          <span>Page <strong>{pdfSource ? page : '—'}</strong> of {pages || '—'}</span>
          <button
            className="viewer-control"
            disabled={!pdfSource || (pages > 0 && page >= pages)}
            onClick={() => setPage(p => Math.min(pages, p + 1))}
            title="Next Page"
            aria-label="Next Page"
          >
            <ChevronRight size={16} />
          </button>
        </div>

        <div className="zoom-controls">
          <button
            className="viewer-control"
            disabled={!pdfSource}
            onClick={() => setScale(s => Math.max(0.6, +(s - 0.1).toFixed(2)))}
            title="Zoom Out"
            aria-label="Zoom Out"
          >
            <Minus size={15} />
          </button>
          <span>{Math.round(scale * 100)}%</span>
          <button
            className="viewer-control"
            disabled={!pdfSource}
            onClick={() => setScale(s => Math.min(2.0, +(s + 0.1).toFixed(2)))}
            title="Zoom In"
            aria-label="Zoom In"
          >
            <Plus size={15} />
          </button>
        </div>
      </div>

      {currentDoc?.status === 'Failed' && (
        <div className="pdf-warning-strip">
          <AlertCircle size={14} />
          <span>AI indexing was interrupted for this document. You can read the PDF here, or re-upload to enable AI chat.</span>
        </div>
      )}

      {/* INDEPENDENT SCROLL CONTAINER FOR PDF */}
      <div className="pdf-canvas" ref={canvasRef}>
        {isDownloading ? (
          <div className="pdf-loading-state" role="status">
            <Loader2 className="animate-spin" size={34} />
            <h4>Downloading document…</h4>
            <p>Retrieving protected PDF content securely</p>
          </div>
        ) : downloadError ? (
          <div className="pdf-error-state" role="alert">
            <div className="pdf-error-icon">
              {downloadError.isAuthError ? <ShieldAlert size={30} /> : <AlertCircle size={30} />}
            </div>
            <h3>{downloadError.isAuthError ? 'Authentication Required' : 'Unable to Display Document'}</h3>
            <p>{downloadError.message}</p>
            <div className="pdf-error-actions">
              {downloadError.isRecoverable && (
                <button
                  type="button"
                  className="primary-button"
                  onClick={() => setRetryCount(c => c + 1)}
                >
                  <RefreshCw size={15} /> Retry
                </button>
              )}
              {downloadError.isAuthError ? (
                <Link to="/login" className="subtle-button">
                  Sign In Again
                </Link>
              ) : (
                onUpload && (
                  <button type="button" className="subtle-button" onClick={onUpload}>
                    <UploadCloud size={15} /> Choose a PDF
                  </button>
                )
              )}
            </div>
          </div>
        ) : pdfSource ? (
          <Document
            file={pdfSource}
            onLoadSuccess={onDocumentLoadSuccess}
            onLoadError={onDocumentLoadError}
            loading={
              <div className="pdf-loading-state">
                <Loader2 className="animate-spin" size={32} />
                <h4>Rendering PDF…</h4>
                <p>Preparing document pages</p>
              </div>
            }
            error={
              <div className="pdf-error-state">
                <div className="pdf-error-icon">
                  <AlertCircle size={30} />
                </div>
                <h3>Failed to Render PDF</h3>
                <p>{renderError || 'The PDF format could not be processed.'}</p>
                <div className="pdf-error-actions">
                  <button
                    type="button"
                    className="primary-button"
                    onClick={() => setRetryCount(c => c + 1)}
                  >
                    <RefreshCw size={15} /> Retry
                  </button>
                </div>
              </div>
            }
          >
            <Page
              pageNumber={page}
              width={pageWidth}
              scale={scale}
              renderTextLayer
              renderAnnotationLayer
              loading={
                <div className="pdf-page-loading">
                  <Loader2 className="animate-spin" size={24} />
                  <span>Loading page {page}…</span>
                </div>
              }
            />
          </Document>
        ) : (
          <div className="pdf-empty">
            <div className="empty-file-icon"><FileText size={30} /></div>
            <h3>Your reading space</h3>
            <p>Upload a PDF to read it here and ask DocuMind questions about its content.</p>
            {onUpload && (
              <button type="button" className="primary-button" onClick={onUpload}>
                <UploadCloud size={16} /> Choose a PDF
              </button>
            )}
            <span>PDF files up to 20 MB</span>
          </div>
        )}
      </div>
    </section>
  )
}
