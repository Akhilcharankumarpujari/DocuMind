import { useState } from 'react'
import { HelpCircle, ChevronDown } from 'lucide-react'

export default function FaqSection() {
  const [openIndex, setOpenIndex] = useState(0)

  const faqs = [
    {
      q: 'What is DocuMind AI?',
      a: 'DocuMind AI is an intelligent document analysis and RAG (Retrieval-Augmented Generation) platform. It allows users to upload PDF documents and converse with them naturally, receiving precise answers backed by verified page citations and an integrated PDF reader.'
    },
    {
      q: 'How does PDF analysis work?',
      a: 'When you upload a PDF, the backend extracts raw text and positional metadata using PdfPig. It then segments the text into 1,000-token chunks with 200-token overlap to maintain context across paragraph boundaries, converts each chunk into a 1536-dimensional vector using Azure OpenAI embeddings, and indexes them into Qdrant vector storage.'
    },
    {
      q: 'Can I ask questions about my documents?',
      a: 'Yes! You can ask high-level questions like "Summarize the primary conclusions", specific factual inquiries like "What was the operating expense reported in Section 4.2?", or comparative queries across different sections of your document.'
    },
    {
      q: 'How are answers generated?',
      a: 'When you ask a question, DocuMind generates an embedding vector for your query and searches Qdrant using cosine similarity to find the most relevant document chunks. These top chunks are formatted into a grounded prompt for Azure OpenAI (gpt-4.1-mini), which streams the answer token-by-token back to your browser over Server-Sent Events (SSE).'
    },
    {
      q: 'Does DocuMind provide page citations?',
      a: 'Yes, absolutely. Every generated response includes interactive citation chips indicating the exact source page numbers and matching confidence scores. Clicking any citation chip automatically jumps the integrated PDF reader directly to that page so you can verify the original text instantly.'
    },
    {
      q: 'Which document formats are supported?',
      a: 'Currently, DocuMind specializes in PDF documents up to 20 MB, covering academic research papers, legal agreements, technical documentation, and financial reports. Support for additional formats such as DOCX, PPTX, and EPUB is planned in future releases.'
    }
  ]

  const toggleFaq = (index) => {
    setOpenIndex(prev => (prev === index ? -1 : index))
  }

  return (
    <section className="faq-section" id="faq">
      <div className="landing-container">
        <div className="section-header-center">
          <div className="section-badge">
            <HelpCircle size={13} />
            <span>Got Questions?</span>
          </div>
          <h2 className="section-title">
            Frequently Asked <span className="coral-text">Questions</span>
          </h2>
          <p className="section-subtitle">
            Everything you need to know about DocuMind AI, how our RAG pipeline functions,
            and how we guarantee grounded accuracy.
          </p>
        </div>

        <div className="faq-list">
          {faqs.map((faq, index) => {
            const isOpen = openIndex === index
            return (
              <div key={index} className={`faq-item ${isOpen ? 'open' : ''}`}>
                <button
                  type="button"
                  className="faq-question-btn"
                  onClick={() => toggleFaq(index)}
                  aria-expanded={isOpen}
                >
                  <span>{faq.q}</span>
                  <div className="faq-icon-indicator">
                    <ChevronDown size={16} />
                  </div>
                </button>
                {isOpen && (
                  <div className="faq-answer">
                    <p style={{ margin: 0 }}>{faq.a}</p>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </div>
    </section>
  )
}
