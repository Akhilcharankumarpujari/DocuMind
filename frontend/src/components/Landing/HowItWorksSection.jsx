import {
  UploadCloud,
  Cpu,
  MessageSquare,
  CheckCircle2,
  Layers,
  ArrowRight
} from 'lucide-react'

export default function HowItWorksSection() {
  const steps = [
    {
      step: '01',
      title: 'Upload Your PDF',
      icon: <UploadCloud size={22} />,
      desc: 'Drag and drop any research paper, legal brief, financial statement, or technical manual up to 20MB directly into the DocuMind workspace.',
      pill: 'Supports up to 20MB PDFs'
    },
    {
      step: '02',
      title: 'Automatic Ingestion & Vector Indexing',
      icon: <Cpu size={22} />,
      desc: 'PdfPig extracts high-fidelity text and page geometry. DocuMind segments text into 1000-token chunks with overlap and generates 1536-dim vector embeddings in Qdrant.',
      pill: 'PdfPig + Qdrant HNSW'
    },
    {
      step: '03',
      title: 'Ask & Verify with Citations',
      icon: <MessageSquare size={22} />,
      desc: 'Ask questions in natural English. DocuMind retrieves the most relevant context, streams answers token-by-token, and provides clickable page citations for instant verification.',
      pill: 'SSE Streaming + Page Chips'
    }
  ]

  return (
    <section className="how-it-works-section" id="how-it-works">
      <div className="landing-container">
        <div className="section-header-center">
          <div className="section-badge">
            <Layers size={13} />
            <span>Process Flow</span>
          </div>
          <h2 className="section-title">
            From Upload to Insight in <span className="coral-text">Three Simple Steps</span>
          </h2>
          <p className="section-subtitle">
            Experience effortless document comprehension. No manual data tagging, complex configurations,
            or tedious setup required.
          </p>
        </div>

        <div className="steps-grid">
          {steps.map((st, idx) => (
            <div key={idx} className="step-card">
              <div className="step-number-badge">
                {st.step}
              </div>
              <h3>{st.title}</h3>
              <p>{st.desc}</p>
              <div className="step-detail-pill">
                <CheckCircle2 size={13} color="#D97757" />
                <span>{st.pill}</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
