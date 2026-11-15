import { Link } from 'react-router-dom'
import {
  FileText,
  ArrowRight,
  Sparkles,
  CheckCircle2,
  Send,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  ZoomIn,
  Bot
} from 'lucide-react'

export default function HeroSection() {
  return (
    <section className="hero-section" id="hero">
      <div className="hero-glow" aria-hidden="true" />
      <div className="landing-container">
        <div className="hero-content">
          {/* Badge */}
          <div className="hero-pill">
            <span className="hero-pill-badge">NEW</span>
            <Sparkles size={14} />
            <span>AI-Powered Document Intelligence • RAG & Citations</span>
          </div>

          {/* Headline */}
          <h1 className="hero-headline">
            Understand Your Documents.<br />
            <span className="gradient-coral">Ask Anything.</span> Get Answers Instantly.
          </h1>

          {/* Description */}
          <p className="hero-description">
            Transform the way you work with PDFs. Upload documents, ask questions in natural language,
            and get AI-powered answers backed by accurate page citations.
          </p>

          {/* CTA Buttons */}
          <div className="hero-cta-group">
            <Link to="/app" className="btn-hero-primary">
              <span>Start Analyzing</span>
              <ArrowRight size={18} />
            </Link>
            <a href="#features" className="btn-hero-secondary">
              <span>Explore Features</span>
            </a>
          </div>

          {/* Trust Highlights */}
          <div className="hero-trust-badges">
            <div className="hero-trust-badge">
              <CheckCircle2 size={16} />
              <span>100% Grounded in your PDFs</span>
            </div>
            <div className="hero-trust-badge">
              <CheckCircle2 size={16} />
              <span>Verifiable Page Citations</span>
            </div>
            <div className="hero-trust-badge">
              <CheckCircle2 size={16} />
              <span>Real-Time SSE Streaming</span>
            </div>
          </div>
        </div>

        {/* Visual Application Preview Mockup */}
        <div className="hero-preview-wrapper">
          <div className="preview-window">
            {/* Window chrome bar */}
            <div className="preview-window-topbar">
              <div className="window-dots">
                <span className="window-dot red" />
                <span className="window-dot yellow" />
                <span className="window-dot green" />
              </div>
              <div className="preview-filename">
                <FileText size={15} color="#D97757" />
                <span>RAG_Architectural_Spec_v2.pdf</span>
                <span className="preview-pill-ready">Ready</span>
              </div>
              <div style={{ width: 48 }} />
            </div>

            {/* Split Screen Application Canvas */}
            <div className="preview-grid">
              {/* Left Side: PDF Viewer */}
              <div className="preview-pdf-side">
                <div className="preview-pdf-toolbar">
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <button type="button" className="viewer-control" style={{ width: 24, height: 24 }}>
                      <ChevronLeft size={13} />
                    </button>
                    <span>Page <strong>4</strong> of 24</span>
                    <button type="button" className="viewer-control" style={{ width: 24, height: 24 }}>
                      <ChevronRight size={13} />
                    </button>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <ZoomIn size={13} />
                    <span>100%</span>
                  </div>
                </div>

                <div className="preview-pdf-sheet">
                  <h4>3.2 High-Dimensional Vector Search & Hybrid Chunking</h4>
                  <p>
                    In retrieval-augmented generation pipelines, text segmentation strategy directly governs
                    relevance scores. Fixed-size windows of 1,000 tokens with 200-token semantic overlap preserve
                    syntactic continuity across boundaries.
                  </p>
                  <div className="pdf-highlight-box">
                    <strong>Referenced Excerpt:</strong> Dense vectors produced by 1536-dimensional embedding models
                    demonstrate a 94.2% top-3 recall accuracy under cosine distance metrics when paired with
                    Qdrant HNSW indexing structures.
                  </div>
                  <p>
                    By extracting text with document-level positional metadata, the orchestration layer binds each
                    vector point to its absolute source page number.
                  </p>
                </div>
              </div>

              {/* Right Side: AI Chat with Citations */}
              <div className="preview-chat-side">
                <div className="preview-chat-header">
                  <div className="preview-chat-title">
                    <Bot size={17} color="#D97757" />
                    <span>DocuMind Assistant</span>
                    <span className="preview-pulse-dot" title="Active model: gpt-4.1-mini" />
                  </div>
                  <span style={{ fontSize: 10, color: '#9D8E88', fontWeight: 600 }}>Streaming Active</span>
                </div>

                <div className="preview-chat-stream">
                  {/* User Query */}
                  <div className="preview-msg user">
                    <div className="preview-msg-bubble">
                      How does the semantic chunking strategy improve vector retrieval accuracy?
                    </div>
                  </div>

                  {/* Assistant Answer */}
                  <div className="preview-msg assistant">
                    <div className="preview-msg-bubble">
                      DocuMind uses <strong>1,000-token segments with 200-token overlap</strong> to maintain
                      semantic continuity across paragraph breaks. When paired with 1536-dimensional embeddings
                      in Qdrant, this achieves a <strong>94.2% top-3 recall rate</strong>.
                      <div className="preview-citation-bar">
                        <span style={{ fontSize: 9, fontWeight: 700, color: '#A18E84', textTransform: 'uppercase' }}>
                          Verified Sources:
                        </span>
                        <div className="preview-citation-chip" title="Click to view page 4 in viewer">
                          <BookOpen size={11} color="#D97757" />
                          <span>Page <strong>4</strong></span>
                          <span className="preview-citation-match">94% match</span>
                        </div>
                        <div className="preview-citation-chip" title="Click to view page 7 in viewer">
                          <BookOpen size={11} color="#D97757" />
                          <span>Page <strong>7</strong></span>
                          <span className="preview-citation-match">89% match</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Input prompt box */}
                <div className="preview-input-box">
                  <span>Ask a follow-up question about Section 3.2...</span>
                  <button type="button" className="preview-send-btn" aria-label="Send query">
                    <Send size={13} />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
