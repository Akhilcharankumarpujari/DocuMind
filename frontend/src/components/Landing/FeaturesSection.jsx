import {
  MessageSquare,
  Database,
  ShieldCheck,
  BookOpen,
  Search,
  Cpu,
  Layers,
  Lock,
  Sparkles,
  ArrowUpRight
} from 'lucide-react'

export default function FeaturesSection() {
  const features = [
    {
      icon: <MessageSquare size={24} />,
      tag: 'Available in v1.0',
      title: 'AI-Powered PDF Chat',
      desc: 'Engage in natural conversations with any complex document. Ask exploratory questions, request concise summaries, or clarify technical definitions instantly.',
      meta: 'Powered by Azure OpenAI gpt-4.1-mini'
    },
    {
      icon: <Database size={24} />,
      tag: 'Core RAG Architecture',
      title: 'Retrieval-Augmented Generation',
      desc: 'Combines dynamic 1000-token semantic chunking with dense 1536-dimensional embeddings in Qdrant, retrieving only relevant context for each question.',
      meta: 'Qdrant HNSW Vector Search'
    },
    {
      icon: <Sparkles size={24} />,
      tag: 'Zero Hallucination',
      title: 'Context-Aware Answers',
      desc: 'Responses are strictly bounded to the content of your uploaded PDFs. Built-in grounding guardrails ensure the model never invents unverified information.',
      meta: 'Strict Source Grounding'
    },
    {
      icon: <BookOpen size={24} />,
      tag: 'Audit Ready',
      title: 'Page-Level Citations',
      desc: 'Every statement is linked to an exact source page number with match confidence metrics. Click any citation chip to jump directly to that page in the PDF.',
      meta: 'Click-to-Page Verification'
    },
    {
      icon: <Search size={24} />,
      tag: 'Vector Embeddings',
      title: 'Semantic Document Search',
      desc: 'Go beyond basic keyword matching. Semantic search understands conceptual meaning and synonyms across hundreds of document pages.',
      meta: 'text-embedding-3-small'
    },
    {
      icon: <ShieldCheck size={24} />,
      tag: 'Isolated Pipeline',
      title: 'Secure Document Processing',
      desc: 'Documents and vector indices are managed in isolated containerized volumes. Metadata is safely orchestrated via transactional MySQL 8 storage.',
      meta: 'Private Docker Architecture'
    }
  ]

  return (
    <section className="features-section" id="features">
      <div className="landing-container">
        <div className="section-header-center">
          <div className="section-badge">
            <Cpu size={13} />
            <span>Core Capabilities</span>
          </div>
          <h2 className="section-title">
            Built for <span className="coral-text">Rigorous Document Analysis</span>
          </h2>
          <p className="section-subtitle">
            Every feature is purposefully designed to help students, researchers, lawyers, and analysts
            extract reliable answers from lengthy PDFs with absolute confidence.
          </p>
        </div>

        <div className="features-grid">
          {features.map((item, idx) => (
            <div key={idx} className="feature-card">
              <span className="feature-tag">{item.tag}</span>
              <div className="feature-icon-box">
                {item.icon}
              </div>
              <h3 className="feature-title">{item.title}</h3>
              <p className="feature-desc">{item.desc}</p>
              <div className="feature-meta">
                <span>{item.meta}</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
