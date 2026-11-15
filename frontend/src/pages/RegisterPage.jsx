import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { FileText, User, Mail, Lock, Eye, EyeOff, AlertCircle, Loader2, ArrowLeft, ArrowRight, Check, X } from 'lucide-react'
import '../styles/auth.css'

export default function RegisterPage() {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const { register } = useAuth()
  const navigate = useNavigate()

  // Real-time password criteria checks
  const criteria = {
    length: password.length >= 8,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    digit: /[0-9]/.test(password),
    special: /[^a-zA-Z0-9]/.test(password)
  }

  const isPasswordValid = Object.values(criteria).every(Boolean)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (submitting) return
    setError('')

    const trimmedName = fullName.trim()
    const trimmedEmail = email.trim()

    if (!trimmedName) {
      setError('Please provide your full name.')
      return
    }

    if (!trimmedEmail) {
      setError('Please provide your email address.')
      return
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      setError('Please enter a valid email address (e.g. name@company.com).')
      return
    }

    if (!isPasswordValid) {
      setError('Password must meet all complexity requirements listed below.')
      return
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match. Please re-enter your password confirmation.')
      return
    }

    setSubmitting(true)
    try {
      await register(trimmedName, trimmedEmail, password)
      navigate('/app', { replace: true })
    } catch (err) {
      setError(err.message || 'Registration failed. Please verify your details or use a different email.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        {/* Brand */}
        <Link to="/" className="auth-brand-link" title="DocuMind AI Home">
          <img
            src="/documind-logo.png"
            alt="DocuMind AI Logo"
            className="auth-brand-logo-img"
          />
        </Link>

        {/* Header */}
        <div className="auth-header">
          <h1 className="auth-title">Create Account</h1>
          <p className="auth-subtitle">
            Get started with AI-powered document intelligence.
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="auth-error-banner" role="alert">
            <AlertCircle size={18} />
            <span>{error}</span>
          </div>
        )}

        {/* Form */}
        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <label className="auth-label" htmlFor="register-name">
              Full Name
            </label>
            <div className="auth-input-wrapper">
              <User size={17} className="auth-input-icon" />
              <input
                id="register-name"
                type="text"
                className="auth-input"
                placeholder="Jane Doe"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                autoComplete="name"
                autoFocus
                required
                disabled={submitting}
              />
            </div>
          </div>

          <div className="auth-field">
            <label className="auth-label" htmlFor="register-email">
              Email Address
            </label>
            <div className="auth-input-wrapper">
              <Mail size={17} className="auth-input-icon" />
              <input
                id="register-email"
                type="email"
                className="auth-input"
                placeholder="name@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
                required
                disabled={submitting}
              />
            </div>
          </div>

          <div className="auth-field">
            <label className="auth-label" htmlFor="register-password">
              Password
            </label>
            <div className="auth-input-wrapper">
              <Lock size={17} className="auth-input-icon" />
              <input
                id="register-password"
                type={showPassword ? 'text' : 'password'}
                className="auth-input"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
                required
                disabled={submitting}
              />
              <button
                type="button"
                className="auth-toggle-pwd"
                onClick={() => setShowPassword(!showPassword)}
                tabIndex={-1}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
              </button>
            </div>
          </div>

          {/* Password criteria checklist */}
          {password.length > 0 && (
            <div className="auth-password-hints">
              <div className={`auth-hint-item ${criteria.length ? 'valid' : ''}`}>
                {criteria.length ? <Check size={13} /> : <X size={13} />}
                <span>At least 8 characters</span>
              </div>
              <div className={`auth-hint-item ${criteria.uppercase ? 'valid' : ''}`}>
                {criteria.uppercase ? <Check size={13} /> : <X size={13} />}
                <span>At least one uppercase letter (A-Z)</span>
              </div>
              <div className={`auth-hint-item ${criteria.lowercase ? 'valid' : ''}`}>
                {criteria.lowercase ? <Check size={13} /> : <X size={13} />}
                <span>At least one lowercase letter (a-z)</span>
              </div>
              <div className={`auth-hint-item ${criteria.digit ? 'valid' : ''}`}>
                {criteria.digit ? <Check size={13} /> : <X size={13} />}
                <span>At least one number (0-9)</span>
              </div>
              <div className={`auth-hint-item ${criteria.special ? 'valid' : ''}`}>
                {criteria.special ? <Check size={13} /> : <X size={13} />}
                <span>At least one special character (!@#$%^&*)</span>
              </div>
            </div>
          )}

          <div className="auth-field">
            <label className="auth-label" htmlFor="register-confirm">
              Confirm Password
            </label>
            <div className="auth-input-wrapper">
              <Lock size={17} className="auth-input-icon" />
              <input
                id="register-confirm"
                type={showPassword ? 'text' : 'password'}
                className="auth-input"
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
                required
                disabled={submitting}
              />
            </div>
          </div>

          <button
            type="submit"
            className="auth-submit-btn"
            disabled={submitting}
          >
            {submitting ? (
              <>
                <Loader2 size={18} className="spin-icon" />
                <span>Creating account...</span>
              </>
            ) : (
              <>
                <span>Create Free Account</span>
                <ArrowRight size={17} />
              </>
            )}
          </button>
        </form>

        {/* Footer */}
        <div className="auth-footer">
          Already have an account?{' '}
          <Link to="/login">Sign in here</Link>
        </div>
      </div>

      <Link to="/" className="auth-back-link">
        <ArrowLeft size={15} />
        <span>Return to DocuMind home</span>
      </Link>
    </div>
  )
}
