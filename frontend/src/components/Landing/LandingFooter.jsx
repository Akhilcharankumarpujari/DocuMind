import { Link } from 'react-router-dom'
import { FileText, Heart } from 'lucide-react'
import GithubIcon from './GithubIcon.jsx'

export default function LandingFooter() {
  const currentYear = new Date().getFullYear()

  return (
    <footer className="landing-footer">
      <div className="landing-container">
        <div className="footer-top">
          {/* Brand Col */}
          <div className="footer-brand">
            <Link to="/" className="landing-brand" aria-label="DocuMind AI Home">
              <img
                src="/documind-logo-horizontal-white.png"
                alt="DocuMind AI Logo"
                className="footer-brand-logo-img"
              />
            </Link>
            <p>
              Transforming complex PDFs into conversational intelligence. Fast, grounded answers
              backed by verifiable page citations.
            </p>
          </div>

          {/* Product Links */}
          <div className="footer-col">
            <h5>Product</h5>
            <ul className="footer-links">
              <li><Link to="/app">Workspace Dashboard</Link></li>
              <li><a href="#features">Features</a></li>
              <li><a href="#product-preview">Interactive Preview</a></li>
              <li><a href="#how-it-works">How It Works</a></li>
              <li><a href="#why-documind">Why DocuMind</a></li>
            </ul>
          </div>

          {/* Architecture & Tech Stack */}
          <div className="footer-col">
            <h5>Architecture</h5>
            <ul className="footer-links">
              <li><span style={{ color: '#A99992', fontSize: '13.5px' }}>.NET 10 ASP.NET Core</span></li>
              <li><span style={{ color: '#A99992', fontSize: '13.5px' }}>Azure OpenAI (gpt-4.1-mini)</span></li>
              <li><span style={{ color: '#A99992', fontSize: '13.5px' }}>Qdrant Vector Engine</span></li>
              <li><span style={{ color: '#A99992', fontSize: '13.5px' }}>MySQL 8 Relational Store</span></li>
              <li><span style={{ color: '#A99992', fontSize: '13.5px' }}>Docker Multi-Container</span></li>
            </ul>
          </div>

          {/* Resources */}
          <div className="footer-col">
            <h5>Resources</h5>
            <ul className="footer-links">
              <li>
                <a
                  href="https://github.com/Akhilcharankumarpujari/DocuMind"
                  target="_blank"
                  rel="noopener noreferrer"
                  style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
                >
                  <GithubIcon size={14} />
                  <span>GitHub Repository</span>
                </a>
              </li>
              <li><a href="#faq">FAQ</a></li>
              <li><Link to="/app">Upload a PDF</Link></li>
            </ul>
          </div>
        </div>

        {/* Bottom Bar */}
        <div className="footer-bottom">
          <div>
            &copy; {currentYear} DocuMind AI. Built with precision and care.
          </div>
          <div>
            <a
              href="https://github.com/Akhilcharankumarpujari/DocuMind"
              target="_blank"
              rel="noopener noreferrer"
              style={{ color: '#A99992', textDecoration: 'none' }}
            >
              github.com/Akhilcharankumarpujari/DocuMind
            </a>
          </div>
        </div>
      </div>
    </footer>
  )
}
