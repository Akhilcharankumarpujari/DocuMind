import {
  Clock,
  Search,
  ShieldCheck,
  Navigation,
  Sparkles,
  TrendingUp,
  FileCheck
} from 'lucide-react'

export default function WhyDocuMindSection() {
  const points = [
    {
      icon: <Clock size={20} />,
      title: 'Reduce Reading Time by 90%',
      desc: 'Instead of manually reading through 50+ page technical reports, ask DocuMind for summaries, key conclusions, or specific clause comparisons in seconds.'
    },
    {
      icon: <Search size={20} />,
      title: 'Find Buried Insights Faster',
      desc: 'Forget tedious Ctrl+F keyword searching. High-dimensional vector search understands conceptual context and synonyms, pinpointing answers regardless of terminology.'
    },
    {
      icon: <ShieldCheck size={20} />,
      title: 'Zero Hallucinations — 100% Grounded',
      desc: 'Every response is strictly restricted to the content of your uploaded PDFs. Built-in system instructions ensure the AI never speculates beyond source material.'
    },
    {
      icon: <Navigation size={20} />,
      title: 'Direct Navigation to Cited Pages',
      desc: 'Audit every answer immediately. Simply click any citation chip to jump the integrated PDF viewer directly to the exact page referenced by the AI.'
    }
  ]

  return (
    <section className="why-documind-section" id="why-documind">
      <div className="landing-container">
        <div className="section-header-center">
          <div className="section-badge">
            <Sparkles size={13} />
            <span>Value Proposition</span>
          </div>
          <h2 className="section-title">
            Why Professionals Choose <span className="coral-text">DocuMind AI</span>
          </h2>
          <p className="section-subtitle">
            Engineered specifically for mission-critical workflows where accuracy, traceability,
            and grounded truth are non-negotiable.
          </p>
        </div>

        <div className="why-grid">
          {/* Left: Value Points */}
          <div className="why-items">
            {points.map((pt, idx) => (
              <div key={idx} className="why-item-card">
                <div className="why-item-icon">
                  {pt.icon}
                </div>
                <div className="why-item-content">
                  <h4>{pt.title}</h4>
                  <p>{pt.desc}</p>
                </div>
              </div>
            ))}
          </div>

          {/* Right: Metrics & Architecture Showcase */}
          <div className="why-stats-panel">
            <div className="stats-grid">
              <div className="stat-item">
                <strong>10x</strong>
                <span>Faster document comprehension and synthesis</span>
              </div>
              <div className="stat-item">
                <strong>100%</strong>
                <span>Traceable page-level citations for every claim</span>
              </div>
              <div className="stat-item">
                <strong>&lt; 1s</strong>
                <span>Average dense vector similarity retrieval time</span>
              </div>
              <div className="stat-item">
                <strong>1536</strong>
                <span>Vector dimensions for deep semantic comprehension</span>
              </div>
            </div>

            <div className="stats-callout-quote">
              &ldquo;DocuMind AI bridges the gap between raw PDF documents and actionable intelligence.
              With page-level citations and split-view reading, you never have to trust an AI answer blindly.&rdquo;
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
