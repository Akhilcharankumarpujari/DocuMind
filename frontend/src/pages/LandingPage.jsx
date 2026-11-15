import { useState } from 'react'
import LandingNavbar from '../components/Landing/LandingNavbar.jsx'
import HeroSection from '../components/Landing/HeroSection.jsx'
import ProductPreview from '../components/Landing/ProductPreview.jsx'
import FeaturesSection from '../components/Landing/FeaturesSection.jsx'
import HowItWorksSection from '../components/Landing/HowItWorksSection.jsx'
import WhyDocuMindSection from '../components/Landing/WhyDocuMindSection.jsx'
import FaqSection from '../components/Landing/FaqSection.jsx'
import FinalCtaSection from '../components/Landing/FinalCtaSection.jsx'
import LandingFooter from '../components/Landing/LandingFooter.jsx'
import SignInModal from '../components/Landing/SignInModal.jsx'
import '../styles/landing.css'

export default function LandingPage() {
  const [signInOpen, setSignInOpen] = useState(false)

  return (
    <div className="landing-page">
      <LandingNavbar onOpenSignIn={() => setSignInOpen(true)} />
      <main>
        <HeroSection />
        <ProductPreview />
        <FeaturesSection />
        <HowItWorksSection />
        <WhyDocuMindSection />
        <FaqSection />
        <FinalCtaSection />
      </main>
      <LandingFooter />
      <SignInModal isOpen={signInOpen} onClose={() => setSignInOpen(false)} />
    </div>
  )
}
