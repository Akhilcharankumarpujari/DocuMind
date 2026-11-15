import { Link } from 'react-router-dom'
import { ArrowRight, Sparkles } from 'lucide-react'
import GithubIcon from './GithubIcon.jsx'

export default function FinalCtaSection() {
  return (
    <section className="final-cta-section" id="final-cta">
      <div className="landing-container">
        <div className="final-cta-card">
          <div className="final-cta-glow" aria-hidden="true" />
          
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 6, background: 'rgba(217, 119, 87, 0.2)', border: '1px solid rgba(217, 119, 87, 0.4)', padding: '5px 14px', borderRadius: 999, fontSize: 12, fontWeight: 700, color: '#EAA58F', marginBottom: 20 }}>
            <Sparkles size={14} />
            <span>Ready for Instant Document Intelligence?</span>
          </div>

          <h2>Your Documents Have Answers. Start Finding Them.</h2>

          <p>
            Stop skimming hundreds of pages. Upload your first PDF and experience intelligent,
            verifiable document reasoning in seconds.
          </p>

          <div className="final-cta-buttons">
            <Link to="/app" className="btn-cta-white">
              <span>Get Started</span>
              <ArrowRight size={17} />
            </Link>

            <a
              href="https://github.com/Akhilcharankumarpujari/DocuMind"
              target="_blank"
              rel="noopener noreferrer"
              className="btn-cta-ghost"
            >
              <GithubIcon size={17} />
              <span>Star on GitHub</span>
            </a>
          </div>
        </div>
      </div>
    </section>
  )
}
